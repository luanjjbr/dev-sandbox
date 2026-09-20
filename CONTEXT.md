# 🧠 CONTEXT.md — Contexto do Projeto dev-sandbox

Documento de contexto técnico e operacional do repositório **dev-sandbox**. Criado para fornecer a desenvolvedores e agentes de IA uma visão completa da arquitetura, módulos implementados, convenções de código, status atual e instruções de evolução.

---

## 📌 1. Visão Geral

* **Repositório:** `dev-sandbox`
* **Finalidade:** Laboratório pessoal de desenvolvimento em C# e .NET para prototipagem rápida, ferramentas de engenharia, utilitários práticos e testes de conceitos arquiteturais.
* **Stack Principal:**
  * **Linguagem / Runtime:** C# 13+ / .NET 10.0 (`net10.0` e `net10.0-windows`)
  * **Interface Gráfica:** Windows Forms com Dark Mode nativo via Windows Desktop Window Manager (`dwmapi.dll`)
  * **Testes Automatizados:** NUnit 4 (`NUnit`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk`)
  * **Solução:** Formato XML de solução do Visual Studio (`dev-sandbox.slnx`)
  * **Comunicação Serial:** `System.IO.Ports`

---

## 🏛️ 2. Arquitetura da Solução

O projeto segue estritamente o princípio da separação de responsabilidades em três camadas fundamentais:

```plaintext
dev-sandbox/
├── dev-sandbox.slnx                     # Solução unificada
├── README.md                            # Guia rápido e documentação externa
├── CONTEXT.md                           # Memória técnica e contexto do projeto
├── publish-standalone.ps1               # Script para gerar executável único standalone
├── app/                                 # Pasta rastreada no Git com o executável único pronto
│   └── DevSandbox.UI.exe                # Executável autocontido pronto para uso e upload no GitHub
├── dist/                                # Executáveis e binários locais (gerados a cada build)
├── src/
│   ├── DevSandbox.Core/                 # Biblioteca de Classes (net10.0 pura)
│   │   ├── Bancada/                     # Emulador de cabo PP e estados de recarga CP (Modo 3)
│   │   ├── Converters/                  # Algoritmos de conversão numérica
│   │   ├── Pomodoro/                    # Motor de tempo e estados do Pomodoro
│   │   └── Serial/                      # Motor de comunicação UART resiliente
│   │
│   └── DevSandbox.UI/                   # Executável WinForms (net10.0-windows)
│       ├── Common/                      # Gerenciador de temas e estilização Win32/DWM
│       ├── Forms/                       # Telas e janelas (MainHub, Pomodoro, Serial, Calc, Bancada)
│       └── Program.cs                   # Ponto de entrada (STAThread)
│
└── tests/
    └── DevSandbox.Tests/                # Suíte de Testes Automatizados (NUnit)
        ├── BancadaEngineTests.cs        # 16 testes de comandos seriais e estados PP/CP
        ├── CalculadoraHexTests.cs       # 13 testes de conversão e casos de borda
        ├── PomodoroEngineTests.cs       # 8 testes de ciclo assíncrono e eventos
        └── SupervisorioSerialEngineTests.cs # 7 testes de estabilidade e limites
```

### Regras de Ouro da Arquitetura:
1. **`DevSandbox.Core` NUNCA referencia `DevSandbox.UI` ou bibliotecas visuais:** Não pode haver acoplamento com WinForms, `System.Drawing` ou `System.Windows.Forms` dentro do Core.
2. **`DevSandbox.UI` é uma Casca Visual Fina (*Thin UI*):** Os formulários apenas tratam cliques, exibem valores e assinam eventos dos motores do Core.
3. **Toda lógica crítica deve ser testável no `DevSandbox.Tests`:** Cálculos, transições de estado assíncronas e tratamentos de erro devem ser validados via NUnit sem instanciar janelas.

---

## 🧩 3. Detalhamento dos Módulos Implementados

### 3.1. 🔌 Supervisório Serial (UART / COM)
* **Core (`src/DevSandbox.Core/Serial/SupervisorioSerialEngine.cs`):**
  * Detecção de portas ativas via `SerialPort.GetPortNames()`.
  * Suporte a taxas de 1.200 a 921.600 bps.
  * **Mecanismo de Resiliência (Watchdog):** Temporizador periódico que monitora a abertura da porta. Se o dispositivo físico for desconectado abruptamente (remoção de cabo USB), dispara `ConexaoPerdida` e tenta reconectar em segundo plano sem travar a aplicação, emitindo `Reconectado` assim que a porta reaparece.
  * Gerenciamento de ciclo de vida seguro com `IDisposable` e descarte de buffers/handles nativos.
* **UI (`src/DevSandbox.UI/Forms/FormSupervisorio.cs`):**
  * Terminal visual escuro com texto verde monocromático.
  * Mecanismo de paginação/corte de memória: mantém o histórico limitado a 50.000 caracteres para impedir vazamentos de RAM em transmissões contínuas.
  * Envio de comandos com tecla Enter e barra de status dinâmica.
* **Testes (`tests/DevSandbox.Tests/SupervisorioSerialEngineTests.cs`):**
  * Garante integridade de inicialização, validação de argumentos inválidos de conexão, e chamada segura/idempotente de `Dispose()`.

---

### 3.2. ⏱️ Pomodoro Timer
* **Core (`src/DevSandbox.Core/Pomodoro/PomodoroEngine.cs` e `EstadoPomodoro.cs`):**
  * Cronômetro de alta precisão baseado em `System.Diagnostics.Stopwatch`.
  * Laço temporal assíncrono não bloqueante via `Task.Delay(50, token)` e controle fino com `CancellationTokenSource`.
  * Suporte real a **Pausar (`Pausar()`)** e **Retomar (`Retomar()`)**, preservando os milissegundos restantes sem perder contagem.
  * Estados explícitos: `Parado`, `Foco`, `DescansoCurto`, `DescansoLongo`, `Pausado`.
  * Eventos: `OnTick(TimeSpan)` (com garantia de disparo no `00:00`) e `OnEstadoMudou(EstadoPomodoro)`.
* **UI (`src/DevSandbox.UI/Forms/FormPomodoro.cs`):**
  * Botões de presets rápidos (25m Foco, 15m Descanso Longo, 5m Descanso Curto) e seletor numérico manual.
  * Alternância dinâmica dos botões de controle (Iniciar/Parar e Pausar/Continuar) com paleta contextual de cores.
  * Alertas visuais e sonoros (`SystemSounds.Beep`).
* **Testes (`tests/DevSandbox.Tests/PomodoroEngineTests.cs`):**
  * Testes assíncronos que usam `TaskCompletionSource` para verificar conclusões de ciclos sem depender de esperas arbitrárias em tempo real.

---

### 3.3. 🔢 Conversor de Bases Numéricas (Calculadora Hex)
* **Core (`src/DevSandbox.Core/Converters/CalculadoraHex.cs`):**
  * Conversão bidirecional entre Hexadecimal, Decimal e Binário.
  * Suporte integral a inteiros de 64 bits (`ulong` e `long` com complemento de dois, suportando de `0x0` até `0xFFFFFFFFFFFFFFFF`).
  * Tolerância a prefixos (`0x`, `0X`, `0b`, `0B`) e separadores de legibilidade (espaços, sublinhados `_`, hifens `-`).
  * Agrupamento visual automático de bits em *nibbles* (blocos de 4 bits).
* **UI (`src/DevSandbox.UI/Forms/FormCalculadora.cs`):**
  * Detecção inteligente ao pressionar Enter: avalia se a entrada contém caracteres hexadecimais, binários ou numéricos decimais e executa a conversão correta.
  * Botões de cópia rápida para o Clipboard com animação visual transitória de sucesso ("✓ Copiado!").
* **Testes (`tests/DevSandbox.Tests/CalculadoraHexTests.cs`):**
  * 13 testes unitários cobrindo desde entradas válidas até casos de borda extremos (32 bits sem overflow, limites de 64 bits, caracteres ilegais e tratamento de exceções).

---

### 3.4. ⚡ Emulador de Bancada EVSE (Modo 3 - IEC 61851-1 e IEC 62196-2)
* **Core (`src/DevSandbox.Core/Bancada/`):**
  * `EstadoCaboPP.cs`: Mapeamento das resistências no Pino de Proximidade (PP) entre PP e PE conforme IEC 62196-2 (Cabo 0 a 6: Circuito Aberto, 4700 Ω, 1500 Ω [13A], 680 Ω [20A], 220 Ω [32A], 100 Ω [63A], 47 Ω [Falha]). Extensões para obtenção de comandos seriais (`cabo X`).
  * `EstadoVeiculoCP.cs`: Mapeamento dos estados do veículo no Pino Piloto de Controle (CP) conforme o nível de tensão da borda positiva do sinal PWM (Estado A: +12V [Desconectado], Estado B: +9V [Conectado], Estado C: +6V [Carregando], Estado D: +3V [Carregando c/ Ventilação], Estado E: 0V [Falha/Curto]). Extensões para comandos seriais (`estado X`).
  * `BancadaEngine.cs`: Coordena o emulador, gerenciando conexão serial com watchdog de reconexão automática, controle de estados atuais e despacho de comandos seriais.
* **UI (`src/DevSandbox.UI/Forms/FormBancada.cs`):**
  * Interface desktop dividida em três colunas operacionais: Emulador do Cabo (PP), Emulador de Estados do Veículo (CP) e Monitor Serial com terminal de log e entrada de comandos manuais.
* **Testes (`tests/DevSandbox.Tests/BancadaEngineTests.cs`):**
  * 16 testes unitários validando a formatação dos comandos CLI, atualização de propriedades, notificação de eventos e tratamento de comandos manuais.

---

### 3.5. 🎨 Sistema Visual Windows 11 Fluent Design (WinUI 3 Dark)
* **Tokens e Temas (`src/DevSandbox.UI/Common/ThemeManager.cs`):**
  * Integração com a API nativa `dwmapi.dll` (`DwmSetWindowAttribute`) ativando o modo escuro na barra de título do Windows 10/11.
  * Paleta oficial Windows 11 Fluent Dark: Fundo Mica `#202020`, Cards `#2B2B2B` com bordas `#383838`, e cor de acento Ciano/Azul Vibrante (`#4CC2FF`).
  * Utilitário `CriarRetanguloArredondado(Rectangle, int)` para renderização GDI+ antialiased com cantos curvos.
* **Componentes Fluent Reutilizáveis:**
  * **`FluentButton.cs`**: Botão com cantos arredondados, suporte ao modo *Accent* (`#4CC2FF` com texto preto) e modo secundário (`#383838` com texto branco), além de efeitos interativos de *hover* e *press*.
  * **`FluentCard.cs`**: Painel de superfície elevada com cantos arredondados (raio 8px), borda de 1px e suporte a destaque suave no *hover*.
  * **`FluentTextBox.cs`**: Caixa de texto com fundo profundo (`#1E1E1E`), cantos arredondados e a assinatura visual do Windows 11: **linha inferior de destaque ciano (`#4CC2FF`) de 2.5px quando focada**.
  * **`FluentToggle.cs`**: Chave de alternância (Toggle Switch) em formato de pílula arredondada fiel ao WinUI 3 (Off com contorno cinza / On com preenchimento em `#4CC2FF` e thumb escuro).
  * **Tratamento de Cantos e Duplo Buffer (`ObterCorFundoEfetiva`):** Todos os controles customizados realizam `g.Clear()` com a cor real do container pai antes da renderização antialiased, eliminando qualquer vestígio de cantos pretos em geometrias arredondadas no Windows Forms.

---

### 3.6. 🏠 Hub Central (`MainForm`)
* **Arquivo:** `src/DevSandbox.UI/Forms/MainForm.cs`
* **Recursos:**
  * Atua como painel de controle principal (Dashboard) para navegação entre as ferramentas.
  * Cards visuais interativos com efeito hover e clique expandido (Supervisório, Pomodoro, Calculadora e Bancada EVSE).
  * Gerencia a exibição e retorno gracioso das janelas filhas com `ShowDialog()`.

---

## 🔄 4. Fluxo para Implementar Novos Módulos

Ao criar uma nova ferramenta no repositório, siga sempre esta sequência:

1. **Crie a Lógica no Core:**
   * Adicione o novo namespace/pasta em `src/DevSandbox.Core/<NomeDoModulo>/`.
   * Mantenha as classes puras, assíncronas quando necessário, e expor eventos/métodos claros.
2. **Crie a Suíte de Testes:**
   * Adicione `<NomeDoModulo>Tests.cs` dentro de `tests/DevSandbox.Tests/`.
   * Cubra cenários de sucesso, entradas inválidas e limites numéricos/temporais.
3. **Crie a Interface Gráfica:**
   * Crie o formulário em `src/DevSandbox.UI/Forms/Form<NomeDoModulo>.cs`.
   * Invoque `ThemeManager.AplicarBarraDeTituloEscura(this)` e aplique o tema.
   * Conecte os componentes visuais ao motor do Core.
4. **Vincule ao Hub:**
   * Crie um novo Card na `MainForm` (`src/DevSandbox.UI/Forms/MainForm.cs`) e implemente a chamada do formulário.

---

## 🚀 5. Comandos Úteis

### Executar Testes Automatizados
```powershell
dotnet test dev-sandbox.slnx
```

### Compilar toda a solução
```powershell
dotnet build dev-sandbox.slnx
```

### Executar a Aplicação Visual (UI)
```powershell
dotnet run --project src/DevSandbox.UI/DevSandbox.UI.csproj
```

### Executar Diretamente o Executável (.exe)
Sempre que a solução é compilada, os executáveis são automaticamente copiados para a pasta `dist/` na raiz. Você pode simplesmente abrir a pasta e clicar duas vezes em:
```plaintext
dist/DevSandbox.UI.exe
```

### Gerar Executável Único para Enviar a Qualquer Pessoa (Single-File Standalone)
Gera um único `.exe` de ~49 MB com o .NET 10 e tudo embutido. Quem receber esse arquivo não precisa instalar nada para rodar:
```powershell
.\publish-standalone.ps1
```
Ou via CLI:
```powershell
dotnet publish src/DevSandbox.UI/DevSandbox.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist/standalone
```
O arquivo gerado fica pronto em `dist/standalone/DevSandbox.UI.exe` e `app/DevSandbox.UI.exe`.

---

## 📊 6. Status Atual de Qualidade
* **Testes Automatizados:** 44 testes passando (0 falhas).
* **Compilação:** 0 erros, 0 avisos em .NET 10.0.
* **Cobertura:** Todos os 4 motores do Core possuem testes unitários dedicados.
