using System;
using System.Drawing;
using System.Windows.Forms;
using DevSandbox.Core.Serial;
using DevSandbox.UI.Common;

namespace DevSandbox.UI.Forms
{
    public partial class FormSupervisorio : Form
    {
        private readonly SupervisorioSerialEngine _engine = new();

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
                try
                {
                    _engine.EnviarDados(mensagem);
                    AppendHistorico($"[{DateTime.Now:HH:mm:ss}] Enviado -> {mensagem}");
                    // txtEnviar.Clear(); // Mantém o texto digitado após o envio
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao transmitir dados: {ex.Message}", "Falha de Envio", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            txtHistorico.Clear();
        }

        private void Engine_DadosRecebidos(object? sender, string dados)
        {
            if (this.IsDisposed || !this.IsHandleCreated || !_engine.EstaConectado) return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => Engine_DadosRecebidos(sender, dados)));
                return;
            }

            AppendHistorico($"[{DateTime.Now:HH:mm:ss}] Recebido <- {dados}");
        }

        private void Engine_ConexaoPerdida(object? sender, EventArgs e)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => Engine_ConexaoPerdida(sender, e)));
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
            if (this.IsDisposed || !this.IsHandleCreated) return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => Engine_Reconectado(sender, e)));
                return;
            }

            AtualizarEstadoUI(conectado: true);
            AppendHistorico($"[{DateTime.Now:HH:mm:ss}] ✅ SUCESSO: Comunicação restabelecida com a porta COM!");
        }

        private void AppendHistorico(string texto)
        {
            if (txtHistorico.TextLength > 50000)
            {
                txtHistorico.Text = txtHistorico.Text.Substring(25000);
            }

            txtHistorico.AppendText(texto + Environment.NewLine);
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _engine.Dispose();
            base.OnFormClosing(e);
        }
    }
}
