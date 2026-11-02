# Guia de Simulação — Bancada de Testes EVSE (IEC 61851-1 / IEC 62196-2) no SimulIDE 1.1.0-SR2

Este guia mostra como simular a **Bancada de Testes de Veículo Elétrico** (Arduino Mega 2560 + firmware FreeRTOS `firmware/Bancada`) no **SimulIDE 1.1.0-SR2** e como ligá-la ao aplicativo desktop **.NET 10** (*Bancada de Testes EVSE*). O circuito reproduz a **placa real** (esquemático EasyEDA `bancada_de_teste_reles_corrigido`, 2026-10-09): o emulador do cabo (**PP**) e os 11 estados do piloto de controle (**CP**: AX, BX+, BX, BX-, CX+, CX, CX-, DX+, DX, DX- e E).

> [!WARNING] Versão do simulador: use o **SimulIDE 1.1.0-SR2**
> `C:\Users\italo\Downloads\SimulIDE_1.1.0-SR2_Win64\SimulIDE_1.1.0-SR2_Win64\simulide.exe`.
> No SimulIDE 2.0 (R260501) o firmware dá boot, mas a CLI trava ao responder (FreeRTOS + recepção + transmissão serial). Captura: `capturas/simulide20_cli_travada_freertos_serial.png`.

---

## 📁 1. Arquivos

| Arquivo | Uso |
| :--- | :--- |
| **`circuito_bancada_mega_placa.sim1`** | **Padrão.** Estação do CP em **+12 V DC + 1 kΩ**, para ler o nível de cada estado no voltímetro `V_CP`. |
| `circuito_bancada_mega_placa_pwm.sim1` | Mesmo circuito com a estação em **PWM ±12 V, 1 kHz** (Wave Generator), para ver a forma de onda no osciloscópio. O voltímetro mostra uma média, não o nível. |
| `circuito_bancada_mega.sim1` | Versão editada pelo usuário (mantida; não é a referência da placa). |
| `circuito_bancada_teste.sim1` | Legado (Arduino Uno, só PP). |
| `capturas/` | Provas: recortes do esquemático (`esquematico_*.png`), circuito aberto/ligado, tensões medidas. |

O `.hex` é carregado por caminho relativo: `../../firmware/Bancada/.pio/build/megaatmega2560/firmware.hex`.

---

## 🌐 2. Topologia de Conexão Virtual (COM1 ↔ COM2)

```text
[ Aplicativo .NET 10 ] <---> ( COM2 ) <=== [ par virtual HHD Bridged Serial ] ===> ( COM1 ) <---> [ SimulIDE: BRIDGE_COM1 ] <---> Mega D0/D1
```

---

## 🔌 3. O que o circuito contém

1. **Arduino Mega 2560 (`Mega-1`)** com o firmware e a ponte **`BRIDGE_COM1`** (COM1, 9600 8-N-1) em D0/D1; GND no `GND0`.
2. **PP (IEC 62196-2):** 6 MOSFETs 2N7002 (gate 220 Ω + pull-down 10 kΩ) em D4..D9 com 4k7 / 1500 / 680 / 220 / 100 / 47 Ω até o PE; estação da **Fig. 5 do PDF 075: 3,3 V com 470 Ω**; voltímetro **`V_PP`**; barra de LEDs `PP_STATUS` (D4..D9 e D13).
3. **CP (IEC 61851-1 Anexo A), igual à placa:**
   - estação +12 V DC (ou PWM ±12 V) → **1 kΩ** → nó **CP** → diodo **D11 (1N4148)** → nó **CPV**; voltímetro **`V_CP`** no nó CP;
   - os **23 resistores individuais** do esquemático: ramo B = R30 1870, R31 910, R32 10, R33 1k8, R34 20; ramo C = R41 2, R40 30, R39 390, R38 390, R37 2, R35 820, R36 75, R42 13, R43 1; ramo D = R48 160, R49 10, R50 3, R45 3, R46 2, R51 130, R47 130, R44 10; falha R29 120;
   - **10 relés** (`RelaySPST`; os seletores são SPDT, `DT=true`: `switch1pinN` = NC, `switch0pinN` = NO), bobina ligada direto ao pino do Mega (o driver S8050 + 1 kΩ + 1N4007 da placa foi omitido; o relé do SimulIDE aciona com 20 mA).
4. **Osciloscópio:** CH1 = PP, CH2 = CP.

As ligações usam **Tunnels nomeados**: as redes (`CP`, `CPV`, `RET`, `FALHA`, `BSEL`, `B_NOM`, `B_INF`, `CDC`, `CT`, `DT`, `CSEL`, `C_NOM`, `C_INF`, `DSEL`, `D_NOM`, `D_INF`, `PP`) e as bobinas (`D22`..`D31`). **Para trocar a pinagem de um relé, basta renomear o Tunnel da bobina.**

### 3.1. Relés do CP (placa EasyEDA)

Cada ramo é uma árvore **habilita (EN) + seletor (SEL)**: o EN (C-NO) leva o nó ao comum do SEL, cujo NC vai ao tap nominal e o NO ao tap inferior.

