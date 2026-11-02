# Guia de Simulação — Bancada de Testes EVSE (IEC 61851-1 / IEC 62196-2) no SimulIDE 2.0

Este guia orienta a integração e os ensaios virtuais completos da **Bancada de Testes de Veículo Elétrico** no **SimulIDE 2.0** com o aplicativo desktop **.NET 10** (*Bancada de Testes EVSE*), contemplando tanto a emulação de cabos **PP** (Proximity Pilot) quanto os 11 estados do piloto de controle **CP** (Control Pilot: AX, BX+, BX, BX-, CX+, CX, CX-, DX+, DX, DX- e E).

---

## 🌐 1. Topologia de Conexão Virtual (COM1 ↔ COM2)

```text
[ Aplicativo Desktop .NET 10 ]  <--->  ( COM2 )  <=== [Túnel Virtual] ===>  ( COM1 )  <--->  [ SimulIDE 2.0: BRIDGE_COM1 ]
    (Selecionar COM2)                                                                           (Arduino Mega 2560 FreeRTOS)
```

O circuito virtual (`circuito_bancada_mega.sim1`) integra:
1. **Microcontrolador Arduino Mega 2560 (`Mega-1`):** Executa o firmware multitarefa FreeRTOS com autocarregamento do binário `.hex`;
2. **Ponte Serial UART (`BRIDGE_COM1`):** Conectada aos pinos RX0/TX0 do Mega e associada à porta virtual `COM1` a 9600 bps (8-N-1);
3. **Barramento do Cabo PP (IEC 62196-2):**
   - 6 MOSFETs 2N7002 ($Q_1$ a $Q_6$) acionados pelos pinos **D4 a D9**;
   - Barramento resistivo calibrado ($4700\,\Omega$, $1500\,\Omega$, $680\,\Omega$, $220\,\Omega$, $100\,\Omega$ e $47\,\Omega$);
   - Circuito de detecção da estação EVSE: Fonte de $+5\text{ V}$ com resistor de pull-up $R_{PU} = 470\,\Omega$;
   - **Voltímetro Probe (`V_PP`):** Exibe a tensão analógica exata no pino PP.
4. **Barramento do Piloto de Controle CP (IEC 61851-1 Anexo A):**
   - 10 Relés SPST de 5V acionados pelos pinos **D22 a D31** com proteção *Break-Before-Make*;
   - Circuito de alimentação da estação EVSE: Fonte de $+12\text{ V}$ e resistor interno $R_1 = 1000\,\Omega$;
   - Diodo retificador veicular `1N4148`;
   - Chave $S_7$ (D22) de conexão com Terra de Proteção (PE);
   - Chave $S_8$ (D23) de simulação de falha direta de linha ($120\,\Omega$ antes do diodo);
   - Barramentos resistivos para os Estados B ($2740\,\Omega$), C ($882\,\Omega$ equiv.) e D ($245\,\Omega$ equiv.) com derivações de tolerância;
   - **Voltímetro Probe (`V_CP`):** Exibe a tensão analógica do sinal de controle piloto em tempo real;
5. **Painel de Sinalização Visual:**
   - Barra de 8 LEDs (`PP_STATUS`): Monitora os canais D4..D9 do cabo e o *Heartbeat* do FreeRTOS em D13;
   - Barra de 10 LEDs (`CP_RELAYS`): Monitora o acionamento em tempo real dos relés D22..D31.

> [!NOTE] Preservação do Circuito Legado (`circuito_bancada_teste.sim1`):
> O arquivo `circuito_bancada_teste.sim1` é mantido na pasta para fins de histórico e compatibilidade com simulações antigas do **Arduino Uno**. No Uno, apenas o barramento PP (D4..D9) é emulado, pois o microcontrolador ATmega328P não possui os pinos digitais D22..D31 requeridos para o controle CP.

---

## 🛠️ 2. Passo a Passo de Execução e Ensaios

### Passo 1: Compilar o Firmware para o Arduino Mega 2560
No terminal da raiz do projeto (`firmware/Bancada`):
```powershell
cd firmware/Bancada
pio run -e megaatmega2560
```
O PlatformIO compilará o firmware gerando o artefato em:
`firmware/Bancada/.pio/build/megaatmega2560/firmware.hex`.

