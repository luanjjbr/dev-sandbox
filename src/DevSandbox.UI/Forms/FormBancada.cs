using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
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
        private readonly ToolTip _dicas;

        /// <summary>Estados visuais do indicador (seção 6 do design), mais "local" e "aguardando".</summary>
        private enum Situacao { Local, Aguardando, Confirmado, NaoEnviado, ErroFirmware, SemResposta }

        public FormBancada()
        {
            InitializeComponent();

            components ??= new Container();
            _dicas = new ToolTip(components);
            _timerRetentativaStatus = new System.Windows.Forms.Timer(components) { Interval = AtrasoRetentativaStatusMs };
            _timerRetentativaStatus.Tick += TimerRetentativaStatus_Tick;

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

            ThemeManager.AplicarBarraDeTituloEscura(this);
            AplicarTema();
            ConfigurarRotulos();

            CarregarPortasDisponiveis();
            CarregarBaudRates();

            _engine.ComandoEnviado += Engine_ComandoEnviado;
            _engine.RespostaRecebida += Engine_RespostaRecebida;
            _engine.ComandoConcluido += Engine_ComandoConcluido;
            _engine.StatusCPRecebido += Engine_StatusCPRecebido;
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

            txtTerminal.BackColor = ThemeManager.FundoInput;
            txtTerminal.ForeColor = ThemeManager.VerdeTerminal;

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
                if (!rb.Text.EndsWith("*", StringComparison.Ordinal)) rb.Text += " *";
                _dicas.SetToolTip(rb, $"Resistor de {cabo.ObterResistenciaNominal()} Ω: {nota}.");
            }

            _dicas.SetToolTip(lblNotaPP, $"Resistor da bancada física: {nota}.");
            _dicas.SetToolTip(btnStatusCP, "Pede ao firmware o estado CP atual (sem alterar a seleção).");
            _dicas.SetToolTip(pnlVariacaoCP, "Limites de tolerância: só nos estados B, C e D.");
        }

        private static string RotuloGrupo(GrupoEstadoCP grupo) => grupo switch
        {
            GrupoEstadoCP.A => "Desconectado",
            GrupoEstadoCP.B => "Conectado / Standby",
            GrupoEstadoCP.C => "Carga normal",
            GrupoEstadoCP.D => "Carga c/ ventilação",
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
                int baudRate = Convert.ToInt32(cboBaudRate.SelectedItem);
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
                lblStatus.Text = $"Conectado ({cboPortas.Text})";
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

        // ------------------------------------------------------------------ PP

        private void rbPP_CheckedChanged(object? sender, EventArgs e)
        {
            if (_carregandoUI) return;
            if (sender is not RadioButton rb || !rb.Checked) return;
            if (!_radiosPP.TryGetValue(rb, out EstadoCaboPP cabo)) return;

            MostrarSelecaoCabo(cabo, Situacao.Aguardando); // antes do Core: a conclusão nunca é sobrescrita
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

        // ------------------------------------------------------------------ CP (dois níveis)

        private void rbCP_CheckedChanged(object? sender, EventArgs e)
        {
            if (_carregandoUI) return;
            if (sender is not RadioButton rb || !rb.Checked) return;

            GrupoEstadoCP grupo = _radiosCP.First(kv => kv.Value == rb).Key;
            if (grupo != _grupoCP)
            {
                _variacaoCP = VariacaoEstadoCP.Nominal; // troca de grupo volta à variação nominal
            }
            _grupoCP = grupo;

            AtualizarVariacaoUI();
            EnviarEstadoSelecionado();
        }

        private void btnVariacao_Click(object? sender, EventArgs e)
        {
            if (!_grupoCP.TemVariacao()) return;
            if (sender is not FluentButton btn || !_botoesVariacao.TryGetValue(btn, out VariacaoEstadoCP variacao)) return;

            _variacaoCP = variacao;
            AtualizarVariacaoUI();
            EnviarEstadoSelecionado();
        }

        private void EnviarEstadoSelecionado()
        {
            EstadoVeiculoCP estado = EstadoSelecionadoCP;
            MostrarSelecaoCP(estado, Situacao.Aguardando); // antes do Core: a conclusão nunca é sobrescrita
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
            _ => "Nominal"
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
            Situacao.Aguardando => ("⋯ aguardando confirmação", ThemeManager.TextoSecundario),
            Situacao.Confirmado => ("✓ confirmado pelo firmware", ThemeManager.CorAcento),
            Situacao.NaoEnviado => ("[não enviado]", ThemeManager.LaranjaAlerta),
            Situacao.SemResposta => ("⚠ sem confirmação", ThemeManager.LaranjaAlerta),
            Situacao.ErroFirmware => ("❌ ERRO: rejeitado pelo hardware!", ThemeManager.VermelhoPerigo),
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
                MostrarSelecaoCP(estado, situacao, detalhe);
            }

            if (c.Cabo is { } cabo && EhPedidoAtual(c, _idPedidoPP, _enviandoPP))
            {
                _idPedidoPP = 0;
                MostrarSelecaoCabo(cabo, situacao, detalhe);
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

        private void Engine_ConexaoPerdida(object? sender, EventArgs e)
        {
            if (!NaThreadDaUI(() => Engine_ConexaoPerdida(sender, e))) return;

            CancelarRetentativaStatus();
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
            else
            {
                txtTerminal.AppendText(linha);
            }
        }

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

            // Desassina antes do Dispose: nenhum callback da serial chega com a form fechando
            _engine.ComandoEnviado -= Engine_ComandoEnviado;
            _engine.RespostaRecebida -= Engine_RespostaRecebida;
            _engine.ComandoConcluido -= Engine_ComandoConcluido;
            _engine.StatusCPRecebido -= Engine_StatusCPRecebido;
            _engine.ConexaoPerdida -= Engine_ConexaoPerdida;
            _engine.Reconectado -= Engine_Reconectado;
            _engine.Dispose();
            base.OnFormClosing(e);
        }
    }
}
