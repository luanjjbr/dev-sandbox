using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DevSandbox.Core.Bancada;
using DevSandbox.UI.Common;

namespace DevSandbox.UI.Forms
{
    public partial class FormBancada : Form
    {
        private readonly BancadaEngine _engine = new();
        private bool _carregandoUI = true;
        private const int LimiteTerminal = 50000;

        // Seleção local da coluna CP em dois níveis. A verdade do hardware é _engine.EstadoConfirmado.
        private GrupoEstadoCP _grupoCP = GrupoEstadoCP.A;
        private VariacaoEstadoCP _variacaoCP = VariacaoEstadoCP.Nominal;

        // Id do último pedido feito pela UI (0 = nenhum): só a conclusão com esse Id altera o indicador.
        // "_enviando*" cobre a conclusão síncrona (NaoEnviado) que chega DENTRO de DefinirEstado/DefinirCabo,
        // antes de o Id poder ser lido em UltimoIdComando.
        private long _idPedidoCP;
        private long _idPedidoPP;
        private bool _enviandoCP;
        private bool _enviandoPP;

        // Sincronização ao conectar: a placa reinicia com o DTR, então o primeiro "estado status" pode se perder.
        // Uma única nova tentativa após 2,5 s se ele voltar SemResposta.
        private const int AtrasoRetentativaStatusMs = 2500;
        private readonly System.Windows.Forms.Timer _timerRetentativaStatus;
        private long _idStatusInicial;
        private bool _retentativaStatusFeita;

        private readonly Dictionary<GrupoEstadoCP, RadioButton> _radiosCP;
        private readonly Dictionary<RadioButton, EstadoCaboPP> _radiosPP;
        private readonly Dictionary<FluentButton, VariacaoEstadoCP> _botoesVariacao;
        private readonly Dictionary<int, LedIndicator> _ledsPorPino;
        private readonly ToolTip _dicas;

        // Reenvio no clique repetido (seção 7 do design v2). O RadioButton dispara CheckedChanged e DEPOIS Click
        // no mesmo clique: o radio marcado pelo CheckedChanged fica aqui para o Click seguinte não reenviar.
        private RadioButton? _radioRecemMarcado;

        // Pulso "⟳ Reenviado" (400 ms): _reenvioXX = o pedido em curso é um reenvio ainda sem conclusão;
        // _restaurarXX = conclusão confirmada a mostrar no formato padrão quando o pulso acabar.
        private const int DuracaoPulsoReenvioMs = 400;
        private readonly System.Windows.Forms.Timer _timerPulsoCP;
        private readonly System.Windows.Forms.Timer _timerPulsoPP;
        private bool _reenvioCP;
        private bool _reenvioPP;
        private (EstadoVeiculoCP Estado, string Detalhe)? _restaurarCP;
        private (EstadoCaboPP Cabo, string Detalhe)? _restaurarPP;

        /// <summary>Estados visuais do indicador (seção 8 do design v2), mais "local" (nada enviado ainda).</summary>
        private enum Situacao { Local, Aguardando, Reenviado, Confirmado, ReenviadoConfirmado, NaoEnviado, ErroFirmware, SemResposta }

        public FormBancada()
        {
            InitializeComponent();

            components ??= new Container();
            _dicas = new ToolTip(components);
            _timerRetentativaStatus = new System.Windows.Forms.Timer(components) { Interval = AtrasoRetentativaStatusMs };
            _timerRetentativaStatus.Tick += TimerRetentativaStatus_Tick;
            _timerPulsoCP = new System.Windows.Forms.Timer(components) { Interval = DuracaoPulsoReenvioMs };
            _timerPulsoCP.Tick += TimerPulsoCP_Tick;
            _timerPulsoPP = new System.Windows.Forms.Timer(components) { Interval = DuracaoPulsoReenvioMs };
            _timerPulsoPP.Tick += TimerPulsoPP_Tick;

            _radiosCP = new()
            {
                [GrupoEstadoCP.A] = rbCPA,
                [GrupoEstadoCP.B] = rbCPB,
                [GrupoEstadoCP.C] = rbCPC,
                [GrupoEstadoCP.D] = rbCPD,
                [GrupoEstadoCP.E] = rbCPE
            };
            _radiosPP = new()
            {
                [rbPP0] = EstadoCaboPP.CircuitoAberto,
                [rbPP1] = EstadoCaboPP.Desconectado,
                [rbPP2] = EstadoCaboPP.Cabo13A,
                [rbPP3] = EstadoCaboPP.Cabo20A,
                [rbPP4] = EstadoCaboPP.Cabo32A,
                [rbPP5] = EstadoCaboPP.Cabo63A,
                [rbPP6] = EstadoCaboPP.FalhaCurto
            };
            _botoesVariacao = new()
            {
                [btnVarSup] = VariacaoEstadoCP.Superior,
                [btnVarNom] = VariacaoEstadoCP.Nominal,
                [btnVarInf] = VariacaoEstadoCP.Inferior
            };
            _ledsPorPino = new()
            {
                [30] = ledD30_PE,
                [31] = ledD31_Falha,
                [29] = ledD29_BHab,
                [28] = ledD28_BSel,
                [27] = ledD27_CDHab,
                [26] = ledD26_CDSel,
                [25] = ledD25_CHab,
                [24] = ledD24_CSel,
                [23] = ledD23_DHab,
                [22] = ledD22_DSel
            };

            // Seleção (CheckedChanged) e reenvio (Click no já marcado) dos radios de PP e CP
            foreach (RadioButton rb in _radiosPP.Keys.Concat(_radiosCP.Values))
            {
                rb.CheckedChanged += rbOpcao_CheckedChanged;
                rb.Click += rbOpcao_Click;
            }

            ThemeManager.AplicarBarraDeTituloEscura(this);
            AplicarTema();
            ConfigurarRotulos();
            ConfigurarReles();

            CarregarPortasDisponiveis();
            CarregarBaudRates();

            _engine.ComandoEnviado += Engine_ComandoEnviado;
            _engine.RespostaRecebida += Engine_RespostaRecebida;
            _engine.ComandoConcluido += Engine_ComandoConcluido;
            _engine.StatusCPRecebido += Engine_StatusCPRecebido;
            _engine.RelesCPAtualizados += Engine_RelesCPAtualizados;
            _engine.RespostaMultilinhaRecebida += Engine_RespostaMultilinhaRecebida;
            _engine.ConexaoPerdida += Engine_ConexaoPerdida;
            _engine.Reconectado += Engine_Reconectado;

            _carregandoUI = false;
            AtualizarEstadoUI(conectado: false);
            AtualizarVariacaoUI();
            MostrarSelecaoCP(EstadoSelecionadoCP, Situacao.Local);
            MostrarSelecaoCabo(_engine.CaboAtual, Situacao.Local);
            AtualizarFirmware();
            AppendTerminal("Bancada Engine inicializada. Selecione a porta e conecte.");
        }

