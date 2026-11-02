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
│   └── standalone/                # Destino dos binários gerados pelo script de publicação
├── firmware/                      # Firmware embarcado da bancada de testes EVSE
│   └── Bancada/                   # Projeto PlatformIO + FreeRTOS (foco no ATmega2560 / Arduino Mega)
├── simulacao/                     # Simulação do hardware e circuito da bancada
│   └── Bancada/                   # Circuito SimulIDE 2.0 (Arduino Mega) e guia de portas virtuais
├── src/
│   ├── DevSandbox.Core/           # [Camada de Domínio / Regras de Negócio - net10.0 pura]
│   │   ├── Bancada/               # BancadaEngine, EstadoCaboPP, EstadoVeiculoCP (11 estados), RespostaFirmware
│   │   ├── Converters/            # CalculadoraHex, BaseNumerica
│   │   ├── Pomodoro/              # PomodoroEngine (TimeProvider), EstadoPomodoro
│   │   └── Serial/                # ISerialEngine, ISerialPort, SistemaSerialPort, SupervisorioSerialEngine
│   │
│   └── DevSandbox.UI/             # [Camada de Apresentação / Interface Gráfica WinUI 3 Dark - net10.0-windows]
│       ├── Common/                # ThemeManager (cache GDI, desenho de foco), botões, cards, caixas de texto Fluent
│       ├── Forms/                 # MainForm, FormBancada, FormCalculadora, FormPomodoro, FormSupervisorio
│       └── Program.cs             # Ponto de inicialização do executável (STAThread)
│
└── tests/
    └── DevSandbox.Tests/          # [Camada de Testes Automatizados - NUnit 4]
        ├── Fakes/                 # Dublês de teste: FakeSerialPort, FakeSerialEngine
        ├── BancadaCPTests.cs      # Testes da máquina de estados CP (11 estados) e tolerâncias
        ├── BancadaEngineTests.cs  # Testes de envio, reconexão e eventos da bancada
        ├── BancadaRevisaoTests.cs # Testes de sincronização, timeouts e concorrência
        ├── CalculadoraHexTests.cs # Testes de conversão de 64 bits e bases numéricas
        ├── CalculadoraHexEntradaTests.cs # Validação de prefixos, hífens, sinal negativo e separadores
        ├── PomodoroEngineTests.cs # Testes determinísticos com FakeTimeProvider
        ├── SupervisorioSerialEngineTests.cs # Testes do motor serial resiliente e watchdog
        ├── QaBancada3cTests.cs    # Validação do contrato textual e desfechos com o firmware
        └── QaBordasTests.cs       # Casos de borda e robustez de API
