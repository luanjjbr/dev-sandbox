"""
Testes de integração do Piloto de Controle (CP - IEC 61851-1 Anexo A) da Bancada.
Valida o protocolo serial "estado ..." contra o firmware gravado (Mega ou Nano).

Uso (precisa do hardware ou do SimulIDE ligado a uma porta serial):
    python test_cp.py --porta COM3               # Arduino Mega 2560
    python test_cp.py --porta COM3 --placa nano  # Arduino Nano (CP indisponível)
Argumentos extras são repassados ao unittest (ex.: -v).
"""

import argparse
import sys
import time
import unittest

try:
    import serial
except ImportError:
    # Sai com código 2 (não 0): sem pyserial o teste NÃO rodou, não é "verde"
    print("ERRO: 'pyserial' nao instalado. Execute: pip install pyserial")
    sys.exit(2)

BAUD_RATE = 9600
TIMEOUT_S = 2.0
PORTA_SERIAL = None   # preenchido por argumento (--porta)
PLACA = "mega"        # preenchido por argumento (--placa)

# comando -> (rótulo esperado no status, relés esperados no status)
# Relés listados pelo pino físico da placa EasyEDA, na ordem do bitmask:
# D30 K_PE (S7), D31 K_FALHA (S8), D29 K_B_EN, D28 K_B_SEL, D27 K_CD_EN,
# D26 K_CD_SEL (NC = ramo C, NO = ramo D), D25 K_C_EN, D24 K_C_SEL,
# D23 K_D_EN, D22 K_D_SEL. Seletor só atua com o seu habilita.
ESTADOS_CP = {
    "ax":  ("AX",  "nenhum"),
    "bx+": ("BX+", "D30"),
    "bx":  ("BX",  "D30 D29"),
    "bx-": ("BX-", "D30 D29 D28"),
    "cx+": ("CX+", "D30 D29 D27"),
    "cx":  ("CX",  "D30 D29 D27 D25"),
    "cx-": ("CX-", "D30 D29 D27 D25 D24"),
    "dx+": ("DX+", "D30 D29 D27 D26"),
    "dx":  ("DX",  "D30 D29 D27 D26 D23"),
    "dx-": ("DX-", "D30 D29 D27 D26 D23 D22"),
    "e":   ("E",   "D30 D31"),
}

ALIASES = {"a": "AX", "b": "BX", "c": "CX", "d": "DX"}


