# 🧪 dev-sandbox

Repositório pessoal destinado a testes, protótipos e desenvolvimento de utilitários em C# e .NET 10. O projeto adota uma arquitetura em camadas desacoplada, permitindo que novas ferramentas, algoritmos e testes sejam adicionados de forma organizada, testável e escalável.

---

## 🏛️ Organização e Arquitetura do Projeto

A solução segue a convenção moderna para ecossistemas .NET, dividindo as responsabilidades em diretórios de alto nível: **`src/`** (código-fonte da aplicação), **`tests/`** (suítes de testes automatizados com NUnit), **`firmware/`** (código embarcado para microcontroladores AVR) e **`simulacao/`** (arquivos de esquemático e simulação em circuito virtual).

```plaintext
dev-sandbox/
├── dev-sandbox.slnx               # Arquivo de solução unificada (Visual Studio Solution XML)
├── Directory.Build.props          # Configuração global de build (TreatWarningsAsErrors, analisadores .NET)
├── README.md                      # Apresentação do projeto e guia rápido
├── CONTEXT.md                     # Documento de contexto técnico e arquitetural detalhado
├── publish-standalone.ps1         # Script para gerar executável único standalone
├── dist/                          # Pasta de compilação local (gerada automaticamente no build)
│   └── standalone/                # Destino dos binários gerados pelo script de publicação (~49 MB)
├── firmware/                      # Firmware embarcado da bancada de testes EVSE
│   └── Bancada/                   # Projeto PlatformIO + FreeRTOS (foco no ATmega2560 / Arduino Mega)
├── simulacao/                     # Simulação do hardware e circuito da bancada
│   └── Bancada/                   # Circuito SimulIDE 2.0 (Arduino Mega) e guia de portas virtuais
├── src/
│   ├── DevSandbox.Core/           # [Camada de Domínio / Regras de Negócio - net10.0 pura]
│   │   ├── Bancada/               # BancadaEngine, ConclusaoComando, EstadoCaboPP, EstadoVeiculoCP (11 estados), RelesCP, RespostaFirmware, StackTasks
│   │   ├── Converters/            # CalculadoraHex, BaseNumerica, OperacoesBits (operações puras de 64 bits)
│   │   ├── Pomodoro/              # PomodoroEngine (TimeProvider), EstadoPomodoro, EstatisticasSessao, TransicaoPomodoro
│   │   └── Serial/                # FimDeLinha, HistoricoComandos, ISerialEngine, ISerialPort, MontadorLinhasSerial, SistemaSerialPort, SupervisorioSerialEngine
│   │
│   └── DevSandbox.UI/             # [Camada de Apresentação / Interface Gráfica Nordic - net10.0-windows]
│       ├── app.ico                # Ícone oficial da aplicação (>_ branco em quadrado Dark Slate)
│       ├── Common/                # Design System Nordic (GDI+ puro): ThemeManager, BarraAbas, BotaoNordic, CardNordic, CaixaTextoNordic, SeletorNordic, SeletorSegmentado, Indicadores, TerminalView, BitMatrixControl, ProgressRingControl, Toast, Icones, PreferenciaTema
│       ├── Forms/                 # MainForm (janela única com alternância de tema e abas integradas)
│       ├── Telas/                 # Telas desacopladas (UserControls herdando de TelaBase): TelaInicio, TelaBancada, TelaSupervisorio, TelaCalculadora, TelaPomodoro
│       └── Program.cs             # Ponto de inicialização do executável (STAThread com inicialização DPI e tema)
│
└── tests/
    └── DevSandbox.Tests/          # [Camada de Testes Automatizados - NUnit 4 (607 testes)]
        ├── Fakes/                 # Dublês de teste: FakeSerialPort, FakeSerialEngine
        ├── BancadaCPTests.cs      # Testes da máquina de estados CP (11 estados) e tolerâncias
        ├── BancadaEngineTests.cs  # Testes de envio, reconexão e eventos da bancada
        ├── BancadaRevisaoTests.cs # Testes de sincronização, timeouts e concorrência
        ├── BancadaV2CoreTests.cs  # Testes dos novos comandos manuais e contratos da Bancada v2
        ├── CalculadoraHexTests.cs # Testes de conversão de 64 bits e operações
        ├── CalculadoraHexEntradaTests.cs # Validação de prefixos, hífens, sinal negativo e separadores
        ├── CorrenteMaximaCaboTests.cs # Validação das correntes máximas nominais de cabo PP
        ├── HistoricoComandosTests.cs # Testes de navegação e limites do histórico de comandos
        ├── MontadorLinhasSerialTests.cs # Testes do montador de frames e bufferização serial
        ├── NordicCoreTests.cs     # Testes de integração e comportamento dos componentes visuais Nordic
        ├── PomodoroEngineTests.cs # Testes determinísticos com FakeTimeProvider e transições
        ├── QaBancada3cTests.cs    # Validação do contrato textual e desfechos com o firmware
        ├── QaBordasTests.cs       # Casos de borda e robustez de API
        ├── QaCorrecoesTests.cs    # Testes de regressão de correções identificadas
        ├── QaNordicBordasTests.cs # 95 testes específicos de robustez e limites da suíte Nordic
        └── SupervisorioSerialEngineTests.cs # Testes do motor serial resiliente e watchdog
```

