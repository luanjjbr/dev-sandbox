# Mapeamento de Hardware e Pinagem — Projeto Bancada

Este documento detalha o mapeamento de pinos e as conexões elétricas para os microcontroladores suportados no projeto **Bancada** (**Arduino Nano** e **Arduino Mega 2560**), em conformidade com as normas **IEC 61851-1** e **IEC 62196-2** (Pino PP).

---

## 1. Tabela de Conexões de Hardware

| Identificação do Sinal | Pino Arduino Nano | Pino Arduino Mega 2560 | Tipo de I/O | Função no Firmware | Resistor / Estado Emulado |
| :--- | :---: | :---: | :---: | :--- | :--- |
| **`PINO_LED_STATUS`** | **D13** | **D13** | Saída Digital | Heartbeat do FreeRTOS (`tarefaBlink`, 500 ms) | LED Onboard |
| **`PINO_CABO_S1`** | **D4** | **D4** | Saída Digital | Chave MOSFET S1: Desconectado Nominal | $4700\,\Omega$ |
| **`PINO_CABO_S2`** | **D5** | **D5** | Saída Digital | Chave MOSFET S2: Cabo de 13 A | $1500\,\Omega$ |
| **`PINO_CABO_S3`** | **D6** | **D6** | Saída Digital | Chave MOSFET S3: Cabo de 20 A | $680\,\Omega$ |
| **`PINO_CABO_S4`** | **D7** | **D7** | Saída Digital | Chave MOSFET S4: Cabo de 32 A | $220\,\Omega$ |
| **`PINO_CABO_S5`** | **D8** | **D8** | Saída Digital | Chave MOSFET S5: Cabo de 63 A / 70 A | $100\,\Omega$ |
| **`PINO_CABO_S6`** | **D9** | **D9** | Saída Digital | Chave MOSFET S6: Simulação de Falha / Curto | $47\,\Omega$ |
| **UART RX** | **D0 (RX)** | **D0 (RX0)** | Entrada Digital | Recepção de comandos CLI (9600 bps, 8-N-1) | Interface Serial |
| **UART TX** | **D1 (TX)** | **D1 (TX0)** | Saída Digital | Transmissão de respostas e telemetria | Interface Serial |
| **Alimentação 5V** | **5V** | **5V** | Alimentação | Alimentação lógica TTL | Fonte do barramento |
| **Terra (GND/PE)**| **GND** | **GND** | Referência | Referência elétrica conectada ao PE | Terra de Proteção |

---

## 2. Emulador do Pino CP (Control Pilot — IEC 61851-1 Anexo A)

Mapeamento dos 10 relés SRD-05VDC-SL-C da **placa real** (esquemático EasyEDA `bancada_de_teste_reles_corrigido`, 2026-10-09) no **Arduino Mega 2560**. Driver de cada relé: `Dxx` → 1 kΩ → NPN S8050 (com 1N4007 na bobina), ativo em `HIGH`. Recortes do esquemático que provam cada linha: `simulacao/Bancada/capturas/esquematico_*.png`.

Cada ramo é uma **árvore "habilita + seletor"**: o relé habilita (EN, contato C-NO) leva o nó ao comum do relé seletor (SEL), cujo **NC** vai ao tap nominal e o **NO** ao tap inferior. O seletor só atua com o seu habilita ligado.

| Identificação do Sinal | Pino Mega | Relé | Função (nome da Tabela II) | Contatos usados |
| :--- | :---: | :---: | :--- | :--- |
| **`PINO_CP_K_PE`** | **D30** | RLY6 | $S_7$: liga o retorno do veículo ao PE (aberto = AX, 12 V) | C = retorno, NO = PE |
| **`PINO_CP_K_FALHA`** | **D31** | RLY1 | $S_8$: falha $120\,\Omega$ (R29) antes do diodo (Estado E) | C = CP, NO = R29 |
| **`PINO_CP_K_B_EN`** | **D29** | RLY7 | habilita o bypass do ramo B ($S_{9.1}$: 2740 Ω, BX 9 V) | C = CPV (catodo), NO = 28_C |
| **`PINO_CP_K_B_SEL`** | **D28** | RLY2 | seletor B: NC = abaixo de R30 (2740 Ω), NO = abaixo de R32 (1820 Ω, $S_{9.2}$, BX- 8 V) | C, NC, NO |
| **`PINO_CP_K_CD_EN`** | **D27** | RLY5 | habilita a carga (ramo C ou D em paralelo com o B) | C = CPV, NO = 26_C |
| **`PINO_CP_K_CD_SEL`** | **D26** | RLY8 | seletor SPDT de ramo: NC = ramo C, NO = ramo D | C, NC, NO |
| **`PINO_CP_K_C_EN`** | **D25** | RLY3 | habilita o bypass do ramo C ($S_{11.1}$: 1301 Ω, CX 6 V) | C = topo C, NO = 24_C |
| **`PINO_CP_K_C_SEL`** | **D24** | RLY4 | seletor C: NC = abaixo de R39 (1301 Ω), NO = abaixo de R37 (909 Ω, $S_{11.2}$, CX- 5 V) | C, NC, NO |
| **`PINO_CP_K_D_EN`** | **D23** | RLY9 | habilita o bypass do ramo D ($S_{12.1}$: 270 Ω, DX 3 V) | C = topo D, NO = 22_C |
| **`PINO_CP_K_D_SEL`** | **D22** | RLY10 | seletor D: NC = abaixo de R46 (270 Ω), NO = abaixo de R51 (140 Ω, $S_{12.2}$, DX- 2 V) | C, NC, NO |

