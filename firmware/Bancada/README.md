# Bancada

Projeto de bancada baseado em **PlatformIO** com sistema operacional de tempo real **FreeRTOS** para microcontroladores AVR.

---

## 🎯 Especificações do Projeto

- **Microcontroladores Suportados:**
  - **Arduino Nano** (ATmega328P - Bootloader Antigo e Novo)
  - **Arduino Mega 2560** (ATmega2560)
- **Comunicação Serial:** `9600 bps` (Baud Rate)
- **Framework:** Arduino com `feilipu/FreeRTOS`
- **Exemplo Base:** Multitarefa *Blink* com temporização não-bloqueante (`vTaskDelay`).

---

## 📂 Estrutura de Pastas

A estrutura segue rigorosamente o padrão do projeto `bancada_de_teste`:

```text
Bancada/
├── .gitignore             # Regras de exclusão do Git
├── platformio.ini         # Configuração dos ambientes de compilação (Nano e Mega)
├── README.md              # Documentação principal do projeto
├── docs/                  # Documentações técnicas e especificações
│   └── README.md
├── include/               # Arquivos de cabeçalho (.h)
│   └── ConfigHardware.h  # Definições de hardware e pinos
├── lib/                   # Bibliotecas e módulos reutilizáveis (POO)
│   ├── EmuladorCabo/      # Pino PP: chaves S1..S6
│   ├── EmuladorVeiculoCP/ # Pino CP: 10 relés D22..D31 (Mega), tabela e parser
│   └── SaidaSerial/       # Saída da UART0 por polling (respostas longas no FreeRTOS/SimulIDE)
├── src/                   # Código-fonte principal
│   └── main.cpp           # CLI serial com FreeRTOS (Blink + SerialCLI)
├── tests/                 # Testes automatizados
│   ├── python/            # Scripts de automação em Python
│   ├── relatorios/        # Relatórios de testes gerados
│   └── robot/             # Cenários de teste do Robot Framework
└── .vscode/               # Configurações do VS Code
    └── settings.json
```

---

## 📡 Protocolo Serial (9600 bps, linhas terminadas em `\n`)

Comandos sem diferença de maiúsculas/minúsculas e tolerantes a espaços. Linhas com mais de 31 caracteres são descartadas inteiras com `ERRO: Comando longo`. `cabo` e `estado` só são reconhecidos como palavra inteira (`cabo1` → `ERRO`).

### Pino PP — cabo (IEC 62196-2), Nano e Mega
| Comando | Resposta |
| :--- | :--- |
| `cabo 0` … `cabo 6` | `OK` (`cabo 0` = circuito aberto) |
| `cabo status` | `[STATUS CABO] Canal: … \| R: … Ohms \| Imax: … A \| Descricao: …` |
| `cabo <inválido>` | `ERRO: Cabo` |

### Pino CP — estado do veículo (IEC 61851-1 Anexo A), somente Mega
| Comando | Estado | V<sub>CP</sub> | Relés energizados (pino) |
| :--- | :--- | :---: | :--- |
| `estado ax` (ou `a`) | AX — não conectado | 12 V | nenhum |
| `estado bx+` | BX limite superior | 10 V | D30 |
| `estado bx` (ou `b`) | BX nominal | 9 V | D30, D29 |
| `estado bx-` | BX limite inferior | 8 V | D30, D29, D28 |
| `estado cx+` | CX limite superior | 7 V | D30, D29, D27 |
| `estado cx` (ou `c`) | CX nominal | 6 V | D30, D29, D27, D25 |
| `estado cx-` | CX limite inferior | 5 V | D30, D29, D27, D25, D24 |
| `estado dx+` | DX limite superior | 4 V | D30, D29, D27, D26 |
| `estado dx` (ou `d`) | DX nominal | 3 V | D30, D29, D27, D26, D23 |
| `estado dx-` | DX limite inferior | 2 V | D30, D29, D27, D26, D23, D22 |
| `estado e` | E — falha / curto CP-PE | 1 V | D30, D31 |

- Respostas: `OK`; argumento inválido → `ERRO: Estado`; no Nano, qualquer `estado …` → `ERRO: CP indisponivel no Nano`.
- `estado status` → `[STATUS CP] Estado: CX | Vcp: 6 V | <descrição> | Reles: D30 D29 D27 D25` (relés listados pelo pino da placa).
- Pinagem da **placa real** (EasyEDA): D30 = PE (S7), D31 = falha 120 Ω (S8); cada ramo é uma árvore habilita + seletor (D29/D28 ramo B, D27 carga + D26 seletor C/D, D25/D24 ramo C, D23/D22 ramo D). Detalhes em [docs/pinout.md](docs/pinout.md).
- **Troca de estado (*break-before-make*):** abre só os relés que saem, aguarda no mínimo `TEMPO_BREAK_BEFORE_MAKE_CP_MS` (20 ms; ~32–48 ms reais com o tick de ~16 ms do FreeRTOS) e fecha só os que entram. A pausa só ocorre quando algum relé sai e outro entra; trocas que só fecham (ex.: AX → BX) ou só abrem (ex.: CX → CX+) são imediatas. O intermediário é a interseção dos dois estados: entre estados conectados ele sempre mantém D30/S7 (nunca passa por AX), nunca tem D31/S8 e é sempre uma combinação coerente da árvore de relés (seletor só com o seu habilita; C e D nunca juntos, pois D26 é SPDT), o que é provado por `static_assert` em `lib/EmuladorVeiculoCP/TabelaEstadosCP.cpp`. Reenviar o estado atual é *no-op*: o firmware não reescreve os relés. Para reaplicá-los (ex.: depois de um brown-out só no módulo de relés), mande `estado ax` e depois o estado desejado.
- **Boot seguro:** todos os relés do CP e as chaves do PP são abertos no `setup()`, antes das tasks.

### Outros
| Comando | Resposta |
| :--- | :--- |
| `help` ou `?` | Menu de comandos |
| `debug stack` | `[STACK] SerialCLI livre: N de M \| Blink livre: N de M` (*high water mark*, bytes) |
| qualquer outro | `ERRO` |

---

## 🧪 Testes (precisam do hardware ou do SimulIDE numa porta serial)

```bash
pip install pyserial
python tests/python/test_cabo.py                         # PP (porta fixa no script)
python tests/python/test_cp.py --porta COM3              # CP no Mega
python tests/python/test_cp.py --porta COM3 --placa nano # Nano: CP indisponível
```
Sem `pyserial`, o `test_cp.py` sai com código 2 (não roda, então não conta como aprovado).

---

## 🚀 Como Compilar e Enviar

### 1. Arduino Nano (Bootloader Antigo - Clones CH340)
```bash
pio run -e nanoatmega328 --target upload
```

### 2. Arduino Nano (Bootloader Novo)
```bash
pio run -e nanoatmega328new --target upload
```

### 3. Arduino Mega 2560
```bash
pio run -e megaatmega2560 --target upload
```

### 4. Monitor Serial
Para abrir o monitor serial a 9600 baud:
```bash
pio device monitor -b 9600
```