---

### 🧩 Papel de Cada Camada

#### 1. Camada de Domínio e Lógica (`DevSandbox.Core`)
* **O que é:** Biblioteca de Classes (*Class Library*) pura em .NET 10 (`net10.0`), sem dependências visuais (sem referências a `System.Windows.Forms` ou `System.Drawing`).
* **Princípios Implementados:**
  * **Inversão de Dependências e Abstrações:** Motores como `SupervisorioSerialEngine` e `BancadaEngine` implementam `ISerialEngine` e operam sobre abstrações `ISerialPort`, permitindo injeção de dublês em testes.
  * **Relógios Desacoplados:** O `PomodoroEngine` utiliza `TimeProvider` injetável (`System.TimeProvider`), permitindo simulação temporal precisa e determinística em testes unitários.
  * **Controle de Terminação Flexível (`FimDeLinha.cs`):** Enumeração com suporte a `Nenhum`, `CR`, `LF` e `CRLF`, acompanhada dos métodos de extensão `ObterTerminador()` e `ObterRotulo()`, suportada nativamente em `ISerialEngine.EnviarDados(string texto, FimDeLinha fimDeLinha)`.
  * **Histórico de Comandos Serial (`HistoricoComandos.cs`):** Motor de navegação para até 100 comandos via setas (↑/↓), com prevenção automática de repetições consecutivas e preservação do rascunho em edição.
  * **Montador de Linhas Seriais (`MontadorLinhasSerial.cs`):** Bufferização eficiente de recepção UART, montando linhas completas delimitadas por `\n` antes da entrega aos módulos consumidores.
  * **Operações de 64 bits (`OperacoesBits.cs`):** Operações bitwise puras: `InverterBit`, `LerBit`, `TrocarEndian` (*byte swap* de 8 bytes), `DeslocarEsquerda` (`<< 1`), `DeslocarDireita` (`>> 1`), `Not`, `Maximo`, `ParaAscii` (8 bytes em *big-endian*) e `ParaInt64`.
  * **Estatísticas e Transições Pomodoro (`EstatisticasSessao.cs`, `TransicaoPomodoro.cs`):** Rastreamento de ciclos (1 a 4, reportando 4/4 no descanso longo), tempo acumulado em foco e interrupções, além de cálculo instantâneo da fração de tempo decorrido via propriedade `Progresso` (0.0 a 1.0) e evento `OnTransicao`.
  * **Mapeamento de Corrente Elétrica (`EstadoCaboPPExtensions.ObterCorrenteMaxima`):** Determinação da capacidade nominal do cabo PP (13 A, 20 A, 32 A, 63 A; `null` para cabo desconectado, resistor de bancada 4700 Ω ou curto-circuito).
  * **Comandos Manuais na Bancada:** Suporte a `BancadaEngine.EnviarComandoManual(string comando, FimDeLinha fimDeLinha = FimDeLinha.LF)` com validação prévia de sintaxe via `ComandoManualValido`.
  * **Isolamento de Erros:** Disparos de eventos notificam os assinantes de forma isolada, impedindo que exceções em manipuladores externos interrompam fluxos internos.