Rede de resistores (placa): ramo B = R30 1870 + R31 910 + R32 10 + R33 1k8 + R34 20 (4610 Ω); ramo C = R41 2 + R40 30 + R39 390 + R38 390 + R37 2 + R35 820 + R36 75 + R42 13 + R43 1 (1723 Ω); ramo D = R48 160 + R49 10 + R50 3 + R45 3 + R46 2 + R51 130 + R47 130 + R44 10 (448 Ω); falha R29 120 Ω; diodo D11 1N4148.

*Nota: sem o habilita, cada ramo fica no Limite Superior (10 V para B, 7 V para C e 4 V para D). Os nomes $S_{9.0}$, $S_{11.0}$ e $S_{12.0}$ da Tabela II não são relés, apenas o repouso sem bypass; o "S10" da Tabela II é o par D27 (habilita) + D26 (seletor C/D).*

### 2.1. Tabela Estado → Relés (comando `estado …`, firmware)

| Estado | Comando | $V_{CP}$ | Relés energizados (demais desligados) |
| :--- | :--- | :---: | :--- |
| AX | `estado ax` / `a` | 12 V | nenhum |
| BX+ | `estado bx+` | 10 V | D30 |
| BX | `estado bx` / `b` | 9 V | D30, D29 |
| BX- | `estado bx-` | 8 V | D30, D29, D28 |
| CX+ | `estado cx+` | 7 V | D30, D29, D27 |
| CX | `estado cx` / `c` | 6 V | D30, D29, D27, D25 |
| CX- | `estado cx-` | 5 V | D30, D29, D27, D25, D24 |
| DX+ | `estado dx+` | 4 V | D30, D29, D27, D26 |
| DX | `estado dx` / `d` | 3 V | D30, D29, D27, D26, D23 |
| DX- | `estado dx-` | 2 V | D30, D29, D27, D26, D23, D22 |
| E | `estado e` | 1 V | D30, D31 |

O Estado F (−12 V) é gerado pela estação e não é emulado pela Bancada (`estado f` → `ERRO: Estado`).

---

## 3. Parâmetros Elétricos e Segurança

- **Nível Lógico:** 5.0 V (TTL);
- **Lógica de Relés:** Configurada via `NIVEL_RELE_ATIVO` (`HIGH` para driver discreto NPN S8050 / `LOW` para módulos comerciais optoacoplados ativos em nível baixo);
- **Comutação Break-Before-Make:** Assegura que apenas uma chave MOSFET do cabo PP conduza por vez, com intervalo de acomodação de $5\,\mu\text{s}$ para evitar resistências espúrias em paralelo;
- **Comutação Break-Before-Make do CP:** abre só os relés que saem, aguarda no mínimo `TEMPO_BREAK_BEFORE_MAKE_CP_MS` = 20 ms (com o tick de ~16 ms do FreeRTOS no AVR, 32–48 ms reais) e fecha só os que entram. O intermediário é a interseção dos dois estados: entre estados conectados mantém S7 (o CP nunca cai para 12 V) e nunca contém S8 nem os ramos C e D juntos (`static_assert` em `TabelaEstadosCP.cpp`);
- **Boot Seguro:** o `setup()` coloca todas as chaves do PP e os relés do CP em nível inativo antes de criar as tasks;
- **Resistores de Gate:** Recomendado uso de resistores pull-down de $10\text{ k}\Omega$ nos gates dos MOSFETs e nas bases dos transistores de relé para mantê-los em corte durante o reset/inicialização do microcontrolador.
