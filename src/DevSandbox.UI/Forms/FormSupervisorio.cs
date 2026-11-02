using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevSandbox.Core.Serial;
using DevSandbox.UI.Common;

namespace DevSandbox.UI.Forms
{
    public partial class FormSupervisorio : Form
    {
        private readonly SupervisorioSerialEngine _engine = new();

        // Recepção em lotes: a thread da serial só acumula; o timer da UI despeja a cada ~50 ms
        private const int IntervaloLoteMs = 50;
        private const int LimiteHistorico = 50000;
        private readonly object _travaRecepcao = new();
        private readonly StringBuilder _bufferRecepcao = new();
        private readonly System.Windows.Forms.Timer _timerRecepcao = new() { Interval = IntervaloLoteMs };

        public FormSupervisorio()
        {
            InitializeComponent();

            ThemeManager.AplicarBarraDeTituloEscura(this);
            AplicarTema();

            CarregarPortasDisponiveis();
            CarregarBaudRates();

            _engine.DadosRecebidos += Engine_DadosRecebidos;
            _engine.ConexaoPerdida += Engine_ConexaoPerdida;
            _engine.Reconectado += Engine_Reconectado;

            txtEnviar.InnerKeyDown += txtEnviar_KeyDown;

            _timerRecepcao.Tick += TimerRecepcao_Tick;
            _timerRecepcao.Start();

            AtualizarEstadoUI(conectado: false);
        }

        private void AplicarTema()
        {
            this.BackColor = ThemeManager.FundoJanela;
            this.ForeColor = ThemeManager.TextoPrincipal;

            cardConfig.CardColor = ThemeManager.FundoCard;
            cardConfig.BorderColor = ThemeManager.BordaCard;

            ThemeManager.EstilarComboBox(cboPortas);
            ThemeManager.EstilarComboBox(cboBaudRate);

            txtHistorico.BackColor = ThemeManager.FundoInput;
            txtHistorico.ForeColor = ThemeManager.VerdeTerminal;
            txtHistorico.BorderStyle = BorderStyle.FixedSingle;

            btnLimpar.IsAccent = false;
            btnAtualizarPortas.IsAccent = false;
            btnEnviar.IsAccent = true;
        }

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
            cboBaudRate.Items.AddRange(new object[] { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600 });
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
                _engine.Desconectar();
                AtualizarEstadoUI(conectado: false);
                return;
            }

            string portaSelecionada = cboPortas.Text;