| Pino | Relé | Função | Comum → NO / NC |
| :---: | :---: | :--- | :--- |
| D30 | RLY6 | K_PE (S7): conecta o veículo ao PE | RET → PE |
| D31 | RLY1 | K_FALHA (S8): falha 120 Ω antes do diodo | CP → FALHA (R29) |
| D29 | RLY7 | K_B_EN: habilita o bypass do ramo B | CPV → BSEL |
| D28 | RLY2 | K_B_SEL: seletor B | BSEL → B_INF / B_NOM |
| D27 | RLY5 | K_CD_EN: habilita a carga | CPV → CDC |
| D26 | RLY8 | K_CD_SEL: seletor de ramo | CDC → DT (ramo D) / CT (ramo C) |
| D25 | RLY3 | K_C_EN: habilita o bypass do ramo C | CT → CSEL |
| D24 | RLY4 | K_C_SEL: seletor C | CSEL → C_INF / C_NOM |
| D23 | RLY9 | K_D_EN: habilita o bypass do ramo D | DT → DSEL |
| D22 | RLY10 | K_D_SEL: seletor D | DSEL → D_INF / D_NOM |

---

## 🛠️ 4. Passo a Passo

1. **Compilar o firmware** (na raiz do repositório):
   ```powershell
   C:\Users\italo\.platformio\penv\Scripts\pio.exe run -d firmware\Bancada -e megaatmega2560
   ```
2. **Par serial virtual:** no *Virtual Serial Port Tools* (HHD), o par `COM1 ↔ COM2` deve estar ativo.
3. **Abrir** o `simulide.exe` do **1.1.0-SR2** → `Ctrl+O` → `simulacao/Bancada/circuito_bancada_mega_placa.sim1` → botão **Power Circuit**. O console mostra `Firmware successfully loaded` e `Connected to COM1`; `V_PP` = **3,300 V** e `V_CP` = **12,00 V** (AX).
4. **Aplicativo:** em *Bancada de Testes EVSE*, porta **COM2**, **9600**, **Conectar**.

**Sem o aplicativo:** troque o `SerialPort` por um **Serial Terminal** (*Peripherals*) ligado a D0/D1. Digite o comando em *Send Text* e envie o valor **10** em *Send Value* (o firmware só processa a linha no `\n`).

---

## 📊 5. Tensões medidas (SimulIDE 1.1.0-SR2, firmware via serial)

### 5.1. PP (estação 3,3 V + 470 Ω)

| Comando | Resistor | $V_{PP}$ calculado | $V_{PP}$ medido |
| :--- | :---: | :---: | :---: |
| `cabo 0` | aberto | 3,30 V | **3,300 V** |
| `cabo 1` | 4700 Ω | 3,00 V | — |
| `cabo 2` | 1500 Ω | 2,51 V | — |
| `cabo 3` | 680 Ω | 1,95 V | **1,958 V** |
| `cabo 4` | 220 Ω | 1,06 V | — |
| `cabo 5` | 100 Ω | 0,59 V | — |
| `cabo 6` | 47 Ω | 0,31 V | **0,335 V** |

### 5.2. CP (estação +12 V DC + 1 kΩ)

| Comando | Relés energizados | Tabela II | $V_{CP}$ medido |
| :--- | :--- | :---: | :---: |
| `estado ax` | nenhum | 12 V | **12,00 V** |
| `estado bx+` | D30 | 9,95 V | **9,928 V** |
| `estado bx` | D30 D29 | 8,92 V | **8,899 V** |
| `estado bx-` | D30 D29 D28 | 7,92 V | **7,892 V** |
| `estado cx+` | D30 D29 D27 | 6,93 V | **6,380 V** |
| `estado cx` | D30 D29 D27 D25 | 5,92 V | **5,858 V** |
| `estado cx-` | D30 D29 D27 D25 D24 | 4,90 V | **5,133 V** |
| `estado dx+` | D30 D29 D27 D26 | 3,91 V | **3,666 V** |
| `estado dx` | D30 D29 D27 D26 D23 | 2,91 V | **2,740 V** |
| `estado dx-` | D30 D29 D27 D26 D23 D22 | 1,96 V | **1,825 V** |
| `estado e` | D30 D31 | 0,97 V | **1,263 V** |

Os desvios em C, D e E vêm da rede da placa e do modelo de diodo do simulador, não do firmware:
- **CX+:** o ramo C sem bypass (1723 Ω) fica em paralelo com o B nominal (2740 Ω) = 1058 Ω, o que dá ~6,4 V.
- **E:** o R29 fica **antes** do diodo: 12 × 120 / 1120 ≈ 1,29 V, fora da janela 0 ± 1 V.
- **Demais (diodo):** o `Diode Default` do SimulIDE tem queda menor que a do 1N4148 real nas correntes de C/D.
- **Contatos:** a resistência dos contatos do relé é desprezível frente aos resistores.

Provas: `capturas/firmware_ponta_a_ponta_11_estados_e_PP.png` e `capturas/camada_c_VCP_11_estados_painel.png`.

> [!NOTE] Respostas longas (resolvido em 2026-11-02)
> No SimulIDE, com FreeRTOS, o MCU travava quando uma resposta passava dos 64 bytes do buffer de TX da `HardwareSerial` (`estado status`, `cabo status`, `help`): o laço "buffer cheio" esperava uma interrupção UDRE que o simulador não voltava a gerar. O firmware agora escreve pela `lib/SaidaSerial` (polling do UDRE, com `taskYIELD`), e todas as respostas saem completas. Provas: `capturas/diag_*.png` (diagnóstico) e `capturas/correcao_saidaserial_respostas_longas_OK.png`.
