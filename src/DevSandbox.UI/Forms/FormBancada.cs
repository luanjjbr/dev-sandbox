using System;
using System.Drawing;
using System.Windows.Forms;
using DevSandbox.Core.Bancada;
using DevSandbox.UI.Common;

namespace DevSandbox.UI.Forms
{
    public partial class FormBancada : Form
    {
        private readonly BancadaEngine _engine = new();
        private bool _carregandoUI = true;

        public FormBancada()
        {
            InitializeComponent();

            ThemeManager.AplicarBarraDeTituloEscura(this);
            AplicarTema();

            CarregarPortasDisponiveis();
            CarregarBaudRates();

            _engine.DadosRecebidos += Engine_DadosRecebidos;
            _engine.ComandoEnviado += Engine_ComandoEnviado;
            _engine.ConexaoPerdida += Engine_ConexaoPerdida;
            _engine.Reconectado += Engine_Reconectado;

            _carregandoUI = false;
            AtualizarEstadoUI(conectado: false);
            AppendTerminal("Bancada Engine inicializada. Selecione a porta e conecte.");
        }

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

            txtTerminal.BackColor = ThemeManager.FundoInput;
            txtTerminal.ForeColor = ThemeManager.VerdeTerminal;

            txtComando.BackColor = ThemeManager.FundoInput;
            txtComando.ForeColor = ThemeManager.TextoPrincipal;

            panelFooter.BackColor = ThemeManager.FundoJanela;
            lblFooter.ForeColor = ThemeManager.TextoSecundario;
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir a porta: {ex.Message}", "Falha de Conexão", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AtualizarEstadoUI(conectado: false);
            }
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

        private void rbPP_CheckedChanged(object? sender, EventArgs e)
        {
            if (_carregandoUI) return;
            if (sender is not RadioButton rb || !rb.Checked) return;

            EstadoCaboPP cabo = EstadoCaboPP.CircuitoAberto;

            if (rb == rbPP0) cabo = EstadoCaboPP.CircuitoAberto;
            else if (rb == rbPP1) cabo = EstadoCaboPP.Desconectado;
            else if (rb == rbPP2) cabo = EstadoCaboPP.Cabo13A;
            else if (rb == rbPP3) cabo = EstadoCaboPP.Cabo20A;
            else if (rb == rbPP4) cabo = EstadoCaboPP.Cabo32A;
            else if (rb == rbPP5) cabo = EstadoCaboPP.Cabo63A;
            else if (rb == rbPP6) cabo = EstadoCaboPP.FalhaCurto;

            lblStatusCabo.Text = $"Ativo: {cabo.ObterDescricao()}";
            _engine.DefinirCabo(cabo);
        }

        private void rbCP_CheckedChanged(object? sender, EventArgs e)
        {
            if (_carregandoUI) return;
            if (sender is not RadioButton rb || !rb.Checked) return;

            EstadoVeiculoCP estado = EstadoVeiculoCP.EstadoA;

            if (rb == rbCPA) estado = EstadoVeiculoCP.EstadoA;
            else if (rb == rbCPB) estado = EstadoVeiculoCP.EstadoB;
            else if (rb == rbCPC) estado = EstadoVeiculoCP.EstadoC;
            else if (rb == rbCPD) estado = EstadoVeiculoCP.EstadoD;
            else if (rb == rbCPE) estado = EstadoVeiculoCP.EstadoE;

            lblStatusVeiculo.Text = $"Ativo: {estado.ObterDescricao()}";
            _engine.DefinirEstado(estado);
        }

        private void btnEnviar_Click(object sender, EventArgs e)
        {
            string cmd = txtComando.Text.Trim();
            if (string.IsNullOrEmpty(cmd)) return;

            _engine.EnviarComandoManual(cmd);
            txtComando.Clear();
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

        private void Engine_ComandoEnviado(object? sender, string cmd)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => Engine_ComandoEnviado(sender, cmd)));
                return;
            }

            AppendTerminal($"[TX] -> {cmd}");
        }

        private void Engine_DadosRecebidos(object? sender, string dados)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => Engine_DadosRecebidos(sender, dados)));
                return;
            }

            AppendTerminal($"[RX] <- {dados}");
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
            pnlStatusLed.BackColor = ThemeManager.LaranjaAlerta;
            AppendTerminal("⚠️ Conexão interrompida. Tentando reconectar automaticamente...");
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
            AppendTerminal("✅ Conexão com o Arduino restabelecida!");
        }

        private void AppendTerminal(string mensagem)
        {
            if (txtTerminal.IsDisposed) return;
            string linha = $"[{DateTime.Now:HH:mm:ss}] {mensagem}{Environment.NewLine}";
            txtTerminal.AppendText(linha);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _engine.Dispose();
            base.OnFormClosing(e);
        }
    }
}

