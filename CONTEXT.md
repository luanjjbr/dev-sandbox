# 🧠 CONTEXT.md — Contexto do Projeto dev-sandbox

Documento de contexto técnico, arquitetural e operacional do repositório **dev-sandbox**. Criado para fornecer a desenvolvedores e agentes de IA uma visão aprofundada da arquitetura, convenções, módulos implementados, status de testes e diretrizes de evolução do projeto na versão **v1.2.1**.

---

## 📌 1. Visão Geral

* **Repositório:** `dev-sandbox`
* **Versão Vigente:** `v1.2.1`
* **Finalidade:** Laboratório pessoal de desenvolvimento em C# e .NET para prototipagem rápida, utilitários práticos de engenharia e experimentação de padrões arquiteturais modernos.
* **Stack Principal:**
  * **Linguagem / Runtime:** C# 13+ / .NET 10.0 (`net10.0` para Core/Testes e `net10.0-windows` para UI).
  * **Padrões de Qualidade Global:** `Directory.Build.props` com compilação estrita (`TreatWarningsAsErrors=true`), nulabilidade ativa (`Nullable=enable`) e analisadores de código de última geração (`EnableNETAnalyzers=true`, `AnalysisLevel=latest-recommended`).
  * **Interface Gráfica:** Visual Nordic (claro e escuro) em janela única com abas integradas (`Forms/MainForm.cs` e `Telas/*.cs`), tokens centralizados no `ThemeManager` e controles GDI+ customizados desenhados 100% em código puro (sem arquivos `.Designer.cs`).
  * **Ícone Oficial:** `src/DevSandbox.UI/app.ico` (símbolo `>_` branco em vetor sobre quadrado Dark Slate), embutido no `.csproj` como `ApplicationIcon` para o executável no Windows Explorer e incorporado ao cabeçalho do `MainForm`.
  * **Comunicação Serial:** `System.IO.Ports` com contratos desacoplados (`ISerialEngine`, `ISerialPort`, `SistemaSerialPort`, `MontadorLinhasSerial`, `HistoricoComandos`, `FimDeLinha`).
  * **Abstração Temporal:** `System.TimeProvider` nativo do .NET com suporte a testes determinísticos sem atrasos reais (`Microsoft.Extensions.TimeProvider.Testing`).
  * **Testes Automatizados:** 607 testes aprovados (0 falhas) executados com NUnit 4 sob .NET 10 (`NUnit`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk`).
  * **Releases e Distribuição:** Releases oficiais **v1.2.0** e **v1.2.1** publicadas no GitHub (a versão v1.2.1 traz o ícone oficial integrado). Tags `v1.0.0` e `v1.1.0` preservadas no histórico Git.
  * **Solução:** Formato de solução unificada XML do Visual Studio (`dev-sandbox.slnx`).
  * **Firmware e Simulação:** PlatformIO, FreeRTOS, SimulIDE 2.0 e HHD Virtual Serial Port Tools.

---

## 🏛️ 2. Arquitetura da Solução

O projeto segue estritamente a separação de responsabilidades em camadas desacopladas, com foco em testabilidade sem dependência de hardware físico ou de subsistemas visuais:

```plaintext
dev-sandbox/
├── dev-sandbox.slnx                     # Arquivo de solução unificada (Solution XML)
├── Directory.Build.props                # Regras corporativas globais (avisos como erro, analisadores)
├── README.md                            # Guia rápido e apresentação do projeto
├── CONTEXT.md                           # Memória técnica e contexto arquitetural completo (v1.2.1)
├── publish-standalone.ps1               # Script para gerar executável único standalone
├── dist/                                # Compilações locais e saídas do publish
│   └── standalone/                      # Diretório de saída do executável único (~49 MB)
├── firmware/                            # Firmware embarcado da bancada de testes EVSE
│   └── Bancada/                         # Projeto PlatformIO + FreeRTOS (foco no ATmega2560)
├── simulacao/                           # Arquivos de simulação virtual da bancada
│   └── Bancada/                         # Esquemático SimulIDE 2.0 (Mega) e circuitos de bancada
├── src/
│   ├── DevSandbox.Core/                 # [Camada de Domínio e Regras de Negócio - net10.0 pura]
│   │   ├── DevSandbox.Core.csproj       # Configuração do projeto da biblioteca de domínio
│   │   ├── Bancada/                     # BancadaEngine, ConclusaoComando, EstadoCaboPP, EstadoVeiculoCP,
│   │   │                                # RelesCP, RespostaFirmware, RespostaMultilinha, StackTasks
│   │   ├── Converters/                  # CalculadoraHex, BaseNumerica, OperacoesBits
│   │   ├── Pomodoro/                    # PomodoroEngine, EstadoPomodoro, EstatisticasSessao, TransicaoPomodoro
│   │   └── Serial/                      # ISerialEngine, ISerialPort, SistemaSerialPort, SupervisorioSerialEngine,
│   │                                    # FimDeLinha, HistoricoComandos, MontadorLinhasSerial
│   │
│   └── DevSandbox.UI/                   # [Camada de Apresentação WinForms Nordic - net10.0-windows]
│       ├── app.ico                      # Ícone oficial (>_ branco sobre quadrado Dark Slate)
│       ├── DevSandbox.UI.csproj         # Configuração de build da UI (ApplicationIcon e EmbeddedResource)
│       ├── Program.cs                   # Ponto de entrada STAThread (DPI awareness e tema)
│       ├── Common/                      # Design System Nordic em GDI+ puro (zero .Designer.cs):
│       │                                # ThemeManager, PreferenciaTema, BarraAbas, BitMatrixControl
│       │                                # [inclui ProgressRingControl], BotaoNordic, CaixaTextoNordic,
│       │                                # CardNordic, ControleNordic, Icones, Indicadores [Leds, LedNordic,
│       │                                # ChaveNordic, CartaoEstado, BlocoRele, LeituraTelemetria, BotaoTema],
│       │                                # SeletorNordic, SeletorSegmentado, TerminalView, Toast
│       ├── Forms/                       # MainForm (janela única com cabeçalho, abas e hospedagem de telas)
│       └── Telas/                       # Telas desacopladas herdando de TelaBase:
│                                        # TelaBase, TelaInicio, TelaBancada, TelaSupervisorio,
│                                        # TelaCalculadora, TelaPomodoro
│
└── tests/
    └── DevSandbox.Tests/                # [Camada de Testes Automatizados - NUnit 4 (607 testes)]
        ├── DevSandbox.Tests.csproj      # Configuração da suíte de testes
        ├── Fakes/                       # Dublês de teste: FakeSerialPort, FakeSerialEngine,
        │                                # SupervisorioTesteExtensions
        ├── BancadaCPTests.cs            # Testes da máquina de estados CP (11 estados) e tolerâncias
        ├── BancadaEngineTests.cs        # Testes de comando, eventos e ciclo da bancada
        ├── BancadaRevisaoTests.cs       # Testes de sincronização, timeouts e concorrência
        ├── BancadaV2CoreTests.cs        # Testes de comandos manuais e contratos da Bancada v2
        ├── CalculadoraHexEntradaTests.cs # Validações de entrada, hífens, sinais e bases
        ├── CalculadoraHexTests.cs       # Testes de conversão 64 bits e operações de base
        ├── CorrenteMaximaCaboTests.cs   # Testes da corrente máxima nominal do cabo PP
        ├── HistoricoComandosTests.cs    # Testes de histórico serial (navegação ↑/↓ e rascunho)
        ├── MontadorLinhasSerialTests.cs # Testes do agregador de frames UART até '\n'
        ├── NordicCoreTests.cs           # Testes de lógica e comportamento dos componentes Nordic
        ├── PomodoroEngineTests.cs       # Testes determinísticos com FakeTimeProvider
        ├── QaBancada3cTests.cs          # Validação de contrato textual e desfechos com o firmware
        ├── QaBordasTests.cs             # Casos de borda de QA e resiliência de API
        ├── QaCorrecoesTests.cs          # Testes de regressão de correções identificadas
        ├── QaNordicBordasTests.cs       # 95 testes específicos de robustez e limites da suíte Nordic
        └── SupervisorioSerialEngineTests.cs # Testes de resiliência serial, reconexão e watchdog
```

### Regras Fundamentais da Arquitetura:

1. **`DevSandbox.Core` NUNCA referencia bibliotecas visuais:** É expressamente proibida a importação de `System.Windows.Forms` ou `System.Drawing` dentro do Core. O Core é uma biblioteca pura, portável para qualquer ambiente .NET (Linux, Android, Web, Contêineres).
2. **Inversão de Dependências e Abstrações de I/O:** Nenhum componente de domínio depende diretamente de classes concretas de hardware (como `System.IO.Ports.SerialPort` ou `System.Diagnostics.Stopwatch`). Motores e serviços utilizam interfaces (`ISerialEngine`, `ISerialPort`) e abstrações temporais (`TimeProvider`), permitindo injeção de dublês determinísticos em testes.
3. **Casca Visual Fina em Código Puro (*Code-First Thin UI*):** Todos os componentes visuais e formulários são desenhados diretamente em código C# utilizando GDI+ e a infraestrutura de `ControleNordic`, eliminando 100% dos arquivos de designer WinForms (`.Designer.cs`). Isso previne código gerado instável, garante controle total sobre handles GDI, redimensionamento, escalas DPI e acessibilidade.
4. **Tema Dinâmico Centralizado e Persistido:** O `ThemeManager` atua como fonte única da verdade para tokens de cores dos modos Claro (*Nordic Clean*) e Escuro (*Slate Dark*). A preferência do usuário é persistida em `%AppData%\DevSandbox\tema.json` (com detecção automática via `AppsUseLightTheme` na primeira execução) e as mudanças propagam-se dinamicamente por evento (`TemaMudou`) sem recriar controles.
5. **Janela Única com Ciclo de Vida Desacoplado:** O `MainForm` instancia as telas derivadas de `TelaBase` uma única vez no carregamento, alternando apenas a visibilidade via abas (`BarraAbas`). Essa abordagem mantém motores e conexões ativas (como portas seriais no Supervisório e na Bancada) operando sem interrupção ao alternar entre telas.
6. **Resiliência e Retorno Explícito de Transmissão:** Operações de escrita em portas externas retornam confirmação booleana e emitem eventos contextuais (`ComandoConcluido`) com desfechos estritamente mapeados (`Confirmado`, `NaoEnviado`, `SemResposta`, `ErroFirmware`).

---

## 🧩 3. Detalhamento dos Módulos Implementados

### 3.1. ⚡ Emulador de Bancada EVSE (Modo 3 - IEC 61851-1 / IEC 62196-2)

* **11 Estados do Control Pilot (CP):**
  * O enum `EstadoVeiculoCP` modela fielmente os 11 estados normalizados:
    * **Família AX:** `AX` (12 V - Desconectado).
    * **Família BX:** `BXMais` (+9,56 V a +8,44 V, nominal ~9 V), `BX` (nominal 2740 Ω), `BXMenos` (+8,56 V a +7,44 V).
    * **Família CX:** `CXMais` (+6,59 V a +5,41 V, nominal ~6 V), `CX` (nominal 882 Ω), `CXMenos` (+5,59 V a +4,41 V).
    * **Família DX:** `DXMais` (+3,59 V a +2,41 V, nominal ~3 V), `DX` (nominal 246 Ω), `DXMenos` (+2,59 V a +1,41 V).
    * **Família E:** `E` (~1 V - Falha: 120 Ω do CP ao PE).
  * *Observação arquitetural:* O Estado F (-12 V) foi expressamente descartado por decisão de projeto, pois reflete uma condição gerada pela estação de recarga, não sendo simulado pelo veículo.
* **Resistor do Pino de Proximidade (PP - Proximity Pilot):**
  * O enum `EstadoCaboPP` modela os canais 0 a 6: `CircuitoAberto` (0), `Desconectado` (1 - 4700 Ω, resistor interno da bancada para calibração, documentado como fora da tabela IEC 62196-2), `Cabo13A` (2 - 1500 Ω), `Cabo20A` (3 - 680 Ω), `Cabo32A` (4 - 220 Ω), `Cabo63A` (5 - 100 Ω) e `FalhaCurto` (6 - 47 Ω).
  * O método de extensão `EstadoCaboPPExtensions.ObterCorrenteMaxima()` extrai a capacidade nominal de condução em amperes (13 A, 20 A, 32 A, 63 A), retornando `null` para estados não operacionais (aberto, desconectado ou curto).
* **Matriz de Relés CP (`RelesCP.cs`):**
  * O circuito comuta 10 relés (RLY1 a RLY10) acionados pelos pinos digitais D22 a D31 do Arduino Mega:
    * **D30 (RLY6 / S7):** Habilitação geral de PE (conecta o diodo do veículo ao aterramento).
    * **D31 (RLY1 / S8):** Curto-circuito CP-PE antes do diodo (exclusivo do Estado E).
    * **D29 (RLY7 / S9.1):** Habilitação do ramo resistivo do estado B (+9 V).
    * **D28 (RLY2 / S9.2):** Seleção de tolerância do estado B.
    * **D27 (RLY5 / S10_C):** Habilitação do ramo paralelo dos estados C e D.
    * **D26 (RLY8 / S10_D):** Comutação entre o ramo C e o ramo D.
    * **D25 (RLY3 / S11.1):** Habilitação do resistor paralelo do estado C (+6 V).
    * **D24 (RLY4 / S11.2):** Seleção de tolerância do estado C.
    * **D23 (RLY9 / S12.1):** Habilitação do resistor paralelo do estado D (+3 V).
    * **D22 (RLY10 / S12.2):** Seleção de tolerância do estado D.
  * *Controle Estrito:* A exibição da matriz de relés é **somente leitura confirmada pelo firmware** (alimentada por `RelesConfirmados` via `[STATUS CP]`), sem permitir acionamento manual ou override local que possa desalinhar o estado físico dos relés.
* **Protocolo Textual e Contrato com Firmware:**
  * Comandos de CP: `estado <ax|bx+|bx|bx-|cx+|cx|cx-|dx+|dx|dx-|e|status>` (atalhos: `a`, `b`, `c`, `d`).
  * Comandos de PP: `cabo <0..6>` e `cabo status`.
  * Comandos de diagnóstico: `debug stack` (avalia o *High Water Mark* da pilha FreeRTOS) e `help`.
  * Respostas padronizadas: `OK`, `ERRO: <mensagem>`, `[STATUS CP ...]` e `[STATUS CABO ...]`.
* **Motor da Bancada (`BancadaEngine.cs`):**
  * Recebe `ISerialEngine` injetável, operando desacoplado da porta real.
  * **Gerenciamento de Transações FIFO (`RespostaFirmware`, `ConclusaoComando`):** Comandos com confirmação esperada entram em fila FIFO com identificador sequencial `Id`. O motor monitora prazos de 4 segundos renovados por atividade da porta serial, consome respostas tardias como órfãs e emite o evento `ComandoConcluido` com desfechos mapeados.
  * **Sincronização na Conexão:** Ao conectar, envia `estado status`. Como o pulso elétrico DTR reinicia placas Arduino Mega, o motor inclui retentativa automática após 2,5 segundos caso o primeiro pedido de status não seja respondido.
  * **Envio de Comandos Manuais (`EnviarComandoManual`):** Aceita `string comando` e terminador flexível `FimDeLinha` (padrão LF). Valida via `ComandoManualValido` se a entrada contém exclusivamente caracteres ASCII imprimíveis (espaço a `~` e tabulação). Caracteres inválidos (acentos, cedilha, controle) são rejeitados imediatamente no Core com `ResultadoComando.NaoEnviado` e o motivo `DetalheCaractereInvalido`. Suporta envio de comando vazio apenas com o terminador (útil para wake-up de terminais).
  * **Respostas Multilinhas (`RespostaMultilinha.cs`):** Agrupa comandos como `help` e `debug stack` que retornam múltiplas linhas sem `OK`, consolidando o bloco após uma janela de silêncio de 300 ms (`JanelaSilencioMultilinhaPadrao`).
* **Interface Visual (`TelaBancada.cs`):**
  * **Layout Funcional em 3 Colunas + Terminal em Largura Total:**
    * **HUD de Telemetria Superior:** 4 blocos de leitura em destaque: Tensão nominal com limites de tolerância do CP (ex.: `+9.0 V (8.44 a 9.56 V)`), Corrente máxima do cabo PP (ex.: `32 A` via `ObterCorrenteMaxima`), Resistência nominal calculada e Indicador de Sincronismo real com a placa (**CONFIRMADO** em verde ou **PENDENTE** em laranja).
    * **Coluna CP:** Seleção em dois níveis via cartões estilizados de estados (famílias A, B, C, D, E) complementados por seletor segmentado de tolerância (`Superior [+]`, `Nominal`, `Inferior [-]`).
    * **Coluna PP:** Cartões individuais para canais 0 a 6 com indicação da corrente suportada e aviso do canal 1 fora da norma.
    * **Coluna de Relés:** Matriz física D22–D31 com blocos visuais abertos/fechados e tooltips descritivos da função de cada relé. Inclui botão de ação rápida **"Reset CP (A)"** que redefine o veículo para AX com segurança mantendo a seleção do cabo PP.
    * **Terminal Integrado:** Painel inferior em largura total contendo barra de conexão serial (portas, baud rate, botão conectar com LED e status), terminal customizado `TerminalView` com scrollbar Nordic, botão de comando manual, seletor de `FimDeLinha`, histórico de comandos (setas ↑/↓) e alternador de autoscroll (*Acompanhar o final*).

---

### 3.2. 🔌 Supervisório Serial (UART / COM)

* **Abstrações e Contratos (`ISerialEngine.cs`, `ISerialPort.cs`, `SistemaSerialPort.cs`):**
  * `ISerialEngine`: Contrato de alto nível desacoplado. Expõe propriedades `EstaConectado`, `TentandoReconectar`, eventos `DadosRecebidos`, `ConexaoPerdida`, `Reconectado`, e transmissão segura:
    * `bool EnviarDados(string texto, FimDeLinha fimDeLinha)`: Transmite o texto seguido do terminador configurado. Suporta envio de comandos vazios apenas com o terminador.
    * Sobrecarga de conveniência `bool EnviarDados(string texto, bool adicionarNovaLinha = true)`.
  * `ISerialPort`: Abstração de baixo nível da porta serial física (`Open`, `Close`, `Write`, `WriteLine`, `ReadExisting`, `DataReceived`), permitindo injeção de dublês (`FakeSerialPort`) e simulação de falhas sem hardware físico.
  * `SistemaSerialPort`: Implementação concreta sobre `System.IO.Ports.SerialPort` com timeouts de leitura e escrita padronizados em 1000 ms.
* **Terminadores de Transmissão (`FimDeLinha.cs`):**
  * Enumeração padronizada: `Nenhum` (""), `CR` (`\r`), `LF` (`\n` - padrão do firmware) e `CRLF` (`\r\n`).
  * Métodos de extensão `ObterTerminador()` e `ObterRotulo()` ("—", "CR", "LF", "CRLF") para exibição limpa em logs de transmissão.
* **Histórico de Comandos Estilo Shell (`HistoricoComandos.cs`):**
  * Gerenciador em memória para até 100 entradas exclusivas acessíveis por setas (↑/↓).
  * **Preservação de Rascunho:** Na primeira pressão de ↑, salva o texto que estava sendo digitado na caixa. Ao descer com ↓ além do comando mais recente, o rascunho é restaurado perfeitamente.
  * Ignora comandos vazios ou repetições consecutivas idênticas.
* **Montador de Linhas Seriais (`MontadorLinhasSerial.cs`):**
  * Bufferizador thread-safe de recepção UART. Monta os fragmentos recebidos do driver USB/Windows em linhas completas delimitadas por `\n`.
  * Entrega antecipada por ociosidade: caso dados parciais fiquem sem `\n` por mais de 100 ms (`TempoOciosoParaEntrega`), descarrega o buffer pendente para a UI.
  * Proteção de memória contra estouro de buffer (limite de 4096 caracteres) e prevenção de linhas vazias espúrias na quebra de sequências `\r\n`.
* **Motor Resiliente (`SupervisorioSerialEngine.cs`):**
  * **Concorrência Segura:** Disparo de eventos estritamente fora de blocos `lock`, prevenindo deadlocks com a fila de mensagens e `Control.Invoke` da UI.
  * **Recepção Desacoplada:** Manipulador `PortaSerial_DataReceived` não retém travas internas; falhas de hardware sinalizam o watchdog.
  * **Watchdog Inteligente:** Monitora a presença física da porta COM. Para contornar limitações de drivers de portas COM virtuais que não registram em `HARDWARE\DEVICEMAP\SERIALCOMM`, a ausência em `GetPortNames()` só ativa perda de conexão se a porta constava na listagem no momento em que foi aberta.
  * **Reconexão Automática Silenciosa:** Reconecta automaticamente em background caso o dispositivo seja desconectado e reinserido.
* **Interface Visual (`TelaSupervisorio.cs`):**
  * **Terminal Desacoplado a 50 ms:** Dados recebidos da serial entram em buffer rápido e são descarregados na tela em lotes a cada 50 ms por timer WinForms, eliminando congelamentos de interface sob tráfego intenso.
  * Terminal com altura fixa e limite de buffer de 50.000 caracteres com scrollbar customizada Nordic.
  * Seletor de `FimDeLinha` (`SeletorNordic`) com padrão em LF.
  * Caixa de envio integrada com `HistoricoComandos`: texto permanece preservado no campo após o envio para facilitar retransmissões repetidas.
  * Exibição explícita de falhas de envio com etiqueta visual ("✖ Não enviado").

---

### 3.3. ⏱️ Pomodoro Timer

* **Motor Baseado em `TimeProvider` (`PomodoroEngine.cs`):**
  * Utiliza a abstração temporal nativa `System.TimeProvider`, viabilizando testes unitários determinísticos instantâneos via `FakeTimeProvider` (sem necessidade de esperas reais de minutos).
  * **Troca Atômica de Ciclos:** O `CancellationTokenSource` é renovado sob `lock`, eliminando condições de corrida entre tarefas assíncronas de ciclos anteriores e novas inicializações.
  * **Fechamento Preciso:** Garante a notificação do tick final `00:00` antes da transição formal para o estado `Parado`.
  * **Propriedade `Progresso`:** Expõe a fração restante do ciclo (`TempoRestante / TempoTotal`, variando de 1.0 a 0.0), alimentando diretamente o anel visual de progresso e mantendo o valor estático durante pausas.
  * **Eventos com Isolamento de Falhas:** `OnTick`, `OnEstadoMudou` e `OnTransicao` iteram por `GetInvocationList()`, garantindo que eventuais exceções em assinantes não derrubem o laço temporal do motor.
* **Modelo de Estatísticas da Sessão (`EstatisticasSessao.cs`, `TransicaoPomodoro.cs`):**
  * Rastreia a sessão em memória de forma thread-safe com trava própria:
    * **Ciclos da Série (1 a 4):** Incrementa a cada foco concluído. Após a conclusão do 4º foco consecutivo, mantém a exibição em **4/4** durante o descanso longo até o início de um novo foco (quando reseta para 1/4).
    * **Tempo Acumulado em Foco:** Mede o tempo real decorrido em foco ativo com resolução de 50 ms (períodos de pausa não contam).
    * **Contagem de Interrupções:** Incrementa ao pausar ou interromper um foco em andamento (inclusive ao iniciar novo ciclo sobre um foco ativo).
    * Notificação através do evento `Mudou`.
* **Interface Visual (`TelaPomodoro.cs`):**
  * **Controle Circular `ProgressRingControl`:** Anel vetorial GDI+ de 10 px com extremidades arredondadas que esvazia em sincronia com `PomodoroEngine.Progresso`, exibindo o tempo restante e o estado no centro.
  * **Painel de Estatísticas Nordic:** Cards informativos com ciclo da série, tempo total em foco formatado e contagem de interrupções.
  * **Alertas e Notificações:** Emissão de aviso sonoro (`SystemSounds.Asterisk`), notificação não intrusiva via `Toast` na tela e alerta visual no Windows através de P/Invoke nativo `FlashWindowEx` para piscar a janela na barra de tarefas caso o aplicativo esteja minimizado ou em segundo plano.

---

### 3.4. 🔢 Conversor de Bases Numéricas e Calculadora 64 bits

* **Motor de Operações Bitwise de 64 bits (`OperacoesBits.cs`):**
  * Biblioteca pura no Core para manipulação de inteiros sem sinal de 64 bits (`ulong`):
    * `InverterBit(ulong valor, int bit)` e `LerBit(ulong valor, int bit)` (bits 0 = LSB a 63 = MSB).
    * `TrocarEndian(ulong valor)`: Inversão rápida da ordem dos 8 bytes (*byte swap*) via `BinaryPrimitives.ReverseEndianness`.
    * `DeslocarEsquerda(ulong valor)` (`valor << 1`) e `DeslocarDireita(ulong valor)` (`valor >> 1`).
    * `Not(ulong valor)`: Complemento bit a bit (`~valor`).
    * Constante `Maximo`: `ulong.MaxValue` (todos os 64 bits em 1).
    * `ParaAscii(ulong valor)`: Converte os 8 bytes em ordem *big-endian* para texto ASCII de 8 caracteres, substituindo bytes fora da faixa imprimível (0x20 a 0x7E) pelo caractere ponto (`.`).
    * `ParaInt64(ulong valor)`: Interpreta o padrão de 64 bits como inteiro com sinal em complemento de dois (`unchecked((long)valor)`).
* **Motor de Conversão e Parsing (`CalculadoraHex.cs`, `BaseNumerica.cs`):**
  * Conversões bidirecionais entre Hexadecimal, Binário, Decimal sem sinal e Decimal com sinal.
  * **Detecção Automática de Bases:** Prefixo `0x`/`0X` para hexadecimal, `0b`/`0B` para binário, caracteres `A–F`/`a–f` para hexadecimal e demais entradas tratadas como decimal.
  * **Validação Rigorosa de Entrada:** Aceita separadores amigáveis (espaços, tabulações e sublinhados `_`). Hífens (`-`) são aceitos estritamente como separadores entre dígitos. Sinais negativos diretos são rejeitados na entrada lançando `FormatException` amigável.
* **Interface Visual (`TelaCalculadora.cs`):**
  * **Controle `BitMatrixControl`:** Matriz de 64 bits (4 linhas × 16 colunas) em HWND único. Exibe MSB [63] no topo à esquerda e LSB [0] embaixo à direita. Permite clique individual para alternar qualquer bit, navegação completa via setas do teclado e desenho de anel de foco.
  * **4 Vistas Sincronizadas em Tempo Real:** Hexadecimal, Decimal sem sinal (`UInt64`), Decimal com sinal em complemento de dois (`Int64`), e Representação ASCII (8 bytes).
  * **Barra de Ações Rápidas de Bits:** Botões Nordic para operações diretas (`NOT`, `Inverter Todos`, `Zerar`, `Swap Endian`, `SHL << 1`, `SHR >> 1`).
  * Botões de cópia rápida com *debounce* visual via `HashSet` para feedback "✓ Copiado!".

---

### 3.5. 🎨 Sistema Visual Nordic Claro e Escuro em Janela Única com Abas

* **Substituição da Arquitetura Fluent Antiga:** A versão v1.2.x consolidou a substituição da casca Fluent escura por um sistema de design **Nordic** moderno, com suporte nativo a temas Claro (*Nordic Clean*) e Escuro (*Slate Dark*) em janela única.
* **Janela Única Unificada (`MainForm.cs`):**
  * Barra de título escura/clara nativa do Windows via chamadas DWM (`dwmapi.dll`).
  * Cabeçalho integrado de 48 px com título, subtítulo, ícone e botão de alternância rápida de tema (`BotaoTema`).
  * `BarraAbas` horizontal com navegação por mouse e teclado via atalhos **Ctrl+Tab** (próxima aba) e **Ctrl+Shift+Tab** (aba anterior).
  * Painel hospedeiro (`_area`) mantendo as 5 telas criadas uma única vez (`TelaInicio`, `TelaSupervisorio`, `TelaBancada`, `TelaCalculadora`, `TelaPomodoro`), preservando motores, conexões seriais e estados durante trocas de aba.
* **Tokens e Gerenciamento Centralizado (`ThemeManager.cs`):**
  * Superfícies: `FundoApp`, `FundoCard`, `FundoInput`, `FundoHover`, `FundoMudo`, `FundoCabecalho`.
  * Bordas: `BordaSutil`, `BordaForte`, `BordaFoco`, `BordaErro` (`#EF4444`).
  * Textos: `TextoPrincipal`, `TextoSecundario`, `TextoMudo`, `TextoInverso`, `TextoSobreAcento`.
  * Acentos e Status: `Acento`, `AcentoHover`, `AcentoSuave`, `AcentoSecundario`, `Sucesso`, `Aviso`, `Perigo`.
  * Cache compartilhado de fontes GDI+ (`FonteUi`, `FonteUiForte`, `FonteRotulo`, `FonteMono`, `FonteRelogio`, etc.) e utilitários de escala DPI (`Px`).
  * Evento global `TemaMudou` para repintura síncrona de todos os controles ativos.
* **Persistência de Preferência (`PreferenciaTema.cs`):**
  * Armazena a preferência em `%AppData%\DevSandbox\tema.json`.
  * Na primeira execução (ausência do arquivo), detecta automaticamente a preferência do sistema operacional lendo `AppsUseLightTheme` no Registro do Windows (`HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize`).
  * Tratamento resiliente de exceções de permissão ou E/S.
* **Catálogo de Controles Nordic em GDI+ Puro (`DevSandbox.UI/Common/`):**
  * `ControleNordic`: Classe base com controle de DPI, tratamento de foco por teclado, estados de hover e acessibilidade.
  * `BotaoNordic`, `CaixaTextoNordic`, `CardNordic`, `BarraAbas`, `SeletorNordic`, `SeletorSegmentado`.
  * `BitMatrixControl`: Matriz interativa de 64 bits.
  * `ProgressRingControl`: Anel circular de progresso.
  * `TerminalView`: Visualizador de terminal com buffer circular de 50.000 caracteres, autoscroll e scrollbar customizada Nordic.
  * `Indicadores.cs`: `Leds` (renderização com brilho/halo), `LedNordic`, `ChaveNordic`, `CartaoEstado`, `BlocoRele`, `LeituraTelemetria`, `BotaoTema`.
  * `Toast`: Notificações flutuantes animadas.
  * `Icones.cs`: Desenho de ícones vetoriais diretamente em GDI+.

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
  * A regra `CopyToDist` está configurada para atuar exclusivamente na configuração `Release`, evitando poluição da pasta `dist/` em compilações `Debug` do dia a dia.
* **Geração de Executável Standalone (`publish-standalone.ps1`):**
  * Compila `DevSandbox.UI` em modo Release autocontido (*Single-File*) para Windows x64.
  * O binário executável final de ~49 MB é gravado em `dist/standalone/DevSandbox.UI.exe`, contendo o ícone oficial embutido (`app.ico`).
* **Distribuição Oficial (GitHub Releases):**
  * A pasta `app/` foi removida do rastreamento do repositório Git e incluída no `.gitignore`.
  * As distribuições oficiais dos binários são realizadas anexando os executáveis standalone diretamente aos lançamentos do GitHub:
    * **`v1.2.0`:** Lançamento oficial da interface Nordic e janela única com abas.
    * **`v1.2.1`:** Lançamento oficial com a incorporação do ícone definitivo da aplicação (`app.ico`).
    * Tags semânticas anteriores (`v1.0.0` e `v1.1.0`) preservadas no histórico do Git.

---

## 🧪 6. Status Atual de Qualidade

* **Testes Automatizados:** **607 testes aprovados (0 falhas)** executados com NUnit 4 sob .NET 10.0.
* **Compilação:** 0 erros e 0 avisos em toda a solução, cumprindo a diretiva estrita de `TreatWarningsAsErrors`.
* **Suíte de QA e Casos de Borda (`QaNordicBordasTests.cs`):** 95 testes dedicados cobrindo casos extremos de integração:
  * Comportamento estático e unicidade de rótulos de `FimDeLinha`.
  * Envio de dados, desconexão imediata e reconexão sob buffer serial no Supervisório.
  * Validações de limites ASCII em comandos manuais da Bancada e comportamento de comandos vazios.
  * Inversão, deslocamentos, *endianness* e conversão ASCII em operações puras de 64 bits (`OperacoesBits`).
  * Navegação bidirecional e preservação de rascunhos no histórico de comandos (`HistoricoComandos`).
  * Concorrência, trocas atômicas de ciclo e estatísticas de sessão no motor `PomodoroEngine`.
  * Mapeamento integral da tabela de corrente máxima de cabo PP (`ObterCorrenteMaxima`).
* **Cobertura de Código:** Superior a **85% de cobertura de linhas** no `DevSandbox.Core` aferida via relatórios Cobertura XML.
* **Validação de Contratos:** Cobertura integral da matriz de comandos e respostas do firmware com testes dedicados em `QaBancada3cTests.cs`, `BancadaCPTests.cs`, `BancadaRevisaoTests.cs` e `BancadaV2CoreTests.cs`.