        private EstadoVeiculoCP EstadoSelecionadoCP => EstadoVeiculoCPExtensions.ObterEstado(_grupoCP, _variacaoCP);

        private void AplicarTema()
        {
            this.BackColor = ThemeManager.FundoJanela;
            this.ForeColor = ThemeManager.TextoPrincipal;

            cardConexao.CardColor = ThemeManager.FundoCard;
            cardConexao.BorderColor = ThemeManager.BordaCard;

            cardCaboPP.CardColor = ThemeManager.FundoCard;
            cardCaboPP.BorderColor = ThemeManager.BordaCard;

            cardVeiculoCP.CardColor = ThemeManager.FundoCard;
            cardVeiculoCP.BorderColor = ThemeManager.BordaCard;

            cardTerminal.CardColor = ThemeManager.FundoCard;
            cardTerminal.BorderColor = ThemeManager.BordaCard;

            ThemeManager.EstilarComboBox(cboPortas);
            ThemeManager.EstilarComboBox(cboBaudRate);

            btnAtualizarPortas.IsAccent = false;
            btnConectar.IsAccent = true;
            btnEnviar.IsAccent = true;
            btnLimpar.IsAccent = false;
            btnStatusCP.IsAccent = false;
            btnStatusPP.IsAccent = false;
            btnDebugStack.IsAccent = false;
            btnHelp.IsAccent = false;

            // Painel de relés: fundo sutil #242424 com borda #333333 (raio 6)
            pnlRelesCP.CardColor = Color.FromArgb(36, 36, 36);
            pnlRelesCP.BorderColor = Color.FromArgb(51, 51, 51);
            pnlRelesCP.CornerRadius = 6;
            lblTituloReles.ForeColor = ThemeManager.TextoSecundario;

            txtTerminal.BackColor = Color.FromArgb(16, 16, 16);
            txtTerminal.ForeColor = ThemeManager.VerdeTerminal;

            chkAutoScroll.ForeColor = ThemeManager.TextoSecundario;

            txtComando.BackColor = ThemeManager.FundoInput;
            txtComando.ForeColor = ThemeManager.TextoPrincipal;

            lblNotaPP.ForeColor = ThemeManager.TextoSecundario;
            lblTensaoEsperada.ForeColor = ThemeManager.TextoSecundario;

            panelFooter.BackColor = ThemeManager.FundoJanela;
            lblFooter.ForeColor = ThemeManager.TextoSecundario;
        }

        /// <summary>
        /// Rótulos montados a partir do Core (tensão nominal de cada grupo e nota do resistor fora da norma),
        /// para a tela não duplicar dados normativos.
        /// </summary>
        private void ConfigurarRotulos()
        {
            foreach (var (grupo, rb) in _radiosCP)
            {
                double tensao = EstadoVeiculoCPExtensions.ObterEstado(grupo, VariacaoEstadoCP.Nominal).ObterTensaoNominal();
                rb.Text = $"{grupo}: {RotuloGrupo(grupo)} ({FormatarTensao(tensao)})";
            }

            string nota = EstadoCaboPPExtensions.NotaValorDaBancada;
            string notaFrase = char.ToUpper(nota[0], CultureInfo.CurrentCulture) + nota.Substring(1);
            lblNotaPP.Text = $"* {notaFrase}";

            foreach (var (rb, cabo) in _radiosPP)
            {
                if (!cabo.ForaDaNorma()) continue;
                if (!rb.Text.EndsWith('*')) rb.Text += " *";
                _dicas.SetToolTip(rb, $"Resistor de {cabo.ObterResistenciaNominal()} Ω: {nota}.");
            }

            _dicas.SetToolTip(lblNotaPP, $"Resistor da bancada física: {nota}.");
            _dicas.SetToolTip(btnStatusCP, "Pede ao firmware o estado CP atual e os relés ativos (sem alterar a seleção).");
            _dicas.SetToolTip(btnStatusPP, "Consulta o canal do cabo PP ativo no firmware.");
            _dicas.SetToolTip(btnDebugStack, "Inspeciona a margem de segurança da pilha de memória das tarefas FreeRTOS.");
            _dicas.SetToolTip(btnHelp, "Solicita ao firmware a lista de comandos seriais disponíveis.");
            _dicas.SetToolTip(btnLimpar, "Limpa o histórico de mensagens do monitor serial.");
            _dicas.SetToolTip(chkAutoScroll, "Mantém o monitor rolando até a última linha recebida.");
            _dicas.SetToolTip(pnlVariacaoCP, "Limites de tolerância: só nos estados B, C e D. Clicar na variação ativa reenvia o comando.");
        }

