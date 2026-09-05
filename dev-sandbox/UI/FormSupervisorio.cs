using dev_sandbox.Core;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace dev_sandbox.UI
{
    public partial class FormSupervisorio : Form
    {
        private readonly SupervisorioSerialEngine _engine = new();

        // Importação da API do Windows para pintar a barra de título de escuro (Windows 10/11)
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public FormSupervisorio()
        {
            InitializeComponent();

            // Configurações do Janela
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Ativa o Dark Mode na barra de título nativa
            AtivarBarraDeTituloEscura();

            // Aplica a paleta Dark na interface
            AplicarTemaEscuro();

            // Carrega portas e taxas
            CarregarPortasDisponiveis();
            CarregarBaudRates();

            // Eventos
            txtEnviar.KeyDown += txtEnviar_KeyDown;
            _engine.DadosRecebidos += Engine_DadosRecebidos;
            _engine.ConexaoPerdida += Engine_ConexaoPerdida;
            _engine.Reconectado += Engine_Reconectado;

            // Estado Inicial
            AtualizarEstadoUI(conectado: false);
        }

        private void AtivarBarraDeTituloEscura()
        {
            try
            {
                int useDarkMode = 1;
                DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
            }
            catch
            {
                // Ignora em versões bem antigas do Windows
            }
        }

        private void AplicarTemaEscuro()
        {
            // Cores do Tema Dark
            Color fundoJanela = Color.FromArgb(24, 24, 27);       // Grafite Escuro
            Color fundoPaineis = Color.FromArgb(39, 39, 42);      // Cinza Médio
            Color fundoInputs = Color.FromArgb(18, 18, 18);       // Preto/Escuro
            Color textoClaro = Color.FromArgb(240, 240, 240);    // Branco Suave
            Color verdeTerminal = Color.FromArgb(34, 197, 94);   // Verde Neon

            // Formulário
            this.BackColor = fundoJanela;
            this.ForeColor = textoClaro;

            // GroupBox
            groupBox1.ForeColor = textoClaro;

            // Caixas de Seleção (ComboBoxes)
            EstilarComboBox(cboPortas, fundoPaineis, textoClaro);
            EstilarComboBox(cboBaudRate, fundoPaineis, textoClaro);

            // Histórico (Estilo Terminal Dark)
            txtHistorico.Multiline = true;
            txtHistorico.ScrollBars = ScrollBars.Vertical;
            txtHistorico.ReadOnly = true;
            txtHistorico.BackColor = fundoInputs;
            txtHistorico.ForeColor = verdeTerminal;
            txtHistorico.Font = new Font("Consolas", 10F, FontStyle.Regular);
            txtHistorico.BorderStyle = BorderStyle.FixedSingle;

            // Campo de Envio
            txtEnviar.BackColor = fundoInputs;
            txtEnviar.ForeColor = textoClaro;
            txtEnviar.Font = new Font("Segoe UI", 10F);
            txtEnviar.BorderStyle = BorderStyle.FixedSingle;

            // Botões Estilizados
            EstilarBotao(btnConectar, Color.FromArgb(37, 99, 235), Color.White);       // Azul
            EstilarBotao(btnEnviar, Color.FromArgb(22, 163, 74), Color.White);         // Verde
            EstilarBotao(btnAtualizarPortas, Color.FromArgb(71, 85, 105), Color.White); // Cinza
        }

        private void EstilarBotao(Button btn, Color corFundo, Color corTexto)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = corFundo;
            btn.ForeColor = corTexto;
            btn.Cursor = Cursors.Hand;
            btn.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        }

        private void EstilarComboBox(ComboBox cbo, Color fundo, Color texto)
        {
            cbo.FlatStyle = FlatStyle.Flat;
            cbo.BackColor = fundo;
            cbo.ForeColor = texto;
            cbo.Font = new Font("Segoe UI", 9F);
        }

        private void CarregarPortasDisponiveis()
        {
            cboPortas.Items.Clear();
            string[] portas = _engine.ObterPortasDisponiveis();
            cboPortas.Items.AddRange(portas);

            if (cboPortas.Items.Count > 0)
            {
                if (cboPortas.Items.Contains("COM6"))
                    cboPortas.SelectedItem = "COM6";
                else
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
            cboBaudRate.Items.AddRange(new object[] { 9600, 19200, 38400, 57600, 115200 });
            cboBaudRate.SelectedIndex = 0;
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
                MessageBox.Show("Conecte-se a uma porta serial antes de enviar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string mensagem = txtEnviar.Text;

            if (!string.IsNullOrWhiteSpace(mensagem))
            {
                _engine.EnviarDados(mensagem);
                txtHistorico.AppendText($"[{DateTime.Now:HH:mm:ss}] Enviado -> {mensagem}{Environment.NewLine}");
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
            if (!_engine.EstaConectado) return;

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => Engine_DadosRecebidos(sender, dados)));
                return;
            }

            txtHistorico.AppendText($"[{DateTime.Now:HH:mm:ss}] Recebido <- {dados}{Environment.NewLine}");
        }

        private void Engine_ConexaoPerdida(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => Engine_ConexaoPerdida(sender, e)));
                return;
            }

            lblStatus.Text = "Conexão Perdida (Reconectando...)";
            lblStatus.ForeColor = Color.FromArgb(245, 158, 11); // Amarelo/Laranja
            prgStatus.Value = 50;

            btnConectar.Text = "Desconectar";
            txtEnviar.Enabled = false;
            btnEnviar.Enabled = false;

            txtHistorico.AppendText($"[{DateTime.Now:HH:mm:ss}] ALERTA -> Conexão perdida. Tentando reconectar...{Environment.NewLine}");
        }

        private void Engine_Reconectado(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => Engine_Reconectado(sender, e)));
                return;
            }

            AtualizarEstadoUI(conectado: true);
            txtHistorico.AppendText($"[{DateTime.Now:HH:mm:ss}] SUCESSO -> Conexão reestabelecida!{Environment.NewLine}");
        }

        private void AtualizarEstadoUI(bool conectado)
        {
            if (conectado)
            {
                btnConectar.Text = "Desconectar";
                btnConectar.BackColor = Color.FromArgb(220, 38, 38); // Vermelho para Desconectar

                lblStatus.Text = "Conectado";
                lblStatus.ForeColor = Color.FromArgb(34, 197, 94);   // Verde Neon
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
                btnConectar.BackColor = Color.FromArgb(37, 99, 235); // Azul para Conectar

                lblStatus.Text = "Desconectado";
                lblStatus.ForeColor = Color.FromArgb(239, 68, 68);   // Vermelho
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
            _engine.Desconectar();
            base.OnFormClosing(e);
        }
    }
}