class BaseSerial(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        try:
            cls.ser = serial.Serial(PORTA_SERIAL, BAUD_RATE, timeout=TIMEOUT_S)
            time.sleep(2.0)  # Aguarda o reset/bootloader do Arduino
            cls.ser.reset_input_buffer()
        except serial.SerialException as e:
            raise unittest.SkipTest(f"Porta serial {PORTA_SERIAL} nao disponivel: {e}")

    @classmethod
    def tearDownClass(cls):
        if hasattr(cls, "ser") and cls.ser.is_open:
            cls.ser.write(b"estado ax\n")  # Volta ao estado seguro
            time.sleep(0.1)
            cls.ser.close()

    def enviar_comando(self, comando: str) -> str:
        self.ser.reset_input_buffer()
        self.ser.write(f"{comando}\n".encode("utf-8"))
        return self.ser.readline().decode("utf-8", errors="ignore").strip()

    def status_cp(self) -> str:
        linha = self.enviar_comando("estado status")
        self.assertTrue(linha.startswith("[STATUS CP]"), f"status inesperado: '{linha}'")
        return linha

    @staticmethod
    def campo(linha: str, nome: str) -> str:
        """Extrai o valor de 'Nome: valor' na linha '[STATUS CP] A: x | B: y'."""
        for parte in linha.replace("[STATUS CP]", "").split("|"):
            parte = parte.strip()
            if parte.startswith(nome + ":"):
                return parte[len(nome) + 1:].strip()
        return ""


class TesteCPMega(BaseSerial):
    def test_01_boot_ou_estado_seguro_ax(self):
        """Após 'estado ax' o status informa AX sem relés fechados."""
        self.assertEqual(self.enviar_comando("estado ax"), "OK")
        linha = self.status_cp()
        self.assertEqual(self.campo(linha, "Estado"), "AX")
        self.assertEqual(self.campo(linha, "Reles"), "nenhum")

    def test_02_todos_os_estados(self):
        """Os 11 estados respondem OK e o status traz o rótulo e os relés da tabela."""
        for comando, (rotulo, reles) in ESTADOS_CP.items():
            with self.subTest(estado=comando):
                self.assertEqual(self.enviar_comando(f"estado {comando}"), "OK")
                linha = self.status_cp()
                self.assertEqual(self.campo(linha, "Estado"), rotulo)
                self.assertEqual(self.campo(linha, "Reles"), reles)

    def test_03_aliases_nominais(self):
        """'estado a|b|c|d' equivalem ao nominal de cada grupo."""
        for alias, rotulo in ALIASES.items():
            with self.subTest(alias=alias):
                self.assertEqual(self.enviar_comando(f"estado {alias}"), "OK")
                self.assertEqual(self.campo(self.status_cp(), "Estado"), rotulo)

    def test_04_maiusculas_e_espacos(self):
        """O parser ignora caixa e espaços extras."""
        self.assertEqual(self.enviar_comando("  ESTADO   CX-  "), "OK")
        self.assertEqual(self.campo(self.status_cp(), "Estado"), "CX-")

    def test_05_rejeicao_estado_invalido(self):
        """Argumentos inválidos retornam 'ERRO: Estado' sem mudar o estado."""
        self.assertEqual(self.enviar_comando("estado bx"), "OK")
        # "f" não é um estado da Bancada: cai no erro genérico
        for invalido in ("", "f", "g", "bx++", "b+", "ex", "ax+", "123"):
            with self.subTest(arg=invalido):
                self.assertEqual(self.enviar_comando(f"estado {invalido}".strip()), "ERRO: Estado")
        self.assertEqual(self.campo(self.status_cp(), "Estado"), "BX")

    def test_06_comandos_cabo_continuam(self):
        """Os comandos do PP seguem iguais com o CP presente."""
        self.assertEqual(self.enviar_comando("cabo 3"), "OK")
        self.assertEqual(self.enviar_comando("cabo 0"), "OK")
        self.assertEqual(self.enviar_comando("estadox"), "ERRO")
        self.assertEqual(self.enviar_comando("cabo1"), "ERRO")

    def test_07_estado_repetido_e_no_op(self):
        """Repetir o estado atual responde OK e mantém estado e relés."""
        self.assertEqual(self.enviar_comando("estado cx-"), "OK")
        self.assertEqual(self.enviar_comando("estado cx-"), "OK")
        linha = self.status_cp()
        self.assertEqual(self.campo(linha, "Estado"), "CX-")
        self.assertEqual(self.campo(linha, "Reles"), "D30 D29 D27 D25 D24")

    def test_08_linha_longa_descartada(self):
        """Linha > 31 caracteres é descartada inteira (não executa o prefixo)."""
        self.assertEqual(self.enviar_comando("estado bx"), "OK")
        longa = "estado e" + " " * 40 + "x"
        self.assertEqual(self.enviar_comando(longa), "ERRO: Comando longo")
        self.assertEqual(self.campo(self.status_cp(), "Estado"), "BX")
        # A próxima linha volta a ser processada normalmente
        self.assertEqual(self.enviar_comando("estado ax"), "OK")

    def test_09_debug_stack(self):
        """'debug stack' informa a stack livre das tasks."""
        self.assertTrue(self.enviar_comando("debug stack").startswith("[STACK] SerialCLI livre:"))


class TesteCPNano(BaseSerial):
    def test_01_estado_indisponivel(self):
        for comando in ("estado ax", "estado cx", "estado status", "estado"):
            with self.subTest(cmd=comando):
                self.assertEqual(self.enviar_comando(comando), "ERRO: CP indisponivel no Nano")


def main():
    global PORTA_SERIAL, PLACA
    parser = argparse.ArgumentParser(description="Testes do CP da Bancada")
    parser.add_argument("--porta", required=True, help="Porta serial (ex.: COM3)")
    parser.add_argument("--placa", choices=("mega", "nano"), default="mega")
    args, resto = parser.parse_known_args()
    PORTA_SERIAL = args.porta
    PLACA = args.placa

    # Na Nano, só a suíte da Nano roda; no Mega, só a do Mega.
    carregador = unittest.TestLoader()
    suite = carregador.loadTestsFromTestCase(TesteCPNano if PLACA == "nano" else TesteCPMega)
    executor = unittest.TextTestRunner(verbosity=2 if "-v" in resto else 1)
    resultado = executor.run(suite)
    sys.exit(0 if resultado.wasSuccessful() else 1)


if __name__ == "__main__":
    main()