        /// <summary>
        /// Função de cada relé no circuito CP (tabela 5.1 do design v2): componente da PCB, símbolo e função,
        /// só para a dica de ferramenta. O nome lógico vem do Core (<see cref="RelesCP.ObterNome"/>).
        /// </summary>
        private static readonly Dictionary<int, string> FuncaoRele = new()
        {
            [30] = "RLY6 / S7: habilitação geral de PE. Conecta o diodo do veículo ao condutor de aterramento.",
            [31] = "RLY1 / S8: curto-circuito CP-PE antes do diodo (exclusivo do estado E).",
            [29] = "RLY7 / S9.1: habilitação do ramo resistivo do estado B (+9 V).",
            [28] = "RLY2 / S9.2: seleção de tolerância do estado B.",
            [27] = "RLY5 / S10_C: habilitação do ramo paralelo dos estados C e D.",
            [26] = "RLY8 / S10_D: comutação entre o ramo C e o ramo D.",
            [25] = "RLY3 / S11.1: habilitação do resistor paralelo do estado C (+6 V).",
            [24] = "RLY4 / S11.2: seleção de tolerância do estado C.",
            [23] = "RLY9 / S12.1: habilitação do resistor paralelo do estado D (+3 V).",
            [22] = "RLY10 / S12.2: seleção de tolerância do estado D."
        };

        private void ConfigurarReles()
        {
            foreach (var (pino, led) in _ledsPorPino)
            {
                led.Text = $"D{pino}: {RelesCP.ObterNome(pino)}";
                led.Perigo = pino == 31; // D31 (falha/curto) acende em vermelho
                _dicas.SetToolTip(led, FuncaoRele.TryGetValue(pino, out string? funcao) ? $"Relé D{pino} ({funcao})" : $"Relé D{pino}");
            }
            _dicas.SetToolTip(pnlRelesCP, "Relés do CP ativos segundo o último [STATUS CP] do firmware.");
            ApagarReles();
        }

        /// <summary>Acende os LEDs dos pinos informados pelo firmware e apaga os demais.</summary>
        private void MostrarReles(IReadOnlyList<int> pinosAtivos)
        {
            foreach (var (pino, led) in _ledsPorPino)
            {
                led.Ligado = pinosAtivos.Contains(pino);
            }
        }

        /// <summary>Sem conexão ou sem confirmação do firmware: todos os LEDs apagados.</summary>
        private void ApagarReles() => MostrarReles(Array.Empty<int>());

        private static string RotuloGrupo(GrupoEstadoCP grupo) => grupo switch
        {
            GrupoEstadoCP.A => "Desconectado",
            GrupoEstadoCP.B => "Conectado / Standby",
            GrupoEstadoCP.C => "Carga Normal",
            GrupoEstadoCP.D => "Carga c/ Ventilação",
            GrupoEstadoCP.E => "Falha / Curto",
            _ => grupo.ToString()
        };

        private static string FormatarTensao(double volts) =>
            $"{volts.ToString("+0.##;-0.##;0", CultureInfo.CurrentCulture)} V";

        private void CarregarPortasDisponiveis()
        {
            cboPortas.Items.Clear();
            string[] portas = _engine.ObterPortasDisponiveis();
            cboPortas.Items.AddRange(portas);

            if (cboPortas.Items.Count > 0)
            {
                cboPortas.SelectedIndex = 0;
            }
            else
            {
                cboPortas.Text = "";
            }
        }

        private void CarregarBaudRates()
        {
            cboBaudRate.Items.Clear();
            cboBaudRate.Items.AddRange(new object[] { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 });
            cboBaudRate.SelectedItem = 9600;
        }

        private void btnAtualizarPortas_Click(object sender, EventArgs e)
        {
            CarregarPortasDisponiveis();
        }

