using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using DevSandbox.Core.Serial;
using DevSandbox.UI.Common;

namespace DevSandbox.UI.Telas
{
    /// <summary>
    /// Supervisório Serial: cartão de conexão (porta, baud, conectar, watchdog) e terminal com envio e fim de linha.
    /// Recepção em lotes: a thread da serial só acumula linhas; um timer da UI as despeja a cada 50 ms.
    /// </summary>
    public sealed class TelaSupervisorio : TelaBase
    {
        private readonly SupervisorioSerialEngine _engine = new();

        private const int IntervaloLoteMs = 50;
        private readonly object _travaRecepcao = new();
        private readonly List<LinhaTerminal> _bufferRecepcao = new();
        private int _caracteresNoBuffer;
        private readonly System.Windows.Forms.Timer _timerRecepcao = new() { Interval = IntervaloLoteMs };

        // Junta os pedaços do ReadExisting em linhas (corte em \n; texto sem \n sai após 100 ms ocioso)
        private readonly MontadorLinhasSerial _montador = new();

        // Histórico de envios (↑/↓ na caixa de mensagem); _navegando evita que a troca de texto pelo histórico
        // seja tratada como edição do usuário
        private readonly HistoricoComandos _historico = new();
        private bool _navegando;

        private readonly CardNordic _cardConexao;
        private readonly SeletorNordic _portas;
        private readonly BotaoNordic _atualizarPortas;
        private readonly SeletorNordic _baud;
        private readonly BotaoNordic _conectar;
        private readonly BotaoNordic _limpar;
        private readonly SeloNordic _seloWatchdog;
        private readonly TerminalView _terminal;
        private readonly CaixaTextoNordic _mensagem;
        private readonly SeletorNordic _fimDeLinha;
        private readonly BotaoNordic _transmitir;
        private readonly ToolTip _dicas = new();

        public TelaSupervisorio()
        {
            // ---- Cartão de conexão
            _cardConexao = new CardNordic { Titulo = "Conexão UART / COM" };

            _portas = new SeletorNordic { AccessibleName = "Porta serial" };
            _atualizarPortas = Botao("", EstiloBotao.Secundario, Icone.Atualizar, (_, _) => CarregarPortas());
            _atualizarPortas.AccessibleName = "Atualizar portas";
            _dicas.SetToolTip(_atualizarPortas, "Atualizar a lista de portas COM");

            _baud = new SeletorNordic { AccessibleName = "Baud rate", Formatar = o => $"{o} bps" };
            _conectar = Botao("Conectar porta", EstiloBotao.Primario, Icone.Plugue, (_, _) => AlternarConexao());
            _limpar = Botao("Limpar histórico", EstiloBotao.Secundario, Icone.Lixeira, (_, _) => Limpar());

            _seloWatchdog = new SeloNordic();
            _dicas.SetToolTip(_seloWatchdog, "O watchdog verifica a porta a cada 1 s e reconecta sozinho se ela cair.");

            var gradeConexao = Grade(2, 10);
            gradeConexao.Dock = DockStyle.Fill;
            gradeConexao.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            gradeConexao.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
            foreach (int h in new[] { 22, 38, 22, 38, 50, 36, 28, 24, 30 })
            {
                gradeConexao.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
            }
            gradeConexao.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // sobra fica embaixo

            gradeConexao.Controls.Add(RotuloCampo("Porta serial"), 0, 0);
            gradeConexao.SetColumnSpan(gradeConexao.GetControlFromPosition(0, 0)!, 2);
            _portas.Dock = DockStyle.Fill;
            _portas.Margin = new Padding(0, 2, 6, 4);
            gradeConexao.Controls.Add(_portas, 0, 1);
            _atualizarPortas.Dock = DockStyle.Fill;
            _atualizarPortas.Margin = new Padding(0, 2, 0, 4);
            gradeConexao.Controls.Add(_atualizarPortas, 1, 1);

            var rotuloBaud = RotuloCampo("Baud rate");
            gradeConexao.Controls.Add(rotuloBaud, 0, 2);
            gradeConexao.SetColumnSpan(rotuloBaud, 2);
            _baud.Dock = DockStyle.Fill;
            _baud.Margin = new Padding(0, 2, 0, 4);
            gradeConexao.Controls.Add(_baud, 0, 3);
            gradeConexao.SetColumnSpan(_baud, 2);

            _conectar.Dock = DockStyle.Fill;
            _conectar.Margin = new Padding(0, 12, 0, 2);
            gradeConexao.Controls.Add(_conectar, 0, 4);
            gradeConexao.SetColumnSpan(_conectar, 2);
            _limpar.Dock = DockStyle.Fill;
            _limpar.Margin = new Padding(0, 4, 0, 2);
            gradeConexao.Controls.Add(_limpar, 0, 5);
            gradeConexao.SetColumnSpan(_limpar, 2);

            var divisoria = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 13, 0, 14), Height = 1 };
            divisoria.Paint += (_, e) => e.Graphics.Clear(ThemeManager.BordaSutil);
            gradeConexao.Controls.Add(divisoria, 0, 6);
            gradeConexao.SetColumnSpan(divisoria, 2);

