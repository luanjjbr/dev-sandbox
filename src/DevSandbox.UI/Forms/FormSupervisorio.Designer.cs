namespace DevSandbox.UI.Forms
{
    partial class FormSupervisorio
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            cardConfig = new DevSandbox.UI.Common.FluentCard();
            lblTituloConfig = new Label();
            btnLimpar = new DevSandbox.UI.Common.FluentButton();
            btnConectar = new DevSandbox.UI.Common.FluentButton();
            btnAtualizarPortas = new DevSandbox.UI.Common.FluentButton();
            lblStatus = new Label();
            prgStatus = new ProgressBar();
            cboBaudRate = new ComboBox();
            lblBaudRate = new Label();
            cboPortas = new ComboBox();
            lblPortas = new Label();
            txtHistorico = new TextBox();
            txtEnviar = new DevSandbox.UI.Common.FluentTextBox();
            btnEnviar = new DevSandbox.UI.Common.FluentButton();
            cardConfig.SuspendLayout();
            SuspendLayout();
            // 
            // cardConfig
            // 
            cardConfig.Controls.Add(lblTituloConfig);
            cardConfig.Controls.Add(btnLimpar);
            cardConfig.Controls.Add(btnConectar);
            cardConfig.Controls.Add(btnAtualizarPortas);
            cardConfig.Controls.Add(lblStatus);
            cardConfig.Controls.Add(prgStatus);
            cardConfig.Controls.Add(cboBaudRate);
            cardConfig.Controls.Add(lblBaudRate);
            cardConfig.Controls.Add(cboPortas);
            cardConfig.Controls.Add(lblPortas);
            cardConfig.CornerRadius = 8;
            cardConfig.Location = new Point(14, 12);
            cardConfig.Name = "cardConfig";
            cardConfig.Size = new Size(540, 122);
            cardConfig.TabIndex = 0;
            // 
            // lblTituloConfig
            // 
            lblTituloConfig.AutoSize = true;
            lblTituloConfig.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblTituloConfig.ForeColor = Color.FromArgb(160, 160, 160);
            lblTituloConfig.Location = new Point(14, 10);
            lblTituloConfig.Name = "lblTituloConfig";
            lblTituloConfig.Size = new Size(207, 15);
            lblTituloConfig.TabIndex = 9;
            lblTituloConfig.Text = "CONFIGURAÇÃO DA CONEXÃO SERIAL";
            // 
            // btnLimpar
            // 
            btnLimpar.Font = new Font("Segoe UI", 9F);
            btnLimpar.Location = new Point(430, 74);
            btnLimpar.Name = "btnLimpar";
            btnLimpar.Size = new Size(95, 32);
            btnLimpar.TabIndex = 8;
            btnLimpar.Text = "Limpar Log";
            btnLimpar.Click += btnLimpar_Click;
            // 
            // btnConectar
            // 
            btnConectar.IsAccent = true;
            btnConectar.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnConectar.Location = new Point(430, 32);
            btnConectar.Name = "btnConectar";
            btnConectar.Size = new Size(95, 34);
            btnConectar.TabIndex = 7;
            btnConectar.Text = "Conectar";
            btnConectar.Click += btnConectar_Click;
            // 
            // btnAtualizarPortas
            // 
            btnAtualizarPortas.Font = new Font("Segoe UI", 9F);
            btnAtualizarPortas.Location = new Point(190, 34);
            btnAtualizarPortas.Name = "btnAtualizarPortas";
            btnAtualizarPortas.Size = new Size(34, 28);
            btnAtualizarPortas.TabIndex = 2;
            btnAtualizarPortas.Text = "🔄";
            btnAtualizarPortas.Click += btnAtualizarPortas_Click;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
            lblStatus.Location = new Point(155, 80);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(94, 17);
            lblStatus.TabIndex = 6;
            lblStatus.Text = "Desconectado";
            // 
            // prgStatus
            // 
            prgStatus.Location = new Point(15, 78);
            prgStatus.Name = "prgStatus";
            prgStatus.Size = new Size(130, 20);
            prgStatus.TabIndex = 5;
            // 
            // cboBaudRate
            // 
            cboBaudRate.DropDownStyle = ComboBoxStyle.DropDownList;
            cboBaudRate.FormattingEnabled = true;
            cboBaudRate.Location = new Point(315, 37);
            cboBaudRate.Name = "cboBaudRate";
            cboBaudRate.Size = new Size(100, 23);
            cboBaudRate.TabIndex = 4;
            // 
            // lblBaudRate
            // 
            lblBaudRate.AutoSize = true;
            lblBaudRate.Font = new Font("Segoe UI", 9F);
            lblBaudRate.ForeColor = Color.FromArgb(160, 160, 160);
            lblBaudRate.Location = new Point(245, 41);
            lblBaudRate.Name = "lblBaudRate";
            lblBaudRate.Size = new Size(63, 15);
            lblBaudRate.TabIndex = 3;
            lblBaudRate.Text = "Baud Rate:";
            // 
            // cboPortas
            // 
            cboPortas.DropDownStyle = ComboBoxStyle.DropDownList;
            cboPortas.FormattingEnabled = true;
            cboPortas.Location = new Point(60, 37);
            cboPortas.Name = "cboPortas";
            cboPortas.Size = new Size(125, 23);
            cboPortas.TabIndex = 1;
            // 
            // lblPortas
            // 
            lblPortas.AutoSize = true;
            lblPortas.Font = new Font("Segoe UI", 9F);
            lblPortas.ForeColor = Color.FromArgb(160, 160, 160);
            lblPortas.Location = new Point(15, 41);
            lblPortas.Name = "lblPortas";
            lblPortas.Size = new Size(41, 15);
            lblPortas.TabIndex = 0;
            lblPortas.Text = "Porta: ";
            // 
            // txtHistorico
            // 
            txtHistorico.BackColor = Color.FromArgb(30, 30, 30);
            txtHistorico.BorderStyle = BorderStyle.FixedSingle;
            txtHistorico.Font = new Font("Consolas", 10F);
            txtHistorico.ForeColor = Color.FromArgb(34, 197, 94);
            txtHistorico.Location = new Point(14, 144);
            txtHistorico.Multiline = true;
            txtHistorico.Name = "txtHistorico";
            txtHistorico.ReadOnly = true;
            txtHistorico.ScrollBars = ScrollBars.Vertical;
            txtHistorico.Size = new Size(540, 348);
            txtHistorico.TabIndex = 1;
            // 
            // txtEnviar
            // 
            txtEnviar.Location = new Point(14, 502);
            txtEnviar.Name = "txtEnviar";
            txtEnviar.PlaceholderText = "Digite um comando ou texto para transmitir...";
            txtEnviar.Size = new Size(436, 34);
            txtEnviar.TabIndex = 2;
            // 
            // btnEnviar
            // 
            btnEnviar.IsAccent = true;
            btnEnviar.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnEnviar.Location = new Point(458, 502);
            btnEnviar.Name = "btnEnviar";
            btnEnviar.Size = new Size(96, 34);
            btnEnviar.TabIndex = 3;
            btnEnviar.Text = "Transmitir";
            btnEnviar.Click += btnEnviar_Click;
            // 
            // FormSupervisorio
            // 
            BackColor = Color.FromArgb(32, 32, 32);
            ForeColor = Color.FromArgb(255, 255, 255);
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(568, 548);
            Controls.Add(btnEnviar);
            Controls.Add(txtEnviar);
            Controls.Add(txtHistorico);
            Controls.Add(cardConfig);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "FormSupervisorio";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Supervisório Serial (UART / COM) • Fluent";
            cardConfig.ResumeLayout(false);
            cardConfig.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DevSandbox.UI.Common.FluentCard cardConfig;
        private Label lblTituloConfig;
        private Label lblPortas;
        private ComboBox cboPortas;
        private Label lblBaudRate;
        private ComboBox cboBaudRate;
        private DevSandbox.UI.Common.FluentButton btnAtualizarPortas;
        private ProgressBar prgStatus;
        private Label lblStatus;
        private DevSandbox.UI.Common.FluentButton btnConectar;
        private DevSandbox.UI.Common.FluentButton btnLimpar;
        private TextBox txtHistorico;
        private DevSandbox.UI.Common.FluentTextBox txtEnviar;
        private DevSandbox.UI.Common.FluentButton btnEnviar;
    }
}