        private void btnConectar_Click(object sender, EventArgs e)
        {
            if (_engine.EstaConectado || _engine.TentandoReconectar)
            {
                CancelarRetentativaStatus();
                _engine.Desconectar();
                AtualizarEstadoUI(conectado: false);
                AppendTerminal("Porta serial desconectada.");
                return;
            }

            string porta = cboPortas.Text;
            if (string.IsNullOrWhiteSpace(porta))
            {
                MessageBox.Show("Selecione uma porta COM válida.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int baudRate = Convert.ToInt32(cboBaudRate.SelectedItem, CultureInfo.InvariantCulture);
                _engine.Conectar(porta, baudRate);
                AtualizarEstadoUI(conectado: true);
                AppendTerminal($"Conectado com sucesso à porta {porta} @ {baudRate} bps.");
                SincronizarComFirmware();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir a porta: {ex.Message}", "Falha de Conexão", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AtualizarEstadoUI(conectado: false);
            }
        }

        /// <summary>
        /// Ao conectar/reconectar, só PEDE o status ao firmware: a seleção local não é enviada
        /// e o estado do hardware não é alterado sozinho.
        /// </summary>
        private void SincronizarComFirmware()
        {
            CancelarRetentativaStatus();
            AppendTerminal("Sincronizando: pedindo o status do CP ao firmware (a seleção local não é enviada).");
            _engine.SolicitarStatusCP();
            _idStatusInicial = _engine.UltimoIdComando;
            _retentativaStatusFeita = false;
        }

        private void TimerRetentativaStatus_Tick(object? sender, EventArgs e)
        {
            _timerRetentativaStatus.Stop();
            if (!PodeAtualizarUI || !_engine.EstaConectado) return;

            AppendTerminal("Nova tentativa: pedindo o status do CP ao firmware.");
            _engine.SolicitarStatusCP();
            _idStatusInicial = 0; // só uma nova tentativa
        }

        private void CancelarRetentativaStatus()
        {
            _timerRetentativaStatus.Stop();
            _idStatusInicial = 0;
        }

        private void AtualizarEstadoUI(bool conectado)
        {
            if (conectado)
            {
                lblStatus.Text = $"Conectado ({cboPortas.Text} @ {cboBaudRate.Text} bps)";
                lblStatus.ForeColor = ThemeManager.VerdeTerminal;
                pnlStatusLed.BackColor = ThemeManager.VerdeTerminal;

                btnConectar.Text = "Desconectar";
                btnConectar.CustomAccentColor = ThemeManager.VermelhoPerigo;
                btnConectar.CustomTextColor = Color.White;

                cboPortas.Enabled = false;
                cboBaudRate.Enabled = false;
                btnAtualizarPortas.Enabled = false;
            }
            else
            {
                ApagarReles();
                lblStatus.Text = "Desconectado";
                lblStatus.ForeColor = ThemeManager.TextoSecundario;
                pnlStatusLed.BackColor = ThemeManager.VermelhoPerigo;

                btnConectar.Text = "Conectar";
                btnConectar.CustomAccentColor = null;
                btnConectar.CustomTextColor = null;
                btnConectar.IsAccent = true;

                cboPortas.Enabled = true;
                cboBaudRate.Enabled = true;
                btnAtualizarPortas.Enabled = true;
            }
        }

        // ------------------------------------------------------------------ Seleção e reenvio (PP e CP)

        /// <summary>Mudança de seleção (mouse, setas ou Espaço num radio novo): envia o cabo/estado escolhido.</summary>
        private void rbOpcao_CheckedChanged(object? sender, EventArgs e)
        {
            if (_carregandoUI) return;
            if (sender is not RadioButton { Checked: true } rb) return;

            // O Click deste mesmo clique (vem logo depois) não deve reenviar. Se não vier Click (mudança sem
            // clique), a marca é limpa ao fim da mensagem atual para não engolir um reenvio futuro.
            _radioRecemMarcado = rb;
            InvocarNaUI(() => _radioRecemMarcado = null);

            if (_radiosPP.TryGetValue(rb, out EstadoCaboPP cabo))
            {
                EnviarCabo(cabo, reenvio: false);
                return;
            }

            GrupoEstadoCP grupo = _radiosCP.First(kv => kv.Value == rb).Key;
            if (grupo != _grupoCP)
            {
                _variacaoCP = VariacaoEstadoCP.Nominal; // troca de grupo volta à variação nominal
            }
            _grupoCP = grupo;

            AtualizarVariacaoUI();
            EnviarEstadoSelecionado(reenvio: false);
        }

        /// <summary>
        /// Click (mouse ou Espaço) no radio JÁ marcado: reenvia o mesmo comando. No clique que muda a seleção
        /// o envio já foi feito pelo CheckedChanged, então aqui não há envio duplo.
        /// </summary>
        private void rbOpcao_Click(object? sender, EventArgs e)
        {
            if (_carregandoUI) return;
            if (sender is not RadioButton { Checked: true } rb) return;

            if (ReferenceEquals(_radioRecemMarcado, rb))
            {
                _radioRecemMarcado = null;
                return;
            }

            if (_radiosPP.TryGetValue(rb, out EstadoCaboPP cabo))
            {
                EnviarCabo(cabo, reenvio: true);
            }
            else if (_radiosCP.ContainsValue(rb))
            {
                EnviarEstadoSelecionado(reenvio: true);
            }
        }

        // ------------------------------------------------------------------ PP

        private void EnviarCabo(EstadoCaboPP cabo, bool reenvio)
        {
            _reenvioPP = reenvio;
            _restaurarPP = null;
            _timerPulsoPP.Stop();

            if (reenvio)
            {
                AppendTerminal($"⟳ Reenviando ao firmware: {cabo.ObterComandoSerial()} (reenvio manual)");
                _timerPulsoPP.Start();
            }

            // Indicador antes do Core: a conclusão (inclusive o NaoEnviado síncrono) nunca é sobrescrita
            MostrarSelecaoCabo(cabo, reenvio ? Situacao.Reenviado : Situacao.Aguardando);
            _enviandoPP = true;
            try
            {
                _engine.DefinirCabo(cabo); // NaoEnviado chega síncrono via ComandoConcluido
            }
            finally
            {
                _enviandoPP = false;
            }
            _idPedidoPP = _engine.UltimoIdComando;
            AtualizarFirmware();
        }

        private void MostrarSelecaoCabo(EstadoCaboPP cabo, Situacao situacao, string? detalhe = null)
        {
            RadioButton? rb = _radiosPP.FirstOrDefault(kv => kv.Value == cabo).Key;
            string nome = rb?.Text ?? cabo.ObterDescricao();
            (string sufixo, Color cor) = AparenciaDe(situacao);

            lblStatusCabo.Text = $"Ativo: {nome}{Environment.NewLine}{sufixo}";
            lblStatusCabo.ForeColor = cor;
            _dicas.SetToolTip(lblStatusCabo, detalhe ?? cabo.ObterDescricao());
        }

        private void btnStatusPP_Click(object? sender, EventArgs e)
        {
            _engine.SolicitarStatusCabo(); // o canal chega em CaboConfirmado; NaoEnviado/ERRO via ComandoConcluido
        }

        // ------------------------------------------------------------------ CP (dois níveis)

        private void btnVariacao_Click(object? sender, EventArgs e)
        {
            if (!_grupoCP.TemVariacao()) return;
            if (sender is not FluentButton btn || !_botoesVariacao.TryGetValue(btn, out VariacaoEstadoCP variacao)) return;

            bool reenvio = variacao == _variacaoCP; // clicar na variação já ativa reenvia o comando
            _variacaoCP = variacao;
            AtualizarVariacaoUI();
            EnviarEstadoSelecionado(reenvio);
        }

        private void EnviarEstadoSelecionado(bool reenvio)
        {
            EstadoVeiculoCP estado = EstadoSelecionadoCP;
            _reenvioCP = reenvio;
            _restaurarCP = null;
            _timerPulsoCP.Stop();

            if (reenvio)
            {
                AppendTerminal($"⟳ Reenviando ao firmware: {estado.ObterComandoSerial()} (reenvio manual)");
                _timerPulsoCP.Start();
            }

            // Indicador antes do Core: a conclusão (inclusive o NaoEnviado síncrono) nunca é sobrescrita
            MostrarSelecaoCP(estado, reenvio ? Situacao.Reenviado : Situacao.Aguardando);
            _enviandoCP = true;
            try
            {
                _engine.DefinirEstado(estado); // NaoEnviado chega síncrono via ComandoConcluido
            }
            finally
            {
                _enviandoCP = false;
            }
            _idPedidoCP = _engine.UltimoIdComando;
            AtualizarFirmware();
        }

        private void AtualizarVariacaoUI()
        {
            bool temVariacao = _grupoCP.TemVariacao();
            pnlVariacaoCP.Enabled = temVariacao;
            lblTituloVariacao.ForeColor = temVariacao ? ThemeManager.TextoSecundario : ThemeManager.TextoDesabilitado;

            foreach (var (btn, variacao) in _botoesVariacao)
            {
                btn.IsAccent = temVariacao && variacao == _variacaoCP;
                btn.Text = temVariacao
                    ? $"{AbreviacaoVariacao(variacao)} {FormatarTensao(EstadoVeiculoCPExtensions.ObterEstado(_grupoCP, variacao).ObterTensaoNominal())}"
                    : RotuloVariacaoInativa(variacao);
            }
        }

        private static string AbreviacaoVariacao(VariacaoEstadoCP v) => v switch
        {
            VariacaoEstadoCP.Superior => "Sup",
            VariacaoEstadoCP.Inferior => "Inf",
            _ => "Nom"
        };

        private static string RotuloVariacaoInativa(VariacaoEstadoCP v) => v switch
        {
            VariacaoEstadoCP.Superior => "+ Sup",
            VariacaoEstadoCP.Inferior => "− Inf",
            _ => "• Nom •"
        };

        private void MostrarSelecaoCP(EstadoVeiculoCP estado, Situacao situacao, string? detalhe = null)
        {
            (string sufixo, Color cor) = AparenciaDe(situacao);

            lblStatusVeiculo.Text = $"Ativo: {estado.ObterDescricao()}{Environment.NewLine}{sufixo}";
            lblStatusVeiculo.ForeColor = cor;
            lblTensaoEsperada.Text = $"Tensão nominal: {FormatarTensao(estado.ObterTensaoNominal())} • {estado.ObterComandoSerial()}";
            _dicas.SetToolTip(lblStatusVeiculo, detalhe ?? "");
        }

        private static (string Sufixo, Color Cor) AparenciaDe(Situacao situacao) => situacao switch
        {
            Situacao.Local => ("(seleção local, não enviada)", ThemeManager.TextoSecundario),
            Situacao.Aguardando => ("⋯ aguardando confirmação...", ThemeManager.TextoSecundario),
            Situacao.Reenviado => ("⟳ Reenviado ao firmware...", ThemeManager.CorAcentoBrilho),
            Situacao.Confirmado => ("✓ confirmado pelo firmware", ThemeManager.CorAcento),
            Situacao.ReenviadoConfirmado => ($"✓ Reenviado e confirmado [{DateTime.Now:HH:mm:ss}]", ThemeManager.CorAcento),
            Situacao.NaoEnviado => ("⚠ seleção local [não enviado]", ThemeManager.LaranjaAlerta),
            Situacao.SemResposta => ("⚠ sem resposta do firmware", ThemeManager.LaranjaAlerta),
            Situacao.ErroFirmware => ("❌ ERRO: Rejeitado pelo hardware!", ThemeManager.VermelhoPerigo),
            _ => ("", ThemeManager.TextoSecundario)
        };

        private static Situacao SituacaoDe(ResultadoComando resultado) => resultado switch
        {
            ResultadoComando.Confirmado => Situacao.Confirmado,
            ResultadoComando.ErroFirmware => Situacao.ErroFirmware,
            ResultadoComando.NaoEnviado => Situacao.NaoEnviado,
            _ => Situacao.SemResposta
        };

        private static string DetalheDe(ConclusaoComando c) => c.Detalhe ?? c.Resultado switch
        {
            ResultadoComando.ErroFirmware when c.Resposta?.Motivo == MotivoErroFirmware.CPIndisponivel =>
                "O firmware informou que o CP não existe nesta placa (Nano). Use o Arduino Mega para emular o CP.",
            ResultadoComando.ErroFirmware => $"Resposta do firmware: {c.Resposta?.Linha}",
            ResultadoComando.NaoEnviado => BancadaEngine.DetalheSemConexao,
            ResultadoComando.SemResposta => "O firmware não respondeu no prazo: o estado real do hardware é desconhecido.",
            _ => $"Resposta do firmware: {c.Resposta?.Linha}"
        };

        /// <summary>
        /// Linha "Firmware" das colunas CP e PP: mostra EstadoConfirmado/CaboConfirmado (a verdade do hardware),
        /// separada da seleção local, e destaca no grupo o estado confirmado.
        /// </summary>
        private void AtualizarFirmware()
        {
            EstadoVeiculoCP? cp = _engine.EstadoConfirmado;
            if (cp is { } estado)
            {
                bool igual = estado == EstadoSelecionadoCP;
                lblFirmwareCP.Text = $"Firmware: {estado.ObterCodigo()} ({FormatarTensao(estado.ObterTensaoNominal())})" +
                                     (igual ? " ✓" : $"{Environment.NewLine}≠ seleção local");
                lblFirmwareCP.ForeColor = igual ? ThemeManager.CorAcento : ThemeManager.LaranjaAlerta;
            }
            else
            {
                lblFirmwareCP.Text = $"Firmware: —{Environment.NewLine}(sem confirmação)";
                lblFirmwareCP.ForeColor = ThemeManager.TextoSecundario;
            }

            foreach (var (grupo, rb) in _radiosCP)
            {
                bool confirmado = cp?.ObterGrupo() == grupo;
                rb.ForeColor = confirmado ? ThemeManager.CorAcento
                             : grupo == GrupoEstadoCP.E ? ThemeManager.VermelhoPerigo
                             : ThemeManager.TextoPrincipal;
            }

            EstadoCaboPP? pp = _engine.CaboConfirmado;
            if (pp is { } cabo)
            {
                bool igual = cabo == _engine.CaboAtual;
                lblFirmwarePP.Text = $"Firmware: canal {(int)cabo}" + (igual ? " ✓" : " ≠ seleção local");
                lblFirmwarePP.ForeColor = igual ? ThemeManager.CorAcento : ThemeManager.LaranjaAlerta;
            }
            else
            {
                lblFirmwarePP.Text = "Firmware: — (sem confirmação)";
                lblFirmwarePP.ForeColor = ThemeManager.TextoSecundario;
            }

            foreach (var (rb, c) in _radiosPP)
            {
                bool confirmado = pp == c;
                rb.ForeColor = confirmado ? ThemeManager.CorAcento
                             : c == EstadoCaboPP.FalhaCurto ? ThemeManager.VermelhoPerigo
                             : ThemeManager.TextoPrincipal;
            }
        }

        private void btnStatusCP_Click(object? sender, EventArgs e)
        {
            _engine.SolicitarStatusCP(); // NaoEnviado/ERRO/sem resposta chegam via ComandoConcluido
        }

        // ------------------------------------------------------------------ Pulso de reenvio (400 ms)

        // Fim do pulso: ainda sem conclusão -> "aguardando"; já confirmado -> formato padrão de confirmação
        private void TimerPulsoCP_Tick(object? sender, EventArgs e)
        {
            _timerPulsoCP.Stop();
            if (!PodeAtualizarUI) return;

            if (_reenvioCP)
            {
                MostrarSelecaoCP(EstadoSelecionadoCP, Situacao.Aguardando);
            }
            else if (_restaurarCP is { } r)
            {
                _restaurarCP = null;
                MostrarSelecaoCP(r.Estado, Situacao.Confirmado, r.Detalhe);
            }
        }

        private void TimerPulsoPP_Tick(object? sender, EventArgs e)
        {
            _timerPulsoPP.Stop();
            if (!PodeAtualizarUI) return;

            if (_reenvioPP)
            {
                MostrarSelecaoCabo(_engine.CaboAtual, Situacao.Aguardando);
            }
            else if (_restaurarPP is { } r)
            {
                _restaurarPP = null;
                MostrarSelecaoCabo(r.Cabo, Situacao.Confirmado, r.Detalhe);
            }
        }

        // ------------------------------------------------------------------ Diagnóstico do firmware

        private void btnDebugStack_Click(object? sender, EventArgs e)
        {
            _engine.SolicitarDebugStack(); // resposta via RespostaMultilinhaRecebida; NaoEnviado via ComandoConcluido
        }

        private void btnHelp_Click(object? sender, EventArgs e)
        {
            _engine.SolicitarHelp(); // linhas via RespostaRecebida/RespostaMultilinhaRecebida
        }

        // ------------------------------------------------------------------ Terminal / comando manual

        private void btnEnviar_Click(object sender, EventArgs e)
        {
            string cmd = txtComando.Text.Trim();
            if (string.IsNullOrEmpty(cmd)) return;

            // Validação local (mesma regra do Core): só ASCII imprimível; nada é transmitido
            if (!BancadaEngine.ComandoManualValido(cmd))
            {
                AppendTerminal($"[não enviado] {cmd} ({BancadaEngine.DetalheCaractereInvalido})");
                return;
            }

            // Em falha, o "[não enviado]" vem do ComandoConcluido; o texto fica para reenviar
            if (_engine.EnviarComandoManual(cmd))
            {
                txtComando.Clear();
            }
        }

        private void txtComando_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnEnviar_Click(sender, EventArgs.Empty);
                e.SuppressKeyPress = true;
            }
        }

