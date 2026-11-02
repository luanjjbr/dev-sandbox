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
│   └── EmuladorVeiculoCP/ # Pino CP: 10 relés D22..D31 (Mega), tabela e parser
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
| Comando | Estado | V<sub>CP</sub> | Relés fechados |
| :--- | :--- | :---: | :--- |
| `estado ax` (ou `a`) | AX — não conectado | 12 V | nenhum |
| `estado bx+` | BX limite superior | 10 V | S7 |
| `estado bx` (ou `b`) | BX nominal | 9 V | S7, S9.1 |
| `estado bx-` | BX limite inferior | 8 V | S7, S9.2 |
| `estado cx+` | CX limite superior | 7 V | S7, S9.1, S10.2 |
| `estado cx` (ou `c`) | CX nominal | 6 V | S7, S9.1, S10.2, S11.1 |
| `estado cx-` | CX limite inferior | 5 V | S7, S9.1, S10.2, S11.2 |
| `estado dx+` | DX limite superior | 4 V | S7, S9.1, S10.1 |
| `estado dx` (ou `d`) | DX nominal | 3 V | S7, S9.1, S10.1, S12.1 |
| `estado dx-` | DX limite inferior | 2 V | S7, S9.1, S10.1, S12.2 |
| `estado e` | E — falha / curto CP-PE | 1 V | S7, S8 |

- Respostas: `OK`; argumento inválido → `ERRO: Estado`; no Nano, qualquer `estado …` → `ERRO: CP indisponivel no Nano`.
- `estado status` → `[STATUS CP] Estado: CX | Vcp: 6 V | <descrição> | Reles: S7 S9.1 S10.2 S11.1`.
- S10.2 (D26) habilita o ramo C; **S10.1 (D27) habilita o ramo D e corresponde ao "S10.0" da Tabela II**.
- **Troca de estado (*break-before-make*):** abre só os relés que saem, aguarda no mínimo `TEMPO_BREAK_BEFORE_MAKE_CP_MS` (20 ms; ~32–48 ms reais com o tick de ~16 ms do FreeRTOS) e fecha só os que entram. A pausa só ocorre quando algum relé sai e outro entra; trocas que só fecham (ex.: AX → BX) ou só abrem (ex.: CX → CX+) são imediatas. O intermediário é a interseção dos dois estados: entre estados conectados ele sempre mantém S7 (nunca passa por AX) e nunca tem S8 nem os ramos C e D juntos, o que é provado por `static_assert` em `lib/EmuladorVeiculoCP/TabelaEstadosCP.cpp`. Reenviar o estado atual é *no-op*: o firmware não reescreve os relés. Para reaplicá-los (ex.: depois de um brown-out só no módulo de relés), mande `estado ax` e depois o estado desejado.
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