#### 2. Camada de Apresentação (`DevSandbox.UI`)
* **O que é:** Aplicação executável gráfica para Windows Forms (`net10.0-windows`) com design system moderno **Nordic** (claro e escuro) integrado em uma janela única com abas.
* **Melhorias de Experiência e Arquitetura Visual:**
  * **Janela Única com Abas Integradas:** O antigo lançador de múltiplas janelas foi substituído pelo `MainForm.cs` unificado, que gerencia as telas como `UserControls` desacoplados herdando de `TelaBase` (`TelaInicio`, `TelaBancada`, `TelaSupervisorio`, `TelaCalculadora` e `TelaPomodoro`), navegáveis através da `BarraAbas.cs`.
  * **Gerenciamento Dinâmico de Temas (`ThemeManager.cs`):** Suporte completo aos modos Claro (*Nordic Snow*) e Escuro (*Nordic Polar Night*). Na primeira execução, o aplicativo adota a preferência do sistema operacional (`AppsUseLightTheme`), persistindo a escolha do usuário em `%AppData%\DevSandbox\tema.json` (`PreferenciaTema.cs`). A troca de temas propaga-se dinamicamente por toda a árvore de controles sem necessidade de reinicialização.
  * **Controles Próprios em GDI+ Puro (`Common/`):** Todos os componentes foram reescritos em GDI+ puro sem dependência do gerador `.Designer.cs` (`BotaoNordic`, `CardNordic`, `CaixaTextoNordic`, `SeletorNordic`, `SeletorSegmentado`, `Indicadores`, `TerminalView`, `BitMatrixControl`, `ProgressRingControl`, `Toast`, `BarraAbas`, `Icones`), eliminando componentes obsoletos da era Fluent/LedIndicator.
  * **Ícone Oficial da Aplicação:** Ícone moderno em alta resolução (`src/DevSandbox.UI/app.ico`, símbolo `>_` em vetor branco sobre quadrado Dark Slate), incorporado ao executável (`ApplicationIcon`) e atribuído à janela principal (`MainForm`).
  * **Scrollbars e Terminais Customizados:** O `TerminalView` possui barra de rolagem desenhada sob medida com estilo Nordic, suporte a *autoscroll* comutável (*Acompanhar o final*), histórico com setas e limite circular de buffer para evitar sobrecarga de memória.
  * **Notificações Visuais Não Intrusivas:** Sistema de `Toast` flutuante e alerta via barra de tarefas com `FlashWindowEx`, dispensando o uso de diálogos modais bloqueantes (`MessageBox`).

#### 3. Camada de Testes Automatizados (`DevSandbox.Tests`)
* **O que é:** Suíte abrangente de testes automatizados com **NUnit 4** e adaptador de teste para .NET 10.
* **Cobertura e Dublês:**
  * **607 testes automatizados aprovados**, cobrindo regras de negócio, limites de conversão, tratamento de exceções, máquinas de estado, bufferização serial e tolerâncias de temporização.
  * Inclusão de suítes aprofundadas como `NordicCoreTests.cs`, `QaNordicBordasTests.cs` (95 testes específicos de robustez e casos de borda), `BancadaV2CoreTests.cs`, `CorrenteMaximaCaboTests.cs`, `HistoricoComandosTests.cs`, `MontadorLinhasSerialTests.cs` e `QaCorrecoesTests.cs`.
  * Uso intensivo de dublês de teste (`FakeSerialPort`, `FakeSerialEngine`) e temporização simulada determinística com `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`).
  * Alta taxa de cobertura de código no `DevSandbox.Core`, garantindo estabilidade em refatorações.

---

## 🚀 Módulos Implementados Atualmente

* **⚡ Emulador de Bancada EVSE (Modo 3 - IEC 61851-1 / IEC 62196-2)**:
  * **11 Estados do Control Pilot (CP):** Suporte completo aos 11 estados de recarga normalizados: `AX` (12 V - Desconectado), `BX+`/`BX`/`BX-` (10/9/8 V - Conectado), `CX+`/`CX`/`CX-` (7/6/5 V - Carregando), `DX+`/`DX`/`DX-` (4/3/2 V - Carregando com Ventilação) e `E` (~1 V - Falha: 120 Ω do CP ao PE), conforme a Tabela II (limite superior/nominal/inferior). O Estado F (-12 V) não é emulado pelo veículo pois é de responsabilidade da estação.
  * **Telemetria de Valores Nominais:** Exibição em tempo real das tensões teóricas (superior, nominal e inferior), resistências elétricas nominais de teste e corrente máxima suportada pelo cabo de recarga PP via `ObterCorrenteMaxima` (13 A, 20 A, 32 A e 63 A).
  * **Indicador de Sincronismo Real:** Sinalização visual com badges coloridos (`CONFIRMADO` em verde quando o estado coincide com a resposta do firmware, ou `PENDENTE` em laranja durante o trânsito do comando).
  * **Matriz de Relés D22–D31 Somente Leitura:** Mapeamento visual estritamente alinhado com o estado físico dos relés reportado pelo microcontrolador, garantindo fidelidade de monitoramento sem permitir overrides manuais conflitantes.
  * **Botão "Reset CP (A)":** Atalho de segurança operacional que retorna a linha CP imediatamente ao estado inicial `AX` sem interferir na seleção configurada no cabo PP.
  * **Terminal Serial da Bancada em Largura Total:** Monitor de comandos e respostas com seletor de fim de linha (`Nenhum`, `CR`, `LF`, `CRLF`), alternador *Acompanhar o final*, histórico de comandos com setas (↑/↓) e scrollbar customizada Nordic.
  * **Nota Técnica do Resistor 4700 Ω (PP):** O canal 1 (4700 Ω) é mantido como valor de teste e calibração interna de bancada, fora da tabela de cabos comerciais da norma IEC 62196-2.