            if (string.IsNullOrWhiteSpace(portaSelecionada))
            {
                MessageBox.Show("Selecione uma porta COM válida.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int baudRateSelecionado = Convert.ToInt32(cboBaudRate.SelectedItem);
                _engine.Conectar(portaSelecionada, baudRateSelecionado);
                AtualizarEstadoUI(conectado: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir a porta: {ex.Message}", "Falha de Conexão", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AtualizarEstadoUI(conectado: false);
            }
        }

        private void btnEnviar_Click(object sender, EventArgs e)
        {
            if (!_engine.EstaConectado)
            {
                MessageBox.Show("Conecte-se a uma porta serial antes de enviar dados.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string mensagem = txtEnviar.Text;

            if (!string.IsNullOrWhiteSpace(mensagem))
            {
                if (_engine.EnviarDados(mensagem))
                {
                    AppendHistorico($"[{DateTime.Now:HH:mm:ss}] Enviado -> {mensagem}");
                    // txtEnviar.Clear(); // Mantém o texto digitado após o envio
                }
                else
                {
                    // Em falha de E/S o Core já disparou ConexaoPerdida (alerta de reconexão no histórico);
                    // aqui só registra que esta mensagem não saiu, sem caixa modal e sem repetir o alerta.
                    AppendHistorico($"[{DateTime.Now:HH:mm:ss}] ✖ Não enviado -> {mensagem}");
                }
            }
        }

        private void txtEnviar_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnEnviar_Click(sender ?? this, EventArgs.Empty);
                e.SuppressKeyPress = true;
            }
        }

        private void btnLimpar_Click(object sender, EventArgs e)
        {
            lock (_travaRecepcao) { _bufferRecepcao.Clear(); }
            txtHistorico.Clear();
        }

        private void Engine_DadosRecebidos(object? sender, string dados)
        {
            // Executa na thread da serial: apenas acumula (sem BeginInvoke por pedaço)
            if (!_engine.EstaConectado) return;

            string linha = $"[{DateTime.Now:HH:mm:ss}] Recebido <- {dados}{Environment.NewLine}";
            lock (_travaRecepcao)
            {
                _bufferRecepcao.Append(linha);

                // Proteção de memória se a UI ficar parada: mantém só a cauda do buffer
                if (_bufferRecepcao.Length > LimiteHistorico)
                {
                    _bufferRecepcao.Remove(0, _bufferRecepcao.Length - LimiteHistorico / 2);
                }
            }
        }

        private void TimerRecepcao_Tick(object? sender, EventArgs e)
        {
            string lote;
            lock (_travaRecepcao)
            {
                if (_bufferRecepcao.Length == 0) return;
                lote = _bufferRecepcao.ToString();
                _bufferRecepcao.Clear();
            }

            AppendBloco(lote);
        }

        /// <summary>
        /// Despeja na tela o que ainda está no buffer, preservando a ordem cronológica
        /// antes de mensagens geradas pela própria UI (envio, alertas).
        /// </summary>
        private void DescarregarRecepcaoPendente() => TimerRecepcao_Tick(this, EventArgs.Empty);

        private void Engine_ConexaoPerdida(object? sender, EventArgs e)
        {
            if (!PodeAtualizarUI) return;

            if (this.InvokeRequired)
            {
                InvocarNaUI(() => Engine_ConexaoPerdida(sender, e));
                return;
            }

            lblStatus.Text = "Conexão Perdida (Reconectando...)";
            lblStatus.ForeColor = ThemeManager.LaranjaAlerta;
            prgStatus.Value = 50;

            btnConectar.Text = "Desconectar";
            btnConectar.IsAccent = true;
            btnConectar.CustomAccentColor = ThemeManager.VermelhoPerigo;
            btnConectar.CustomTextColor = Color.White;

            txtEnviar.Enabled = false;
            btnEnviar.Enabled = false;

            AppendHistorico($"[{DateTime.Now:HH:mm:ss}] ⚠️ ALERTA: Conexão interrompida. Tentando reconectar...");
        }

        private void Engine_Reconectado(object? sender, EventArgs e)
        {
            if (!PodeAtualizarUI) return;

            if (this.InvokeRequired)
            {
                InvocarNaUI(() => Engine_Reconectado(sender, e));
                return;
            }

            AtualizarEstadoUI(conectado: true);
            AppendHistorico($"[{DateTime.Now:HH:mm:ss}] ✅ SUCESSO: Comunicação restabelecida com a porta COM!");
        }

        private void AppendHistorico(string texto)
        {
            DescarregarRecepcaoPendente();
            AppendBloco(texto + Environment.NewLine);
        }

        private void AppendBloco(string bloco)
        {
            if (txtHistorico.IsDisposed) return;

            if (txtHistorico.TextLength + bloco.Length > LimiteHistorico)
            {
                string atual = txtHistorico.Text + bloco;
                txtHistorico.Text = atual.Substring(Math.Max(0, atual.Length - LimiteHistorico / 2));
            }
            else
            {
                txtHistorico.AppendText(bloco);
            }

            txtHistorico.SelectionStart = txtHistorico.TextLength;
            txtHistorico.ScrollToCaret();
        }

        private void AtualizarEstadoUI(bool conectado)
        {
            if (conectado)
            {
                btnConectar.Text = "Desconectar";
                btnConectar.IsAccent = true;
                btnConectar.CustomAccentColor = ThemeManager.VermelhoPerigo;
                btnConectar.CustomTextColor = Color.White;

                lblStatus.Text = "Conectado";
                lblStatus.ForeColor = ThemeManager.VerdeTerminal;
                prgStatus.Value = 100;

                cboPortas.Enabled = false;
                cboBaudRate.Enabled = false;
                btnAtualizarPortas.Enabled = false;

                txtEnviar.Enabled = true;
                btnEnviar.Enabled = true;
            }
            else
            {
                btnConectar.Text = "Conectar";
                btnConectar.IsAccent = true;
                btnConectar.CustomAccentColor = ThemeManager.CorAcento;
                btnConectar.CustomTextColor = ThemeManager.CorAcentoTexto;

                lblStatus.Text = "Desconectado";
                lblStatus.ForeColor = ThemeManager.VermelhoPerigo;
                prgStatus.Value = 0;

                cboPortas.Enabled = true;
                cboBaudRate.Enabled = true;
                btnAtualizarPortas.Enabled = true;

                txtEnviar.Enabled = false;
                btnEnviar.Enabled = false;
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
            _timerRecepcao.Stop();
            _timerRecepcao.Tick -= TimerRecepcao_Tick;
            _timerRecepcao.Dispose();

            // Desassina antes do Dispose: nenhum callback da serial chega com a form fechando
            _engine.DadosRecebidos -= Engine_DadosRecebidos;
            _engine.ConexaoPerdida -= Engine_ConexaoPerdida;
            _engine.Reconectado -= Engine_Reconectado;
            _engine.Dispose();
            base.OnFormClosing(e);
        }
    }
}