            var rotuloWatchdog = RotuloCampo("Watchdog de conexão");
            gradeConexao.Controls.Add(rotuloWatchdog, 0, 7);
            gradeConexao.SetColumnSpan(rotuloWatchdog, 2);

            var linhaWatchdog = Grade(2, 1);
            linhaWatchdog.Dock = DockStyle.Fill;
            linhaWatchdog.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            linhaWatchdog.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            var monitor = Rotulo("Monitor:", PapelTexto.Secundario, ThemeManager.FontePequena, autoSize: false);
            monitor.Dock = DockStyle.Fill;
            monitor.TextAlign = ContentAlignment.MiddleLeft;
            linhaWatchdog.Controls.Add(monitor, 0, 0);
            _seloWatchdog.Dock = DockStyle.Fill;
            linhaWatchdog.Controls.Add(_seloWatchdog, 1, 0);
            gradeConexao.Controls.Add(linhaWatchdog, 0, 8);
            gradeConexao.SetColumnSpan(linhaWatchdog, 2);

            _cardConexao.Controls.Add(gradeConexao);

            // ---- Terminal e barra de envio
            _terminal = new TerminalView
            {
                Titulo = "TERMINAL LOG (DESACOPLADO 50 MS)",
                NomeAcessivelLog = "Histórico da serial"
            };

            _mensagem = new CaixaTextoNordic
            {
                PlaceholderText = "Digite o comando serial e pressione Enter...",
                InnerFont = ThemeManager.FonteMono,
                NomeAcessivel = "Mensagem a transmitir"
            };
            _mensagem.InnerKeyDown += Mensagem_KeyDown;
            _mensagem.InnerTextChanged += (_, _) =>
            {
                _mensagem.Erro = false;
                if (!_navegando) _historico.ResetarNavegacao();
            };

            _fimDeLinha = CriarSeletorFimDeLinha();
            _transmitir = Botao("Transmitir", EstiloBotao.Primario, Icone.Enviar, (_, _) => Transmitir());

            var barraEnvio = Grade(4, 1);
            barraEnvio.Dock = DockStyle.Bottom;
            barraEnvio.Height = 36;
            barraEnvio.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            barraEnvio.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            barraEnvio.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            barraEnvio.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            _mensagem.Dock = DockStyle.Fill;
            _mensagem.Margin = new Padding(0, 2, 8, 2);
            var rotuloFim = Rotulo("Fim de linha:", PapelTexto.Secundario, ThemeManager.FonteRotulo, autoSize: false);
            rotuloFim.Dock = DockStyle.Fill;
            rotuloFim.TextAlign = ContentAlignment.MiddleRight;
            rotuloFim.Margin = new Padding(0, 0, 6, 0);
            _fimDeLinha.Dock = DockStyle.Fill;
            _fimDeLinha.Margin = new Padding(0, 2, 8, 2);
            _transmitir.Dock = DockStyle.Fill;
            _transmitir.Margin = new Padding(0, 2, 0, 2);
            barraEnvio.Controls.Add(_mensagem, 0, 0);
            barraEnvio.Controls.Add(rotuloFim, 1, 0);
            barraEnvio.Controls.Add(_fimDeLinha, 2, 0);
            barraEnvio.Controls.Add(_transmitir, 3, 0);

