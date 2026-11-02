# 🧠 CONTEXT.md — Contexto do Projeto dev-sandbox

Documento de contexto técnico, arquitetural e operacional do repositório **dev-sandbox**. Criado para fornecer a desenvolvedores e agentes de IA uma visão aprofundada da arquitetura, convenções, módulos implementados, status de testes e diretrizes de evolução.

---

## 📌 1. Visão Geral

* **Repositório:** `dev-sandbox`
* **Finalidade:** Laboratório pessoal de desenvolvimento em C# e .NET para prototipagem rápida, utilitários práticos de engenharia e experimentação de padrões arquiteturais modernos.
* **Stack Principal:**
  * **Linguagem / Runtime:** C# 13+ / .NET 10.0 (`net10.0` para Core/Testes e `net10.0-windows` para UI)
  * **Padrões de Qualidade Global:** `Directory.Build.props` com compilação estrita (`TreatWarningsAsErrors=true`), nulabilidade ativa (`Nullable=enable`) e analisadores de código de última geração (`EnableNETAnalyzers=true`, `AnalysisLevel=latest-recommended`)
  * **Interface Gráfica:** Windows Forms com Dark Mode nativo via Desktop Window Manager (`dwmapi.dll`) e controles customizados WinUI 3 Fluent Dark
  * **Comunicação Serial:** `System.IO.Ports` com contratos desacoplados (`ISerialEngine`, `ISerialPort`, `SistemaSerialPort`)
  * **Abstração Temporal:** `System.TimeProvider` nativo do .NET com suporte a testes determinísticos (`Microsoft.Extensions.TimeProvider.Testing`)
  * **Testes Automatizados:** NUnit 4 (`NUnit`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk`)
  * **Solução:** Formato de solução XML do Visual Studio (`dev-sandbox.slnx`)
  * **Firmware e Simulação:** PlatformIO, FreeRTOS, SimulIDE 2.0 e HHD Virtual Serial Port Tools

---

## 🏛️ 2. Arquitetura da Solução

O projeto segue estritamente a separação de responsabilidades em camadas desacopladas, com foco em testabilidade sem dependência de hardware físico ou subsistemas de janelas:

```plaintext
dev-sandbox/
├── dev-sandbox.slnx                     # Arquivo de solução unificada
├── Directory.Build.props                # Regras de build corporativas (avisos como erro, analisadores)
├── README.md                            # Guia rápido e apresentação do projeto
├── CONTEXT.md                           # Memória técnica e contexto arquitetural completo
├── publish-standalone.ps1               # Script para gerar executável único standalone
├── dist/                                # Compilações locais e saídas do publish
│   └── standalone/                      # Diretório de saída do executável único (~49 MB)
├── firmware/                            # Firmware embarcado da bancada de testes EVSE
│   └── Bancada/                         # Projeto PlatformIO + FreeRTOS (foco no ATmega2560)
├── simulacao/                           # Arquivos de simulação virtual da bancada
│   └── Bancada/                         # Esquemático SimulIDE 2.0 (Mega) e guia de portas COM
├── src/
│   ├── DevSandbox.Core/                 # [Camada de Domínio e Regras de Negócio - net10.0 pura]
│   │   ├── Bancada/                     # BancadaEngine, EstadoCaboPP, EstadoVeiculoCP (11 estados), RespostaFirmware
│   │   ├── Converters/                  # CalculadoraHex, BaseNumerica
│   │   ├── Pomodoro/                    # PomodoroEngine (TimeProvider), EstadoPomodoro
│   │   └── Serial/                      # ISerialEngine, ISerialPort, SistemaSerialPort, SupervisorioSerialEngine
│   │
│   └── DevSandbox.UI/                   # [Camada de Apresentação WinForms Fluent - net10.0-windows]
│       ├── Common/                      # ThemeManager (cache GDI, desenho de foco), controles Fluent
│       ├── Forms/                       # MainForm, FormBancada, FormCalculadora, FormPomodoro, FormSupervisorio
│       └── Program.cs                   # Ponto de entrada STAThread
│
└── tests/
    └── DevSandbox.Tests/                # [Camada de Testes Automatizados - NUnit 4]
        ├── Fakes/                       # FakeSerialPort, FakeSerialEngine
        ├── BancadaCPTests.cs            # Testes da máquina de estados CP (11 estados) e tolerâncias
        ├── BancadaEngineTests.cs        # Testes de comando, eventos e ciclo da bancada
        ├── BancadaRevisaoTests.cs       # Testes de sincronização, timeouts e concorrência
        ├── CalculadoraHexTests.cs       # Testes de conversão 64 bits e bases
        ├── CalculadoraHexEntradaTests.cs # Validações de entrada, hífens e sinais
        ├── PomodoroEngineTests.cs       # Testes determinísticos com FakeTimeProvider
        ├── SupervisorioSerialEngineTests.cs # Testes de resiliência serial e watchdog
        ├── QaBancada3cTests.cs          # Validação do contrato textual e desfechos com o firmware
        └── QaBordasTests.cs             # Casos de borda de QA e resiliência de API
```

### Regras Fundamentais da Arquitetura:
1. **`DevSandbox.Core` NUNCA referencia bibliotecas visuais:** É expressamente proibida a importação de `System.Windows.Forms` ou `System.Drawing` dentro do Core. O Core é uma biblioteca pura, portável para qualquer ambiente .NET (Linux, Android, Web).
2. **Inversão de Dependências e Abstrações de I/O:** Nenhum componente de domínio depende diretamente de classes concretas de hardware (como `System.IO.Ports.SerialPort` ou `System.Diagnostics.Stopwatch`). Motores e serviços utilizam interfaces (`ISerialEngine`, `ISerialPort`) e abstrações temporais (`TimeProvider`), permitindo injeção de dublês em testes.
3. **Casca Visual Fina (*Thin UI*):** Formulários não processam regras de negócio, não implementam parsing numérico complexo e não gerenciam threads de I/O diretamente; apenas apresentam estados e repassam eventos aos motores.
4. **Resiliência e Retorno Explícito de Transmissão:** Operações de escrita em portas externas retornam confirmação booleana e emitem eventos contextuais (`ComandoConcluido`) com desfechos mapeados (`Confirmado`, `NaoEnviado`, `SemResposta`, `ErroFirmware`).

---

## 🧩 3. Detalhamento dos Módulos Implementados

### 3.1. ⚡ Emulador de Bancada EVSE (Modo 3 - IEC 61851-1 / IEC 62196-2)
* **11 Estados do Control Pilot (CP):**
  * O enum `EstadoVeiculoCP` modela fielmente os 11 estados normalizados:
    * **Família AX:** `AX` (12V - Desconectado).
    * **Família BX:** `BXMais` (+9,56V a +8,44V, nominal ~9V), `BX` (nominal 2740 Ω), `BXMenos` (+8,56V a +7,44V).
    * **Família CX:** `CXMais` (+6,59V a +5,41V, nominal ~6V), `CX` (nominal 882 Ω), `CXMenos` (+5,59V a +4,41V).
    * **Família DX:** `DXMais` (+3,59V a +2,41V, nominal ~3V), `DX` (nominal 246 Ω), `DXMenos` (+2,59V a +1,41V).
    * **Família E:** `E` (~1 V - Falha: 120 Ω do CP ao PE).
  * *Observação arquitetural:* O Estado F (-12V) foi expressamente descartado por decisão de projeto, pois reflete uma condição gerada pela estação de recarga, não sendo simulado pelo veículo.
* **Protocolo Textual e Contrato com Firmware:**
  * Comandos de CP: `estado <ax|bx+|bx|bx-|cx+|cx|cx-|dx+|dx|dx-|e|status>` (com atalhos `a`, `b`, `c`, `d`).
  * Comandos de PP: `cabo <0..6>`.
  * Comandos de diagnóstico: `debug stack` (avalia o *High Water Mark* da pilha FreeRTOS).
  * Respostas padronizadas do firmware: `OK` para sucesso e `ERRO: <mensagem>` para falhas ou parâmetros inválidos.
  * Canal 1 do PP (4700 Ω): Mantido como resistor de bancada para calibração interna, devidamente documentado como "fora da norma IEC 62196-2".
* **Motor da Bancada (`BancadaEngine.cs`):**
  * Recebe `ISerialEngine` injetável, operando desacoplado da porta real.
  * **Gerenciamento de Transações e Respostas (`RespostaFirmware`, `ConclusaoComando`):** Comandos enviados entram em uma fila FIFO com identificador incremental (`Id`). A classe monitora as respostas seriais com prazo de atividade de 4 segundos, descartando mensagens órfãs e notificando o evento `ComandoConcluido` com os desfechos `Confirmado`, `NaoEnviado`, `SemResposta` ou `ErroFirmware`.
  * **Sincronização na Conexão:** Ao abrir a porta, envia `estado status` para sincronizar o estado real do microcontrolador, incluindo mecanismo de nova tentativa após o reset elétrico provocado pelo pulso DTR.
* **Interface Visual (`FormBancada.cs`):**
  * **Seleção em Dois Níveis:** Painel superior para escolha da família do estado (`AX`, `BX`, `CX`, `DX`, `E`) e painel secundário para o nível de tolerância (`Sup (+)`, `Nom`, `Inf (-)`).
  * **Linha de Status com Confirmação do Firmware:** Exibe o estado confirmado pela placa (ex.: `Firmware: BX+ (+10 V)`), complementado por indicadores coloridos do último comando despachado.
  * Botão **Status** para consulta manual do estado dos relés e terminal com limite circular de 50.000 caracteres.

---

### 3.2. 🔌 Supervisório Serial (UART / COM)
* **Abstrações e Contratos (`ISerialEngine.cs`, `ISerialPort.cs`, `SistemaSerialPort.cs`):**
  * `ISerialEngine`: Contrato de alto nível (propriedades `EstaConectado`, `TentandoReconectar`, eventos `DadosRecebidos`, `ConexaoPerdida`, `Reconectado`, métodos de conexão, desconexão e transmissão `EnviarDados`).
  * `ISerialPort`: Abstração mínima da porta serial física (`Open`, `Close`, `Write`, `WriteLine`, `ReadExisting`, `DataReceived`), permitindo simulação de falhas de E/S sem hardware.
  * `SistemaSerialPort`: Adaptador sobre `System.IO.Ports.SerialPort` com timeouts configurados para 1000 ms.
* **Motor Resiliente (`SupervisorioSerialEngine.cs`):**
  * **Concorrência e Prevenção de Deadlocks:** Disparo de eventos estritamente fora de blocos `lock` para evitar inversão de prioridade/deadlock com `Control.Invoke` da UI.
  * **Desacoplamento de Locks na Recepção:** `PortaSerial_DataReceived` não adquire a trava interna `_lock`; falhas de leitura são sinalizadas para resolução segura no ciclo do watchdog.
  * **Watchdog Inteligente:** Monitora a presença da porta serial. Para suportar portas COM virtuais de drivers emulados que não gravam em `HARDWARE\DEVICEMAP\SERIALCOMM`, a ausência em `GetPortNames()` só é tratada como desconexão se a porta constava na lista no momento da conexão inicial.
  * **Transmissão Segura (`EnviarDados`):** Retorna `bool`. Em caso de erro de E/S, fecha a porta, dispara `ConexaoPerdida` e aciona a reconexão automática sem quebrar a UI.
* **Interface Visual (`FormSupervisorio.cs`):**
  * **Recepção em Lotes (50 ms):** Dados recebidos são acumulados em buffer rápido e renderizados na tela a cada 50 ms por um timer WinForms, eliminando travamentos de UI.
  * Histórico limitado a 50.000 caracteres e registro explícito de falhas de envio ("✖ Não enviado").

---

### 3.3. ⏱️ Pomodoro Timer
* **Motor Baseado em `TimeProvider` (`PomodoroEngine.cs`):**
  * Utiliza `TimeProvider` injetável (`System.TimeProvider`), permitindo testes determinísticos e rápidos via `FakeTimeProvider`.
  * **Troca Atômica de Ciclos:** Substituição do `CancellationTokenSource` sob trava protegida, eliminando concorrência entre ciclos anteriores e novos.
  * **Fechamento Preciso:** Garante disparo do tick `00:00` antes da transição para o estado `Parado`.
  * **Isolamento de Assinantes:** Iteração individual por `GetInvocationList()`, garantindo que exceções em manipuladores externos não derrubem o motor.
* **Interface Visual (`FormPomodoro.cs`):**
  * Notificação não bloqueante ao concluir o ciclo: emite alerta sonoro (`SystemSounds.Asterisk`) e aciona a API nativa `FlashWindowEx` via P/Invoke para piscar o aplicativo na barra de tarefas caso esteja em segundo plano, sem utilizar caixas modais `MessageBox`.

---

### 3.4. 🔢 Conversor de Bases Numéricas (Calculadora Hex)
* **Motor de Conversão e Parsing (`CalculadoraHex.cs`, `BaseNumerica.cs`):**
  * Conversões bidirecionais de 64 bits (`ulong`/`long` em complemento de dois).
  * **Detecção Inteligente no Core (`DetectarBase` / `ConverterEntrada`):** Prefixo `0x`/`0X` indica hexadecimal; `0b`/`0B` seguido de bits indica binário; presença de caracteres hexadecimais (`A–F`, `a–f`) indica hexadecimal; demais entradas são processadas como decimal.
  * **Convenções Estritas de Validação:**
    * Separadores livres: espaços, tabulações e sublinhados (`_`).
    * Hífens (`-`): permitidos **exclusivamente como separadores entre dígitos** (ex.: `FF-FF`, `1100-0011`).
    * Rejeição de Sinal Negativo: Números negativos lançam `FormatException` com mensagem amigável.
    * Entradas sem dígitos válidos lançam `ArgumentException`.
* **Interface Visual (`FormCalculadora.cs`):**
  * Apresentação binária com quebra automática em duas linhas de 32 bits (8 nibbles por linha) para números de 64 bits.
  * Botões de cópia rápida com debounce via `HashSet<FluentButton>` para evitar reentrância visual durante o feedback "✓ Copiado!".

---

### 3.5. 🎨 Sistema Visual Windows 11 Fluent Design (WinUI 3 Dark)
* **Gerenciador de Temas (`ThemeManager.cs`):**
  * Ativação de barra de título nativa escura via DWM (`dwmapi.dll`).
  * **Cache Estático de Fontes GDI+:** Fontes compartilhadas (`FontePadrao`, `FonteBotao`, `FonteEntrada`, `FonteTerminal`, `FonteSubtitulo`) instanciadas uma única vez para mitigar consumo de handles GDI do sistema operacional.
  * **Navegação por Teclado:** Método `DesenharFoco(Graphics g, Rectangle area, int raio)` que renderiza um anel interno de foco de alto contraste no padrão Fluent quando um controle customizado recebe foco por Tab/teclado.

---

## 🔬 4. Firmware e Simulação da Bancada

### 4.1. Firmware Embarcado (`firmware/Bancada/`)
* **Stack:** C++ para microcontroladores AVR com PlatformIO e FreeRTOS.
* **Hardware Alvo:**
  * **ATmega2560 (Arduino Mega):** Suporte integral aos relés do CP (S7–S12) e PP (S1–S6).
  * **ATmega328P (Arduino Nano):** Suporte exclusivo aos relés do PP (responde `ERRO` para comandos de CP).
* **Segurança Operacional:**
  * **Break-Before-Make por Interseção:** Desliga apenas os relés que não fazem parte do novo estado, aguarda uma pausa mínima de 20 ms (somente quando há relés saindo e novos relés entrando), e liga os relés necessários. Trocas que apenas abrem ou apenas fecham relés ocorrem sem atraso desnecessário.
  * **Boot Seguro:** Na inicialização, todos os relés são desenergizados para evitar configurações indesejadas no circuito elétrico.
  * **Reaplicação de Estado:** Reenviar o mesmo estado é tratado como *no-op*. Para forçar um desligamento temporário, comanda-se `estado ax` antes do estado pretendido.
* **Manual Técnico:** Documentação detalhada disponível em [firmware/Bancada/README.md](firmware/Bancada/README.md).

### 4.2. Simulação em Circuito Virtual (`simulacao/Bancada/`)
* **Simulador:** SimulIDE 2.0.
* **Circuito:** `simulacao/Bancada/circuito_bancada_mega.sim1`, configurado para o microcontrolador Arduino Mega 2560, relés do CP e PP, voltímetros de aferição e interface serial COM virtual.
* **Integração de Portas Seriais Virtuais:**
  * Configurado com par virtual **COM1 ↔ COM2** no **HHD Virtual Serial Port Tools**.
  * SimulIDE conectado na **COM1** (9600 bps).
  * `DevSandbox.UI` conectado na **COM2** (9600 bps).
* **Guia Operacional:** Consulte o passo a passo completo em [simulacao/Bancada/GUIA_SIMULACAO_BANCADA.md](simulacao/Bancada/GUIA_SIMULACAO_BANCADA.md).

---

## 📦 5. Estratégia de Build e Distribuição

* **Configuração Centralizada (`Directory.Build.props`):**
  * Todos os projetos da solução herdam `TreatWarningsAsErrors=true` e `AnalysisLevel=latest-recommended`.
  * A regra `CopyToDist` está configurada para atuar exclusivamente na configuração `Release`, evitando poluição da raiz em compilações `Debug`.
* **Geração de Executável Standalone (`publish-standalone.ps1`):**
  * Compila `DevSandbox.UI` em modo Release autocontido (Single-File) para Windows x64.
  * O binário executável final de ~49 MB é gravado em `dist/standalone/DevSandbox.UI.exe`.
* **Distribuição Oficial (GitHub Releases):**
  * A pasta `app/` foi removida do rastreamento do repositório Git e incluída no `.gitignore`.
  * A partir da versão **`v1.0.0`**, as distribuições oficiais dos binários são realizadas anexando os executáveis standalone diretamente aos lançamentos do GitHub vinculados a tags de versão semântica.

---

## 🧪 6. Status Atual de Qualidade

* **Testes Automatizados:** **345 testes aprovados (0 falhas)** executados com NUnit 4 sob .NET 10.0.
* **Compilação:** 0 erros e 0 avisos em toda a solução, cumprindo a diretiva estrita de `TreatWarningsAsErrors`.
* **Cobertura de Código:** Cerca de **85% de cobertura de linhas** no `DevSandbox.Core` aferida via relatório Cobertura XML.
* **Validação Textual:** Cobertura integral da matriz de comandos e respostas do firmware com testes dedicados em `QaBancada3cTests.cs`, `BancadaCPTests.cs` e `BancadaRevisaoTests.cs`.