        private void btnLimpar_Click(object sender, EventArgs e)
        {
            txtTerminal.Clear();
        }

        // ------------------------------------------------------------------ Eventos do motor (qualquer thread)

        /// <summary>
        /// true se já está na thread da UI e pode seguir; false se a form está fechando
        /// ou se a chamada foi reagendada na UI (via <see cref="InvocarNaUI"/>).
        /// </summary>
        private bool NaThreadDaUI(Action reexecutar)
        {
            if (!PodeAtualizarUI) return false;
            if (!this.InvokeRequired) return true;

            InvocarNaUI(reexecutar);
            return false;
        }

        private void Engine_ComandoEnviado(object? sender, string cmd)
        {
            if (!NaThreadDaUI(() => Engine_ComandoEnviado(sender, cmd))) return;

            // Só registra: pode chegar depois da conclusão, então não mexe nos indicadores
            AppendTerminal($"[TX] -> {cmd}");
        }

        private void Engine_RespostaRecebida(object? sender, RespostaFirmware r)
        {
            if (!NaThreadDaUI(() => Engine_RespostaRecebida(sender, r))) return;

            AppendTerminal(r.EhErro ? $"⚠️ [RX] <- {r.Linha}" : $"[RX] <- {r.Linha}");
            AtualizarFirmware();
        }