### Passo 2: Criar o Par de Portas Seriais Virtuais no Windows
1. Abra o utilitário **Virtual Serial Port Tools** (ou software de emulação serial equivalente);
2. Crie um par de portas virtuais conectadas entre si: **`COM1 ↔ COM2`**;
3. Inicie o serviço de tunelamento.

### Passo 3: Abrir e Ligar o Circuito no SimulIDE 2.0
1. Execute o **SimulIDE 2.0** (`C:\Users\italo\Downloads\SimulIDE_2-R260501_Win64\SimulIDE_2-R260501_Win64\simulide.exe`);
2. Pressione `Ctrl + O` e abra o circuito:
   ```text
   simulacao/Bancada/circuito_bancada_mega.sim1
   ```
3. Clique no botão vermelho de energia **Power Circuit** (topo esquerdo da barra de ferramentas);
4. O SimulIDE se conectará automaticamente à porta **`COM1`** e o **LED D13 (Heartbeat)** começará a piscar com cadência de 500 ms;
5. Inicialmente, o voltímetro **`V_PP`** exibirá **`5.00 V`** (circuito aberto) e o voltímetro **`V_CP`** exibirá **`12.00 V`** (Estado AX Nominal — veículo desconectado).

### Passo 4: Conectar o Aplicativo Desktop .NET 10
1. Inicie a aplicação **DevSandbox** (ou o executável standalone da Bancada);
2. Navegue até a tela **Bancada de Testes EVSE**;
3. No seletor **PORTA SERIAL**, escolha **`COM2`**;
4. No seletor **BAUD RATE**, mantenha **`9600`**;
5. Clique em **Conectar**. O indicador ficará verde (*Conectado*).

---

## 📊 3. Tabela de Estados e Respostas Esperadas nos Voltímetros

### 3.1. Ensaio do Pino de Proximidade (PP — IEC 62196-2)

| Opção no Aplicativo / Comando CLI | Resistor ($R_{PP}$) | LED Ativo | Corrente Máxima | Tensão no Voltímetro ($V_{PP}$) |
| :--- | :---: | :---: | :---: | :---: |
| **0: Circuito Aberto** (`cabo 0`) | $\infty$ (Aberto) | Nenhum | 0 A | **$5{,}00\text{ V}$** |
| **1: Desconectado** (`cabo 1`) | $4700\,\Omega$ | **LED 0** (`D4`) | 0 A | **$\approx 4{,}54\text{ V}$** |
| **2: Cabo 13 A** (`cabo 2`) | $1500\,\Omega$ | **LED 1** (`D5`) | **13 A** | **$\approx 3{,}80\text{ V}$** |
| **3: Cabo 20 A** (`cabo 3`) | $680\,\Omega$ | **LED 2** (`D6`) | **20 A** | **$\approx 2{,}95\text{ V}$** |
| **4: Cabo 32 A** (`cabo 4`) | $220\,\Omega$ | **LED 3** (`D7`) | **32 A** | **$\approx 1{,}59\text{ V}$** |
| **5: Cabo 63 A** (`cabo 5`) | $100\,\Omega$ | **LED 4** (`D8`) | **63 A** | **$\approx 0{,}87\text{ V}$** |
| **6: Falha / Curto** (`cabo 6`) | $47\,\Omega$ | **LED 5** (`D9`) | 0 A | **$\approx 0{,}45\text{ V}$** |

---

### 3.2. Ensaio do Piloto de Controle (CP — IEC 61851-1 Anexo A)