* **🔌 Supervisório Serial (UART / COM)**:
  * Motor no `Core` (`SupervisorioSerialEngine`) sob contrato `ISerialEngine`, com detecção dinâmica de portas, watchdog inteligente via `GetPortNames()`, retorno booleano de envio (`EnviarDados`) e reconexão automática resiliente.
  * Terminal serial desacoplado em altura fixa com recepção em lote a cada 50 ms e barra de rolagem customizada no estilo Nordic.
  * Seletor de finalizador de linha com quatro modos (`Nenhum`, `CR`, `LF`, `CRLF`, com padrão em `LF`).
  * Envio flexível: permite enviar linhas vazias (transmitindo somente os bytes do terminador selecionado) e preserva o foco e conteúdo da caixa de envio.
  * Histórico de comandos com navegação rápida via setas (↑/↓) e toggle de rolagem automática (*Acompanhar o final*).

* **⏱️ Pomodoro Timer**:
  * Motor no `Core` (`PomodoroEngine`) baseado em `TimeProvider`, com suporte seguro a `Pausar()`, `Retomar()`, cancelamento atômico, disparo garantido de `00:00` no fechamento de ciclo e proteção de manipuladores de eventos.
  * **Mostrador Circular Moderno (`ProgressRingControl`):** Anel de progresso que é consumido suavemente conforme a fração de tempo restante (`Progresso`), adaptando suas cores ao modo ativo (Foco, Pausa Curta ou Pausa Longa).
  * **Estatísticas de Sessão em Memória (`EstatisticasSessao.cs`):** Monitoramento do ciclo atual (1 a 4, exibindo 4/4 durante o descanso longo), tempo acumulado em foco e total de interrupções registradas.
  * **Alertas Não Bloqueantes:** Avisos sonoros integrados, destaque visual na barra de tarefas (`FlashWindowEx`) caso a janela esteja em segundo plano e notificações elegantes em `Toast`.

* **🔢 Conversor de Bases Numéricas e Manipulação de Bits (Calculadora 64 bits)**:
  * Motor no `Core` (`CalculadoraHex`, `OperacoesBits`): conversão bidirecional e manipulação matemática pura de 64 bits.
  * **Matriz Interativa de 64 Bits (`BitMatrixControl`):** Grade de 4×16 bits interativos com alternância individual de bits por clique e destaque de estado ativo/inativo.
  * **Operações de Bits Dedicadas:** Botões para inversão rápida de extremidade (*Swap Endian* de 8 bytes), deslocamento à esquerda (`<< 1`), deslocamento à direita (`>> 1`), negação lógica (`NOT`), zeramento (`Zerar`) e saturação máxima (`Máximo` - `0xFFFFFFFFFFFFFFFF`).
  * **4 Exibições Sincronizadas Simultaneamente:** Leitura instantânea e coerente nos formatos Hexadecimal (`0x...`), Decimal Unsigned (`UInt64`), Decimal Signed (`Int64` em complemento de dois) e representação em caracteres ASCII (*big-endian*).

---

## 🔬 Firmware e Simulação da Bancada

### 1. Firmware Embarcado (`firmware/Bancada/`)
* **Plataforma:** C++ com PlatformIO e FreeRTOS para microcontroladores AVR.
* **Hardware Alvo:**
  * **Arduino Mega 2560:** Suporte completo aos relés do CP (S7 a S12) e PP (S1 a S6).
  * **Arduino Nano:** Modo compacto com suporte exclusivo aos relés do PP (responde `ERRO` para comandos de CP).
* **Segurança e Chaveamento:**
  * **Chaveamento por Interseção (*Break-Before-Make*):** Desliga apenas os relés que saem do novo estado, aguarda uma pausa mínima de 20 ms (somente quando há relés saindo e novos relés entrando), e liga os relés necessários, prevenindo curtos e sobrecorrentes nos resistores de teste.
  * **Boot Seguro:** Na inicialização, todos os relés são desenergizados por segurança.
  * **Reaplicação de Estado:** Reenviar o mesmo estado é tratado como operação sem efeito (*no-op*); para forçar um ciclo elétrico de reabertura, seleciona-se `estado ax` antes do estado desejado.