        /// <summary>A verdade final de cada comando: só aqui o indicador sai de "aguardando".</summary>
        private void Engine_ComandoConcluido(object? sender, ConclusaoComando c)
        {
            if (!NaThreadDaUI(() => Engine_ComandoConcluido(sender, c))) return;

            switch (c.Resultado)
            {
                case ResultadoComando.NaoEnviado:
                    AppendTerminal($"[não enviado] {c.Comando} ({DetalheDe(c)})");
                    break;
                case ResultadoComando.SemResposta:
                    AppendTerminal($"[sem confirmação] {c.Comando} ({DetalheDe(c)})");
                    break;
                case ResultadoComando.ErroFirmware when c.Resposta?.Motivo == MotivoErroFirmware.CPIndisponivel:
                    AppendTerminal("💡 CP indisponível nesta placa (Nano): use o Arduino Mega para emular o CP.");
                    break;
            }

            Situacao situacao = SituacaoDe(c.Resultado);
            string detalhe = DetalheDe(c);

            // Só a conclusão do último pedido da UI mexe no indicador (conclusões mais velhas são ignoradas)
            if (c.Estado is { } estado && EhPedidoAtual(c, _idPedidoCP, _enviandoCP))
            {
                _idPedidoCP = 0;
                if (_reenvioCP && c.Resultado == ResultadoComando.Confirmado)
                {
                    // "✓ Reenviado e confirmado" por um pulso; depois volta ao formato padrão (TimerPulsoCP_Tick)
                    MostrarSelecaoCP(estado, Situacao.ReenviadoConfirmado, detalhe);
                    _restaurarCP = (estado, detalhe);
                    _timerPulsoCP.Stop();
                    _timerPulsoCP.Start();
                }
                else
                {
                    _timerPulsoCP.Stop();
                    MostrarSelecaoCP(estado, situacao, detalhe);
                }
                _reenvioCP = false;
            }

            if (c.Cabo is { } cabo && EhPedidoAtual(c, _idPedidoPP, _enviandoPP))
            {
                _idPedidoPP = 0;
                if (_reenvioPP && c.Resultado == ResultadoComando.Confirmado)
                {
                    MostrarSelecaoCabo(cabo, Situacao.ReenviadoConfirmado, detalhe);
                    _restaurarPP = (cabo, detalhe);
                    _timerPulsoPP.Stop();
                    _timerPulsoPP.Start();
                }
                else
                {
                    _timerPulsoPP.Stop();
                    MostrarSelecaoCabo(cabo, situacao, detalhe);
                }
                _reenvioPP = false;
            }

            // Os relés ativos só vêm no [STATUS CP]: depois de cada OK de estado (da UI ou manual), pede o status,
            // a menos que um estado mais novo da UI ainda esteja pendente (o OK dele pedirá o status)
            if (c.Estado != null && c.Resultado == ResultadoComando.Confirmado && _idPedidoCP == 0 && _engine.EstaConectado)
            {
                _engine.SolicitarStatusCP();
            }

            // DTR: o status pedido ao conectar se perdeu no reinício da placa -> uma nova tentativa em 2,5 s
            if (c.Id == _idStatusInicial && c.Resultado == ResultadoComando.SemResposta
                && !_retentativaStatusFeita && _engine.EstaConectado)
            {
                _retentativaStatusFeita = true;
                _idStatusInicial = 0;
                AppendTerminal("Aguardando a placa reiniciar... (nova tentativa de status em 2,5 s)");
                _timerRetentativaStatus.Stop();
                _timerRetentativaStatus.Start();
            }

            AtualizarFirmware();
        }