            var direita = PainelTransparente();
            _terminal.Dock = DockStyle.Fill;
            var espaco = new Panel { Dock = DockStyle.Bottom, Height = 12, BackColor = Color.Transparent };
            direita.Controls.Add(_terminal);
            direita.Controls.Add(espaco);
            direita.Controls.Add(barraEnvio);

            var principal = Grade(2, 1);
            principal.Dock = DockStyle.Fill;
            principal.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 338));
            principal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            principal.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _cardConexao.Dock = DockStyle.Top;
            _cardConexao.Height = 372;
            var esquerda = PainelTransparente();
            esquerda.Padding = new Padding(0, 0, 18, 0);
            esquerda.Controls.Add(_cardConexao);
            esquerda.Dock = DockStyle.Fill;
            direita.Dock = DockStyle.Fill;
            principal.Controls.Add(esquerda, 0, 0);
            principal.Controls.Add(direita, 1, 0);
            AutoScrollMinSize = new Size(0, 520);
            Controls.Add(principal);

            CarregarPortas();
            _baud.DefinirItens(new object[] { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600 });
            _baud.SelectedItem = 9600;

            _engine.DadosRecebidos += Engine_DadosRecebidos;
            _engine.ConexaoPerdida += Engine_ConexaoPerdida;
            _engine.Reconectado += Engine_Reconectado;

            _timerRecepcao.Tick += TimerRecepcao_Tick;
            _timerRecepcao.Start();

            AtualizarEstadoUI(EstadoConexao.Desconectado);
            Registrar(TipoLinha.Sistema, "Motor de comunicação serial inicializado. Selecione a porta e conecte.");
        }

        private enum EstadoConexao { Desconectado, Conectado, Reconectando }

        /// <summary>Lista suspensa Nenhum / CR / LF (padrão) / CR+LF, compartilhada com a Bancada.</summary>
        internal static SeletorNordic CriarSeletorFimDeLinha()
        {
            var s = new SeletorNordic
            {
                Mono = true,
                AccessibleName = "Fim de linha",
                Formatar = o => o is FimDeLinha f ? f switch
                {
                    FimDeLinha.Nenhum => "Nenhum",
                    FimDeLinha.CR => @"Só \r (CR)",
                    FimDeLinha.LF => @"Só \n (LF)",
                    _ => @"\r\n (CR+LF)"
                } : ""
            };
            s.DefinirItens(new object[] { FimDeLinha.Nenhum, FimDeLinha.CR, FimDeLinha.LF, FimDeLinha.CRLF });
            s.SelectedItem = FimDeLinha.LF;
            return s;
        }

        public override void AoAtivar() => _mensagem.Focus();

        private void CarregarPortas()
        {
            string anterior = _portas.TextoSelecionado;
            _portas.DefinirItens(_engine.ObterPortasDisponiveis());
            if (_portas.Itens.Count == 0) return;
            _portas.SelectedItem = _portas.Itens.FirstOrDefault(p => string.Equals(p as string, anterior, StringComparison.OrdinalIgnoreCase));
            if (_portas.SelectedIndex < 0) _portas.SelectedIndex = 0;
        }

        private void AlternarConexao()
        {
            if (_engine.EstaConectado || _engine.TentandoReconectar)
            {
                _engine.Desconectar();
                RegistrarPendenteDescarregado();
                AtualizarEstadoUI(EstadoConexao.Desconectado);
                Registrar(TipoLinha.Aviso, "Porta desconectada.");
                return;
            }

            string porta = _portas.TextoSelecionado;
            if (string.IsNullOrWhiteSpace(porta))
            {
                Toast.Mostrar(this, "Nenhuma porta COM selecionada. Conecte o dispositivo e clique em atualizar.", TipoToast.Aviso);
                return;
            }

            try
            {
                int baud = Convert.ToInt32(_baud.SelectedItem, CultureInfo.InvariantCulture);
                // Nova conexão não herda texto pendente nem a marca do '\r' da conexão anterior
                _montador.Limpar();
                _engine.Conectar(porta, baud);
                AtualizarEstadoUI(EstadoConexao.Conectado);
                Registrar(TipoLinha.Sistema, $"Conectado à porta {porta} @ {baud} bps.");
            }
            catch (Exception ex)
            {
                AtualizarEstadoUI(EstadoConexao.Desconectado);
                Registrar(TipoLinha.Erro, $"Falha ao abrir {porta}: {ex.Message}");
                Toast.Mostrar(this, $"Não foi possível abrir {porta}: {ex.Message}", TipoToast.Erro);
            }
        }

        private void Transmitir()
        {
            string mensagem = _mensagem.Text;
            FimDeLinha fim = _fimDeLinha.SelectedItem is FimDeLinha f ? f : FimDeLinha.LF;

            // Campo vazio envia só o terminador; vazio sem terminador não tem o que transmitir
            if (mensagem.Length == 0 && fim == FimDeLinha.Nenhum)
            {
                _mensagem.Erro = true;
                Toast.Mostrar(this, "Nada a transmitir: digite um texto ou escolha um fim de linha.", TipoToast.Aviso);
                return;
            }

            if (!_engine.EstaConectado)
            {
                Toast.Mostrar(this, "Sem conexão: conecte uma porta serial antes de transmitir.", TipoToast.Aviso);
                return;
            }

            // O texto fica na caixa depois do envio (pedido do usuário); o histórico guarda o que foi enviado
            _historico.Adicionar(mensagem);
            if (_engine.EnviarDados(mensagem, fim))
            {
                Registrar(TipoLinha.Tx, mensagem, fim.ObterRotulo());
            }
            else
            {
                // Em falha de E/S o Core já disparou ConexaoPerdida (alerta de reconexão no log);
                // aqui só registra que esta mensagem não saiu
                Registrar(TipoLinha.Erro, $"Não enviado: {mensagem}");
            }
        }

        private void Mensagem_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Transmitir();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode is Keys.Up or Keys.Down)
            {
                NavegarHistorico(_historico, _mensagem, e.KeyCode == Keys.Up, ref _navegando);
                e.Handled = e.SuppressKeyPress = true;
            }
        }

        /// <summary>↑ = envio anterior; ↓ = seguinte (ao passar do último volta o texto que estava sendo digitado).</summary>
        internal static void NavegarHistorico(HistoricoComandos historico, CaixaTextoNordic caixa, bool anterior, ref bool navegando)
        {
            string? texto = anterior ? historico.Anterior(caixa.Text) : historico.Proximo();
            if (texto is null) return;
            navegando = true;
            try
            {
                caixa.DefinirTextoComCursorNoFim(texto);
            }
            finally
            {
                navegando = false;
            }
        }

        private void Limpar()
        {
            _montador.Limpar();
            lock (_travaRecepcao)
            {
                _bufferRecepcao.Clear();
                _caracteresNoBuffer = 0;
            }
            _terminal.Limpar();
        }

        // ------------------------------------------------------------------ Recepção (thread da serial)

        private void Engine_DadosRecebidos(object? sender, string dados)
        {
            // Executa na thread da serial: apenas acumula (sem BeginInvoke por pedaço)
            if (!_engine.EstaConectado) return;

            foreach (string linha in _montador.Adicionar(dados))
            {
                RegistrarRecebido(linha);
            }
        }

        /// <summary>Acumula uma linha recebida (thread da serial ou UI). O horário é o de quando a linha fechou.</summary>
        private void RegistrarRecebido(string linha)
        {
            lock (_travaRecepcao)
            {
                _bufferRecepcao.Add(new LinhaTerminal(DateTime.Now, TipoLinha.Rx, linha));
                _caracteresNoBuffer += linha.Length + 20;

                // Proteção de memória se a UI ficar parada: mantém só a cauda do buffer
                while (_caracteresNoBuffer > TerminalView.LimiteCaracteres && _bufferRecepcao.Count > 1)
                {
                    _caracteresNoBuffer -= _bufferRecepcao[0].Texto.Length + 20;
                    _bufferRecepcao.RemoveAt(0);
                }
            }
        }

        /// <summary>Entrega o texto sem \n que ainda está no montador (desconexão/perda de conexão).</summary>
        private void RegistrarPendenteDescarregado()
        {
            string? pendente = _montador.Descarregar();
            if (pendente is not null) RegistrarRecebido(pendente);
        }

        private void TimerRecepcao_Tick(object? sender, EventArgs e)
        {
            string? ocioso = _montador.ColetarPendenteSeOcioso();
            if (ocioso is not null) RegistrarRecebido(ocioso);

            LinhaTerminal[] lote;
            lock (_travaRecepcao)
            {
                if (_bufferRecepcao.Count == 0) return;
                lote = _bufferRecepcao.ToArray();
                _bufferRecepcao.Clear();
                _caracteresNoBuffer = 0;
            }
            _terminal.AdicionarLinhas(lote);
        }

        /// <summary>Linha gerada pela UI: antes, despeja o que já chegou (ordem cronológica).</summary>
        private void Registrar(TipoLinha tipo, string texto, string? fimDeLinha = null)
        {
            TimerRecepcao_Tick(this, EventArgs.Empty);
            _terminal.AdicionarLinha(tipo, texto, fimDeLinha);
        }

        private void Engine_ConexaoPerdida(object? sender, EventArgs e)
        {
            if (!PodeAtualizarUI) return;
            if (InvokeRequired)
            {
                InvocarNaUI(() => Engine_ConexaoPerdida(sender, e));
                return;
            }

            AtualizarEstadoUI(EstadoConexao.Reconectando);
            RegistrarPendenteDescarregado();
            Registrar(TipoLinha.Aviso, "Conexão interrompida. Tentando reconectar...");
        }

        private void Engine_Reconectado(object? sender, EventArgs e)
        {
            if (!PodeAtualizarUI) return;
            if (InvokeRequired)
            {
                InvocarNaUI(() => Engine_Reconectado(sender, e));
                return;
            }

            AtualizarEstadoUI(EstadoConexao.Conectado);
            Registrar(TipoLinha.Sistema, "Comunicação restabelecida com a porta COM.");
        }

        private void AtualizarEstadoUI(EstadoConexao estado)
        {
            bool livre = estado == EstadoConexao.Desconectado;

            _conectar.Text = livre ? "Conectar porta" : "Desconectar";
            _conectar.Estilo = livre ? EstiloBotao.Primario : EstiloBotao.Perigo;
            _portas.Enabled = livre;
            _baud.Enabled = livre;
            _atualizarPortas.Enabled = livre;

            bool podeEnviar = estado == EstadoConexao.Conectado;
            _mensagem.Enabled = podeEnviar;
            _transmitir.Enabled = podeEnviar;

            (_cardConexao.Selo, _cardConexao.TipoSelo, _terminal.EstadoLed) = estado switch
            {
                EstadoConexao.Conectado => ("CONECTADO", TipoSelo.Sucesso, EstadoLed.Verde),
                EstadoConexao.Reconectando => ("RECONECTANDO", TipoSelo.Aviso, EstadoLed.Ambar),
                _ => ("DESCONECTADO", TipoSelo.Neutro, EstadoLed.Apagado)
            };
            (_seloWatchdog.Text, _seloWatchdog.Tipo) = estado switch
            {
                EstadoConexao.Conectado => ("VIGILANTE (1000 ms)", TipoSelo.Sucesso),
                EstadoConexao.Reconectando => ("RECONECTANDO", TipoSelo.Aviso),
                _ => ("INATIVO", TipoSelo.Neutro)
            };
        }

        // Guarda para callbacks vindos da thread da serial: a tela pode estar sendo descartada
        private bool PodeAtualizarUI => !IsDisposed && !Disposing && IsHandleCreated;

        private void InvocarNaUI(Action acao)
        {
            if (!PodeAtualizarUI) return;
            try
            {
                BeginInvoke(acao);
            }
            catch (InvalidOperationException)
            {
                // Handle destruído entre a checagem e o BeginInvoke (fechamento concorrente)
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timerRecepcao.Stop();
                _timerRecepcao.Tick -= TimerRecepcao_Tick;
                _timerRecepcao.Dispose();

                // Desassina antes do Dispose: nenhum callback da serial chega com a tela fechando
                _engine.DadosRecebidos -= Engine_DadosRecebidos;
                _engine.ConexaoPerdida -= Engine_ConexaoPerdida;
                _engine.Reconectado -= Engine_Reconectado;
                _engine.Dispose();
                _dicas.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
