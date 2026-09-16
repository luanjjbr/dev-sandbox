# 🧪 dev-sandbox

Repositório pessoal destinado a testes, protótipos e desenvolvimento de utilitários em C# e .NET 10. O projeto adota uma arquitetura em camadas desacoplada, permitindo que novas ferramentas, algoritmos e testes sejam adicionados de forma organizada e escalável.

---

## 🏛️ Organização e Arquitetura do Projeto

A solução segue a convenção padrão de mercado para ecossistemas .NET modernos, dividindo as responsabilidades em diretórios de alto nível: **`src/`** (código-fonte da aplicação) e **`tests/`** (suítes de testes automatizados).

```plaintext
dev-sandbox/
├── dev-sandbox.slnx               # Arquivo de solução (Visual Studio Solution XML)
├── src/
│   ├── DevSandbox.Core/           # [Camada de Domínio / Regras de Negócio]
│   │   └── <Modulos_Logicos>/     # Motores, algoritmos, comunicação e utilitários puros
│   │
│   └── DevSandbox.UI/             # [Camada de Apresentação / Interface Gráfica]
│       ├── Common/                # Gerenciamento de temas, estilos e recursos visuais
│       ├── Forms/                 # Telas, janelas e painéis de controle
│       └── Program.cs             # Ponto de inicialização do executável
│
└── tests/
    └── DevSandbox.Tests/          # [Camada de Testes Automatizados]
        └── <Testes_Unitarios>/    # Validação de regras de negócio, timers e cálculos
```

---

### 🧩 Papel de Cada Camada

#### 1. Camada de Domínio e Lógica (`DevSandbox.Core`)
* **O que é:** Uma Biblioteca de Classes (*Class Library*) pura em .NET, sem qualquer dependência de bibliotecas de interface visual (sem dependência de Windows Forms, WPF, etc.).
* **O que deve conter:**
  * Regras de negócio e modelos de domínio.
  * Motores de execução assíncrona, timers e rotinas em segundo plano.
  * Serviços de comunicação de baixo nível (ex.: portas seriais, sockets, leitura de arquivos).
  * Funções matemáticas, algoritmos e conversores de dados.
* **Objetivo:** Garantir que a lógica seja totalmente independente da interface gráfica, tornando o código altamente testável, portável e reutilizável em outros tipos de aplicação (como CLI, APIs ou apps mobile) no futuro.

#### 2. Camada de Apresentação (`DevSandbox.UI`)
* **O que é:** A aplicação executável gráfica voltada ao usuário final (Windows Forms).
* **O que deve conter:**
  * Formulários (`Forms`), janelas modais e painéis.
  * Componentes visuais e eventos de interação (cliques, atalhos de teclado).
  * Gerenciamento de estilo e temas (suporte a Dark Mode nativo).
  * Painel principal (*Dashboard/Hub*) para navegação entre os utilitários da sandbox.
* **Objetivo:** Manter a camada visual "fina" (*thin UI*), servindo apenas como uma casca de exibição e interação que consome as regras e eventos disponibilizados pelo `DevSandbox.Core`.

#### 3. Camada de Testes Automatizados (`DevSandbox.Tests`)
* **O que é:** Projeto dedicado de testes unitários e de integração utilizando **NUnit**.
* **O que deve conter:**
  * Testes que exercitam as classes, métodos e eventos do `DevSandbox.Core`.
  * Validação de fluxos assíncronos, limites numéricos e tratamento de exceções.
* **Objetivo:** Assegurar a estabilidade do código, garantindo que novas funcionalidades ou correções não introduzam regressões, sem depender de testes manuais na tela.

---

### 🔄 Como Adicionar um Novo Protótipo ou Utilitário

Para manter a consistência da arquitetura, o fluxo recomendado para criar uma nova ferramenta no repositório é:

1. **Implementar a Lógica no `Core`**: Crie a classe de serviço, cálculo ou motor dentro de `src/DevSandbox.Core/<NovaFerramenta>/`.
2. **Criar os Testes**: Adicione testes unitários em `tests/DevSandbox.Tests/` cobrindo cenários válidos e casos de borda.
3. **Construir a Tela no `UI`**: Crie o formulário correspondente em `src/DevSandbox.UI/Forms/` conectando os botões aos métodos do `Core`.
4. **Registrar no Hub Central**: Adicione um card de acesso na `MainForm` para permitir que o usuário abra a nova ferramenta com um clique.

---

## 🚀 Módulos Implementados Atualmente

A título de exemplo prático desta arquitetura, os seguintes utilitários já estão disponíveis:

* **🔌 Supervisório Serial (UART / COM)**:
  * Motor no `Core` (`SupervisorioSerialEngine`) com detecção de portas, leitura contínua, *watchdog*, proteção contra vazamento de handles e auto-reconexão resiliente.
  * Interface em `UI` (`FormSupervisorio`) com visual de terminal escuro, buffer controlado, seleção ampla de Baud Rates e envio rápido.
* **⏱️ Pomodoro Timer**:
  * Motor no `Core` (`PomodoroEngine`) com cronômetro de precisão, suporte a pausa/retomada (`Pausar()` / `Retomar()`), cancelamento gracioso e estados dinâmicos.
  * Interface em `UI` (`FormPomodoro`) com controles de iniciar/pausar/continuar/parar e alertas visuais/sonoros de finalização.
* **🔢 Conversor de Bases Numéricas (Calculadora Hex)**:
  * Motor no `Core` (`CalculadoraHex`) com conversão bidirecional de 64 bits entre bases Hexadecimal, Decimal e Binário, suporte a separadores (`_`, `-`) e agrupamento de bits.
  * Interface em `UI` (`FormCalculadora`) com conversão rápida (Hex/Dec/Bin), detecção inteligente e botões de cópia rápida para a área de transferência.

---

## 🛠️ Tecnologias Utilizadas

* **Linguagem**: C# 13+ / .NET 10.0 (`net10.0` / `net10.0-windows`)
* **Interface Gráfica**: Windows Forms com Dark Mode nativo via DWM (`dwmapi.dll`)
* **Framework de Testes**: `NUnit 4`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk`
* **Comunicação Serial**: `System.IO.Ports`

---

## 🧪 Como Executar os Testes

```powershell
dotnet test dev-sandbox.slnx
```

## ▶️ Como Executar a Aplicação

```powershell
dotnet run --project src/DevSandbox.UI/DevSandbox.UI.csproj
```