        private bool EhPedidoAtual(ConclusaoComando c, long idPedido, bool enviando) =>
            (idPedido != 0 && c.Id == idPedido) || (enviando && c.Id == _engine.UltimoIdComando);

        private void Engine_StatusCPRecebido(object? sender, EstadoVeiculoCP estado)
        {
            if (!NaThreadDaUI(() => Engine_StatusCPRecebido(sender, estado))) return;

            // Sincronização sem efeito colateral: a seleção local não é alterada
            if (estado != EstadoSelecionadoCP)
            {
                AppendTerminal($"ℹ️ Firmware em {estado.ObterCodigo()}; seleção local em {EstadoSelecionadoCP.ObterCodigo()} (não alterada).");
            }
            AtualizarFirmware();
        }

        private void Engine_RelesCPAtualizados(object? sender, IReadOnlyList<int> pinos)
        {
            if (!NaThreadDaUI(() => Engine_RelesCPAtualizados(sender, pinos))) return;

            MostrarReles(pinos);
        }

        /// <summary>
        /// Resposta completa de help/debug stack. As linhas já aparecem uma a uma como [RX]; aqui só o resumo
        /// (o diagnóstico de stack interpretado pelo Core, ou o aviso de que nada chegou no prazo).
        /// </summary>
        private void Engine_RespostaMultilinhaRecebida(object? sender, RespostaMultilinha r)
        {
            if (!NaThreadDaUI(() => Engine_RespostaMultilinhaRecebida(sender, r))) return;

            if (r.Linhas.Count == 0)
            {
                AppendTerminal($"[sem resposta] {r.Comando} (nenhuma linha recebida no prazo)");
            }
            else if (r.Stack is { } s)
            {
                AppendTerminal($"[STACK] SerialCLI livre: {s.SerialCLILivre} de {s.SerialCLITotal} | " +
                               $"Blink livre: {s.BlinkLivre} de {s.BlinkTotal}");
            }
            else
            {
                AppendTerminal($"ℹ️ {r.Comando}: {r.Linhas.Count} linha(s) recebida(s).");
            }
        }

