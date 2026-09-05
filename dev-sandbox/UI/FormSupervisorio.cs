using dev_sandbox.Core;
using System;
using System.Drawing;
using System.Reflection.Emit;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace dev_sandbox.UI
{
    public partial class FormSupervisorio : Form
    {
        private readonly SupervisorioSerialEngine _engine = new();

        public FormSupervisorio()
        {
            InitializeComponent();

            // Configurações do histórico
            txtHistorico.Multiline = true;
            txtHistorico.ScrollBars = ScrollBars.Vertical;
            txtHistorico.ReadOnly = true;
            txtHistorico.Font = new Font("Consolas", 10F, FontStyle.Regular);

            // Carrega portas e taxas
            CarregarPortasDisponiveis();
            CarregarBaudRates();

            // Vincula os eventos da UI e da Engine
            txtEnviar.KeyDown += txtEnviar_KeyDown;
            _engine.DadosRecebidos += Engine_DadosRecebidos;

            // Define estado inicial da interface (Desconectado)
            AtualizarEstadoUI(conectado: false);
        }

        private void CarregarPortasDisponiveis()
        {
            cboPortas.Items.Clear();

            string[] portas = _engine.ObterPortasDisponiveis();
            cboPortas.Items.AddRange(portas);

            if (cboPortas.Items.Count > 0)
            {
                if (cboPortas.Items.Contains("COM6"))
                {
                    cboPortas.SelectedItem = "COM6";
                }
                else
                {
                    cboPortas.SelectedIndex = 0;
                }
            }
            else
            {
                cboPortas.Text = "";
            }
        }

        private void CarregarBaudRates()
        {
            cboBaudRate.Items.Clear();
            cboBaudRate.Items.AddRange(new object[] { 9600, 19200, 38400, 57600, 115200 });
            cboBaudRate.SelectedIndex = 0;
        }

        private void btnAtualizarPortas_Click(object sender, EventArgs e)
        {
            CarregarPortasDisponiveis();
        }

        private void btnConectar_Click(object sender, EventArgs e)
        {
            if (_engine.EstaConectado)
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
            // Trava de segurança extra
            if (!_engine.EstaConectado)
            {
                MessageBox.Show("Conecte-se a uma porta serial antes de enviar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string mensagem = txtEnviar.Text;

            if (!string.IsNullOrWhiteSpace(mensagem))
            {
                _engine.EnviarDados(mensagem);
                txtHistorico.AppendText($"[{DateTime.Now:HH:mm:ss}] Enviado -> {mensagem}{Environment.NewLine}");

                //nao qeuro isso
                //txtEnviar.Clear();
                //txtEnviar.Focus();
            }
        }

        private void txtEnviar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnEnviar_Click(sender, e);
                e.SuppressKeyPress = true;
            }
        }

        private void Engine_DadosRecebidos(object sender, string dados)
        {
            // Se estiver desconectado ou desconectando, descarta o dado recebido
            if (!_engine.EstaConectado) return;

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => Engine_DadosRecebidos(sender, dados)));
                return;
            }

            txtHistorico.AppendText($"[{DateTime.Now:HH:mm:ss}] Recebido <- {dados}{Environment.NewLine}");
        }

        private void AtualizarEstadoUI(bool conectado)
        {
            if (conectado)
            {
                btnConectar.Text = "Desconectar";
                lblStatus.Text = "Conectado";
                lblStatus.ForeColor = Color.Green;
                prgStatus.Value = 100;

                // Bloqueia configurações de porta
                cboPortas.Enabled = false;
                cboBaudRate.Enabled = false;
                btnAtualizarPortas.Enabled = false;

                // Libera campo de envio
                txtEnviar.Enabled = true;
                btnEnviar.Enabled = true;
            }
            else
            {
                btnConectar.Text = "Conectar";
                lblStatus.Text = "Desconectado";
                lblStatus.ForeColor = Color.Red;
                prgStatus.Value = 0;

                // Libera configurações de porta
                cboPortas.Enabled = true;
                cboBaudRate.Enabled = true;
                btnAtualizarPortas.Enabled = true;

                // Bloqueia campo de envio e botão
                txtEnviar.Enabled = false;
                btnEnviar.Enabled = false;
            }
        }

        // Garante que a porta COM seja fechada ao fechar a janela no "X" ou Alt+F4
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_engine.EstaConectado)
            {
                _engine.Desconectar();
            }

            base.OnFormClosing(e);
        }
    }
}