| Estado CP / Comando CLI | Condição Operacional | Relés Fechados | Tensão Teórica | Tensão no Voltímetro ($V_{CP}$) |
| :--- | :--- | :--- | :---: | :---: |
| **`estado ax`** | Veículo não conectado (Desconectado) | Nenhum (S7 aberto) | $+12\text{ V}$ | **$12{,}00\text{ V}$** |
| **`estado bx+`** | Veículo conectado (Standby) — Limite Superior | S7 (D22) | $+10\text{ V}$ | **$\approx 9{,}95\text{ V}$** |
| **`estado bx`** | Veículo conectado (Standby) — **Nominal** | S7 (D22), S9.1 (D24) | $+9\text{ V}$ | **$\approx 8{,}92\text{ V}$** |
| **`estado bx-`** | Veículo conectado (Standby) — Limite Inferior | S7 (D22), S9.2 (D25) | $+8\text{ V}$ | **$\approx 7{,}92\text{ V}$** |
| **`estado cx+`** | Carga sem ventilação — Limite Superior | S7 (D22), S9.1 (D24), S10.2 (D26) | $+7\text{ V}$ | **$\approx 6{,}93\text{ V}$** |
| **`estado cx`** | Carga sem ventilação — **Nominal** | S7, S9.1, S10.2, S11.1 (D28) | $+6\text{ V}$ | **$\approx 5{,}92\text{ V}$** |
| **`estado cx-`** | Carga sem ventilação — Limite Inferior | S7, S9.1, S10.2, S11.2 (D29) | $+5\text{ V}$ | **$\approx 4{,}90\text{ V}$** |
| **`estado dx+`** | Carga com ventilação — Limite Superior | S7 (D22), S9.1 (D24), S10.1 (D27) | $+4\text{ V}$ | **$\approx 3{,}91\text{ V}$** |
| **`estado dx`** | Carga com ventilação — **Nominal** | S7, S9.1, S10.1, S12.1 (D30) | $+3\text{ V}$ | **$\approx 2{,}91\text{ V}$** |
| **`estado dx-`** | Carga com ventilação — Limite Inferior | S7, S9.1, S10.1, S12.2 (D31) | $+2\text{ V}$ | **$\approx 1{,}96\text{ V}$** |
| **`estado e`** | Falha: Curto-circuito CP-PE antes do diodo | S7 (D22), S8 (D23) | $+1\text{ V}$ | **$\approx 0{,}97\text{ V}$** |

---

## 🔧 4. Mapeamento de Pinos no Arduino Mega 2560

- **`D0 (RX0)` / `D1 (TX0)`:** Conexão UART com a ponte `BRIDGE_COM1` (9600 baud, 8-N-1);
- **`D4`:** MOSFET $Q_1$ $\rightarrow$ Resistor $4700\,\Omega$ (PP Desconectado);
- **`D5`:** MOSFET $Q_2$ $\rightarrow$ Resistor $1500\,\Omega$ (PP 13 A);
- **`D6`:** MOSFET $Q_3$ $\rightarrow$ Resistor $680\,\Omega$ (PP 20 A);
- **`D7`:** MOSFET $Q_4$ $\rightarrow$ Resistor $220\,\Omega$ (PP 32 A);
- **`D8`:** MOSFET $Q_5$ $\rightarrow$ Resistor $100\,\Omega$ (PP 63 A);
- **`D9`:** MOSFET $Q_6$ $\rightarrow$ Resistor $47\,\Omega$ (PP Falha/Curto);
- **`D13`:** LED Onboard / Heartbeat do FreeRTOS (500 ms);
- **`D22`:** Relé $S_7$ $\rightarrow$ Conexão do circuito veicular ao Terra de Proteção (PE);
- **`D23`:** Relé $S_8$ $\rightarrow$ Ramo de falha $120\,\Omega$ direto para PE antes do diodo;
- **`D24`:** Relé $S_{9.1}$ $\rightarrow$ Bypass no barramento B (BX Nominal, 9 V);
- **`D25`:** Relé $S_{9.2}$ $\rightarrow$ Bypass no barramento B (BX Limite Inferior, 8 V);
- **`D26`:** Relé $S_{10.2}$ ($S_{10\_C}$) $\rightarrow$ Habilita o barramento de carga C;
- **`D27`:** Relé $S_{10.1}$ ($S_{10\_D}$) $\rightarrow$ Habilita o barramento de carga D;
- **`D28`:** Relé $S_{11.1}$ $\rightarrow$ Bypass no barramento C (CX Nominal, 6 V);
- **`D29`:** Relé $S_{11.2}$ $\rightarrow$ Bypass no barramento C (CX Limite Inferior, 5 V);
- **`D30`:** Relé $S_{12.1}$ $\rightarrow$ Bypass no barramento D (DX Nominal, 3 V);
- **`D31`:** Relé $S_{12.2}$ $\rightarrow$ Bypass no barramento D (DX Limite Inferior, 2 V).