        private void Engine_ConexaoPerdida(object? sender, EventArgs e)
        {
            if (!NaThreadDaUI(() => Engine_ConexaoPerdida(sender, e))) return;

            CancelarRetentativaStatus();
            ApagarReles();
            lblStatus.Text = "Conexão Perdida (Reconectando...)";
            lblStatus.ForeColor = ThemeManager.LaranjaAlerta;
            pnlStatusLed.BackColor = ThemeManager.LaranjaAlerta;
            AppendTerminal("⚠️ Conexão interrompida. Tentando reconectar automaticamente...");
        }

        private void Engine_Reconectado(object? sender, EventArgs e)
        {
            if (!NaThreadDaUI(() => Engine_Reconectado(sender, e))) return;

            AtualizarEstadoUI(conectado: true);
            AppendTerminal("✅ Conexão com o Arduino restabelecida!");
            SincronizarComFirmware();
        }

        private void AppendTerminal(string mensagem)
        {
            if (txtTerminal.IsDisposed) return;
            string linha = $"[{DateTime.Now:HH:mm:ss}] {mensagem}{Environment.NewLine}";

            // Teto de memória igual ao Supervisório: acima de 50.000 caracteres, descarta a metade mais antiga
            if (txtTerminal.TextLength + linha.Length > LimiteTerminal)
            {
                string atual = txtTerminal.Text + linha;
                txtTerminal.Text = atual.Substring(Math.Max(0, atual.Length - LimiteTerminal / 2));
                txtTerminal.SelectionStart = txtTerminal.TextLength;
                txtTerminal.ScrollToCaret();
            }
            else if (chkAutoScroll.Checked)
            {
                txtTerminal.AppendText(linha);
            }
            else
            {
                AcrescentarSemRolar(linha);
            }
        }

        // "Rolar ao final" desmarcado: acrescenta a linha mantendo a posição de leitura e a seleção do operador
        private void AcrescentarSemRolar(string linha)
        {
            IntPtr h = txtTerminal.Handle;
            int primeiraVisivel = (int)SendMessage(h, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero);
            int inicio = txtTerminal.SelectionStart;
            int tamanho = txtTerminal.SelectionLength;

            SendMessage(h, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            try
            {
                txtTerminal.AppendText(linha);
                txtTerminal.Select(inicio, tamanho);
                int delta = primeiraVisivel - (int)SendMessage(h, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero);
                if (delta != 0) SendMessage(h, EM_LINESCROLL, IntPtr.Zero, delta);
            }
            finally
            {
                SendMessage(h, WM_SETREDRAW, 1, IntPtr.Zero);
                txtTerminal.Invalidate();
            }
        }

        private void chkAutoScroll_CheckedChanged(object? sender, EventArgs e)
        {
            if (!chkAutoScroll.Checked) return;
            txtTerminal.SelectionStart = txtTerminal.TextLength;
            txtTerminal.ScrollToCaret();
        }

        private const int WM_SETREDRAW = 0x000B;
        private const int EM_GETFIRSTVISIBLELINE = 0x00CE;
        private const int EM_LINESCROLL = 0x00B6;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        // Guarda para callbacks vindos da thread da serial: a form pode estar fechando/descartada
        private bool PodeAtualizarUI => !this.IsDisposed && !this.Disposing && this.IsHandleCreated;

        private void InvocarNaUI(Action acao)
        {
            if (!PodeAtualizarUI) return;
            try
            {
                this.BeginInvoke(acao);
            }
            catch (InvalidOperationException)
            {
                // Handle destruído entre a checagem e o BeginInvoke (fechamento concorrente)
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _timerRetentativaStatus.Stop();
            _timerRetentativaStatus.Tick -= TimerRetentativaStatus_Tick;
            _timerPulsoCP.Stop();
            _timerPulsoCP.Tick -= TimerPulsoCP_Tick;
            _timerPulsoPP.Stop();
            _timerPulsoPP.Tick -= TimerPulsoPP_Tick;

            // Desassina antes do Dispose: nenhum callback da serial chega com a form fechando
            _engine.ComandoEnviado -= Engine_ComandoEnviado;
            _engine.RespostaRecebida -= Engine_RespostaRecebida;
            _engine.ComandoConcluido -= Engine_ComandoConcluido;
            _engine.StatusCPRecebido -= Engine_StatusCPRecebido;
            _engine.RelesCPAtualizados -= Engine_RelesCPAtualizados;
            _engine.RespostaMultilinhaRecebida -= Engine_RespostaMultilinhaRecebida;
            _engine.ConexaoPerdida -= Engine_ConexaoPerdida;
            _engine.Reconectado -= Engine_Reconectado;
            _engine.Dispose();
            base.OnFormClosing(e);
        }
    }
}