```

---

### 🧩 Papel de Cada Camada

#### 1. Camada de Domínio e Lógica (`DevSandbox.Core`)
* **O que é:** Biblioteca de Classes (*Class Library*) pura em .NET 10 (`net10.0`), sem dependências visuais (sem referências a WinForms, WPF ou `System.Drawing`).
* **Princípios Implementados:**
  * **Inversão de Dependências e Abstrações:** Motores como `SupervisorioSerialEngine` implementam `ISerialEngine` e operam sobre portas `ISerialPort`, permitindo injeção de dublês em testes.
  * **Relógios Desacoplados:** O `PomodoroEngine` utiliza `TimeProvider` injetável (`System.TimeProvider`), permitindo simulação temporal precisa e determinística em testes unitários.
  * **Confiabilidade de Transmissão:** Métodos de envio como `EnviarDados` e comandos da bancada retornam `bool` indicando se a transmissão ocorreu com sucesso, tratando exceções de E/S sem derrubar a aplicação.
  * **Contrato Textual com Firmware:** Parser e gerador de comandos alinhados estritamente com o firmware (`estado <código>`, `cabo <0..6>`, `debug stack`, respostas `OK` / `ERRO: ...`).
  * **Isolamento de Erros:** Disparos de eventos notificam os assinantes de forma isolada, impedindo que exceções em manipuladores externos interrompam laços internos.

#### 2. Camada de Apresentação (`DevSandbox.UI`)
* **O que é:** Aplicação executável gráfica para Windows Forms (`net10.0-windows`) com estética moderna fiel ao Windows 11 (WinUI 3 Fluent Dark).
* **Melhorias de Experiência e Desempenho:**
  * **Controle da Bancada em Dois Níveis:** Seleção intuitiva de família de estado do CP (`AX`, `BX`, `CX`, `DX`, `E`) e seleção de subnível de tolerância (`Sup (+)`, `Nom`, `Inf (-)`), com confirmação em tempo real recebida do firmware (`Confirmado`, `Não enviado`, `Sem confirmação`, `ERRO`).
  * **Acessibilidade e Foco por Teclado:** Suporte completo a navegação via teclado com renderização de anel de foco de alto contraste (`DesenharFoco`), facilitando operação sem mouse.
  * **Recepção Serial em Lotes:** A recepção no terminal do Supervisório acumula dados e realiza despejos em lotes periódicos de ~50 ms, eliminando sobrecarga e travamentos por chamadas excessivas de `Invoke`.
  * **Avisos Não Bloqueantes:** O término do ciclo Pomodoro emite alertas sonoros e pisca a barra de tarefas (`FlashWindowEx`) caso a janela esteja em segundo plano, sem travar a interface com diálogos modais (`MessageBox`).
  * **Otimização GDI+:** O `ThemeManager` mantém um cache estático de fontes compartilhadas (`Segoe UI`, `Consolas`), prevenindo esgotamento de *handles* GDI do sistema operacional.

#### 3. Camada de Testes Automatizados (`DevSandbox.Tests`)
* **O que é:** Suíte abrangente de testes automatizados com **NUnit 4** e adaptador de teste para .NET 10.
* **Cobertura e Dublês:**
  * **345 testes automatizados** cobrindo regras de negócio, limites de conversão, tratamento de exceções, máquinas de estado e matriz de desfechos de comunicação.
  * Uso de dublês de hardware (`FakeSerialPort`, `FakeSerialEngine`) que simulam perdas abruptas de cabo, erros de leitura/escrita e respostas UART sem necessidade de portas físicas.
  * Utilização do `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`) para testes instantâneos de timers assíncronos.
  * **Cobertura de cerca de 85% de linhas** no `DevSandbox.Core`.

---

## 🚀 Módulos Implementados Atualmente

* **⚡ Emulador de Bancada EVSE (Modo 3 - IEC 61851-1 / IEC 62196-2)**:
  * **11 Estados do Control Pilot (CP):** Suporte completo aos 11 estados de recarga normalizados: `AX` (12 V - Desconectado), `BX+`/`BX`/`BX-` (10/9/8 V - Conectado), `CX+`/`CX`/`CX-` (7/6/5 V - Carregando), `DX+`/`DX`/`DX-` (4/3/2 V - Carregando com Ventilação) e `E` (~1 V - Falha: 120 Ω do CP ao PE), conforme a Tabela II (limite superior/nominal/inferior). O Estado F (-12V) não é emulado pelo veículo pois é de responsabilidade da estação.
  * **Protocolo Serial:** Comandos de estado `estado <ax|bx+|bx|bx-|cx+|cx|cx-|dx+|dx|dx-|e|status>` (com atalhos `a`, `b`, `c`, `d`), resistores de cabo `cabo <0..6>`, diagnóstico de memória `debug stack` e respostas padronizadas `OK` ou `ERRO: <mensagem>`.
  * **Nota Técnica do Resistor 4700 Ω (PP):** O canal 1 (4700 Ω) é mantido como valor de teste e calibração de bancada, fora da tabela de cabos comerciais da norma IEC 62196-2.
  * **Interface Visual Avançada (`FormBancada`):** Seleção de CP em dois níveis, painel de status com confirmação do firmware (`Confirmado`, `Não enviado`, `Sem confirmação`, `ERRO`), botão `Status` para sincronização manual, consulta automática ao conectar (com retry após reset por DTR) e monitor de terminal com teto de 50.000 caracteres.
* **🔌 Supervisório Serial (UART / COM)**:
  * Motor no `Core` (`SupervisorioSerialEngine`) sob contrato `ISerialEngine`, com detecção dinâmica de portas, watchdog inteligente via `GetPortNames()`, retorno booleano de envio (`EnviarDados`) e reconexão automática resiliente.
  * Interface em `UI` (`FormSupervisorio`) com recepção em lote a cada 50 ms, histórico com teto de 50.000 caracteres e registro de mensagens não enviadas.
* **⏱️ Pomodoro Timer**:
  * Motor no `Core` (`PomodoroEngine`) baseado em `TimeProvider`, com suporte seguro a `Pausar()`, `Retomar()`, cancelamento atômico, disparo garantido de `00:00` no fechamento de ciclo e proteção de manipuladores de eventos.
  * Interface em `UI` (`FormPomodoro`) com presets rápidos (25m/15m/5m), mostrador dinâmico e alerta sonoro/visual não invasivo (`FlashWindowEx`).
* **🔢 Conversor de Bases Numéricas (Calculadora Hex)**:
  * Motor no `Core` (`CalculadoraHex`, enum `BaseNumerica`) com conversão bidirecional de 64 bits (Hexadecimal, Decimal e Binário), suporte a separadores (`_`, ` `) e validação estrita: hífens `-` só são permitidos como separadores entre dígitos (ex.: `FF-FF`, `1100-0011`) e números negativos são rejeitados com mensagem amigável.
  * Interface em `UI` (`FormCalculadora`) com detecção inteligente de base, quebra visual em blocos de 32 bits para números binários de 64 bits e botões de cópia rápida com debounce.

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
* **Interface Gráfica**: Windows Forms com Dark Mode nativo via DWM (`dwmapi.dll`) e componentes Fluent
* **Framework de Testes**: `NUnit 4`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk`, `Microsoft.Extensions.TimeProvider.Testing`
* **Comunicação Serial**: `System.IO.Ports` com contratos desacoplados `ISerialEngine` e `ISerialPort`
* **Firmware e Simulação**: PlatformIO, FreeRTOS, SimulIDE 2.0, HHD Virtual Serial Port Tools

---

## 🧪 Como Executar os Testes

Execute toda a suíte de 345 testes automatizados via CLI:

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
* **Repositório Limpo:** O executável compilado `.exe` não é rastreado no histórico do Git (a pasta `app/` foi removida do versionamento e adicionada ao `.gitignore`).
* **Lançamentos Oficiais:** A partir da versão **`v1.0.0`**, os executáveis standalone são disponibilizados diretamente na aba de **Releases** do repositório no GitHub, anexados a cada tag de versão publicada.

---

## 📱 Compatibilidade Multiplataforma

* **Interface Gráfica (`DevSandbox.UI`)**: Restrita ao Windows Desktop (Windows 10/11), pois baseia-se em APIs Win32, DWM e GDI+ via Windows Forms (`net10.0-windows`).
* **Lógica e Motores (`DevSandbox.Core`)**: 100% portável para qualquer plataforma suportada pelo .NET 10 (Linux, macOS, Android, iOS, WebAssembly), graças à ausência total de dependências visuais e ao desacoplamento completo de hardware via interfaces.