* **Documentação Detalhada:** Consulte o manual completo do firmware e tabela de pinagem em [firmware/Bancada/README.md](firmware/Bancada/README.md).

### 2. Simulação em Circuito Virtual (`simulacao/Bancada/`)
* **Simulador:** **SimulIDE 2.0**.
* **Circuito:** Arquivo `simulacao/Bancada/circuito_bancada_mega.sim1`, contendo o Arduino Mega 2560, arranjo completo de relés do CP, transistores de acionamento do PP, instrumentos de medição e conexão UART virtual.
* **Portas Seriais Virtuais:** Integração via par virtual COM1 ↔ COM2 utilizando o **HHD Virtual Serial Port Tools**:
  * O SimulIDE conecta-se à porta **COM1** a 9600 bps.
  * O aplicativo `DevSandbox.UI` conecta-se à porta **COM2** a 9600 bps.
* **Guia Passo a Passo:** Instruções completas para carregar o binário `.hex`, configurar o par de portas virtuais e executar a bancada estão descritas em [simulacao/Bancada/GUIA_SIMULACAO_BANCADA.md](simulacao/Bancada/GUIA_SIMULACAO_BANCADA.md).

---

## 🛠️ Tecnologias Utilizadas

* **Linguagem**: C# 13+ / .NET 10.0 (`net10.0` / `net10.0-windows`)
* **Compilação e Qualidade**: `Directory.Build.props` com `TreatWarningsAsErrors=true` e analisadores .NET ativos
* **Interface Gráfica**: Windows Forms com Design System **Nordic** desenhado em GDI+ puro, Dark/Light Mode com detecção automática do Windows e persistência local
* **Framework de Testes**: `NUnit 4`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk`, `Microsoft.Extensions.TimeProvider.Testing`
* **Comunicação Serial**: `System.IO.Ports` com contratos desacoplados `ISerialEngine`, `ISerialPort` e montador de frames
* **Firmware e Simulação**: PlatformIO, FreeRTOS, SimulIDE 2.0, HHD Virtual Serial Port Tools

---

## 🧪 Como Executar os Testes

Execute toda a suíte de **607 testes automatizados** via CLI:

```powershell
dotnet test dev-sandbox.slnx
```

Para coletar o relatório de cobertura de código do Core:

```powershell
dotnet test tests/DevSandbox.Tests/DevSandbox.Tests.csproj --collect:"XPlat Code Coverage"
```

---

## ▶️ Como Executar a Aplicação

```powershell
dotnet run --project src/DevSandbox.UI/DevSandbox.UI.csproj
```

---

## 📦 Distribuição e Executável Standalone

### 1. Geração Local (Script de Publicação)
Para gerar uma versão autocontida (standalone) que roda em qualquer computador Windows x64 sem exigir instalação prévia do .NET Runtime:

```powershell
.\publish-standalone.ps1
```

O binário final é compilado e disponibilizado em:
```plaintext
dist/standalone/DevSandbox.UI.exe
```

*(Nota: Na compilação em modo Release, os binários normais também são copiados para `dist/` através da regra `CopyToDist`).*

### 2. Distribuição via GitHub Releases
* **Repositório Limpo:** O executável compilado `.exe` não é rastreado no histórico do Git (as pastas `dist/` e `app/` estão no `.gitignore`).
* **Lançamentos Oficiais:** As releases oficiais **`v1.2.0`** e **`v1.2.1`** estão disponíveis diretamente na aba de **Releases** do repositório no GitHub (a versão `v1.2.1` traz o executável standalone `DevSandbox.UI.exe` de ~49 MB com o ícone oficial incorporado). As tags anteriores (`v1.0.0` e `v1.1.0`) permanecem preservadas no histórico do Git.

---

## 📱 Compatibilidade Multiplataforma

* **Interface Gráfica (`DevSandbox.UI`)**: Restrita ao Windows Desktop (Windows 10/11 x64), pois baseia-se em APIs Win32, DWM e GDI+ via Windows Forms (`net10.0-windows`).
* **Lógica e Motores (`DevSandbox.Core`)**: 100% portável para qualquer plataforma suportada pelo .NET 10 (Linux, macOS, Android, iOS, WebAssembly), graças à ausência total de dependências visuais e ao desacoplamento completo de hardware via interfaces.
