namespace DevSandbox.UI.Forms
{
    partial class FormBancada
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
            cardConexao = new DevSandbox.UI.Common.FluentCard();
            lblStatus = new Label();
            pnlStatusLed = new Panel();
            btnConectar = new DevSandbox.UI.Common.FluentButton();
            cboBaudRate = new ComboBox();
            lblBaudRate = new Label();
            btnAtualizarPortas = new DevSandbox.UI.Common.FluentButton();
            cboPortas = new ComboBox();
            lblPortas = new Label();
            cardCaboPP = new DevSandbox.UI.Common.FluentCard();
            lblStatusCabo = new Label();
            lblSubtituloPP = new Label();
            lblTituloPP = new Label();
            rbPP6 = new RadioButton();
            rbPP5 = new RadioButton();
            rbPP4 = new RadioButton();
            rbPP3 = new RadioButton();
            rbPP2 = new RadioButton();
            rbPP1 = new RadioButton();
            rbPP0 = new RadioButton();
            cardVeiculoCP = new DevSandbox.UI.Common.FluentCard();
            lblStatusVeiculo = new Label();
            lblSubtituloCP = new Label();
            lblTituloCP = new Label();
            rbCPE = new RadioButton();
            rbCPD = new RadioButton();
            rbCPC = new RadioButton();
            rbCPB = new RadioButton();
            rbCPA = new RadioButton();
            cardTerminal = new DevSandbox.UI.Common.FluentCard();
            btnLimpar = new DevSandbox.UI.Common.FluentButton();
            btnEnviar = new DevSandbox.UI.Common.FluentButton();
            txtComando = new TextBox();
            txtTerminal = new TextBox();
            lblTituloTerminal = new Label();
            panelFooter = new Panel();
            lblFooter = new Label();
            cardConexao.SuspendLayout();
            cardCaboPP.SuspendLayout();
            cardVeiculoCP.SuspendLayout();
            cardTerminal.SuspendLayout();
            panelFooter.SuspendLayout();
            SuspendLayout();
            // 
            // cardConexao
            // 
            cardConexao.Controls.Add(lblStatus);
            cardConexao.Controls.Add(pnlStatusLed);
            cardConexao.Controls.Add(btnConectar);
            cardConexao.Controls.Add(cboBaudRate);
            cardConexao.Controls.Add(lblBaudRate);
            cardConexao.Controls.Add(btnAtualizarPortas);
            cardConexao.Controls.Add(cboPortas);
            cardConexao.Controls.Add(lblPortas);
            cardConexao.Location = new Point(14, 12);
            cardConexao.Name = "cardConexao";
            cardConexao.Size = new Size(812, 68);
            cardConexao.TabIndex = 0;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 9F);
            lblStatus.ForeColor = Color.FromArgb(160, 160, 160);
            lblStatus.Location = new Point(485, 34);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(82, 15);
            lblStatus.TabIndex = 7;
            lblStatus.Text = "Desconectado";
            // 
            // pnlStatusLed
            // 
            pnlStatusLed.BackColor = Color.FromArgb(239, 68, 68);
            pnlStatusLed.Location = new Point(465, 36);
            pnlStatusLed.Name = "pnlStatusLed";
            pnlStatusLed.Size = new Size(12, 12);
            pnlStatusLed.TabIndex = 6;
            // 
            // btnConectar
            // 
            btnConectar.IsAccent = true;
            btnConectar.CornerRadius = 6;
            btnConectar.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnConectar.Location = new Point(340, 24);
            btnConectar.Name = "btnConectar";
            btnConectar.Size = new Size(110, 32);
            btnConectar.TabIndex = 5;
            btnConectar.Text = "Conectar";
            btnConectar.Click += btnConectar_Click;
            // 
            // cboBaudRate
            // 
            cboBaudRate.DropDownStyle = ComboBoxStyle.DropDownList;
            cboBaudRate.Font = new Font("Segoe UI", 9F);
            cboBaudRate.FormattingEnabled = true;
            cboBaudRate.Location = new Point(220, 29);
            cboBaudRate.Name = "cboBaudRate";
            cboBaudRate.Size = new Size(100, 23);
            cboBaudRate.TabIndex = 4;
            // 
            // lblBaudRate
            // 
            lblBaudRate.AutoSize = true;
            lblBaudRate.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblBaudRate.ForeColor = Color.FromArgb(160, 160, 160);
            lblBaudRate.Location = new Point(220, 11);
            lblBaudRate.Name = "lblBaudRate";
            lblBaudRate.Size = new Size(67, 15);
            lblBaudRate.TabIndex = 3;
            lblBaudRate.Text = "BAUD RATE";
            // 
            // btnAtualizarPortas
            // 
            btnAtualizarPortas.CornerRadius = 4;
            btnAtualizarPortas.Font = new Font("Segoe UI", 9F);
            btnAtualizarPortas.Location = new Point(170, 27);
            btnAtualizarPortas.Name = "btnAtualizarPortas";
            btnAtualizarPortas.Size = new Size(34, 27);
            btnAtualizarPortas.TabIndex = 2;
            btnAtualizarPortas.Text = "🔄";
            btnAtualizarPortas.Click += btnAtualizarPortas_Click;
            // 
            // cboPortas
            // 
            cboPortas.DropDownStyle = ComboBoxStyle.DropDownList;
            cboPortas.Font = new Font("Segoe UI", 9F);
            cboPortas.FormattingEnabled = true;
            cboPortas.Location = new Point(16, 29);
            cboPortas.Name = "cboPortas";
            cboPortas.Size = new Size(148, 23);
            cboPortas.TabIndex = 1;
            // 
            // lblPortas
            // 
            lblPortas.AutoSize = true;
            lblPortas.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblPortas.ForeColor = Color.FromArgb(160, 160, 160);
            lblPortas.Location = new Point(16, 11);
            lblPortas.Name = "lblPortas";
            lblPortas.Size = new Size(77, 15);
            lblPortas.TabIndex = 0;
            lblPortas.Text = "PORTA SERIAL";
            // 
            // cardCaboPP
            // 
            cardCaboPP.Controls.Add(lblStatusCabo);
            cardCaboPP.Controls.Add(lblSubtituloPP);
            cardCaboPP.Controls.Add(lblTituloPP);
            cardCaboPP.Controls.Add(rbPP6);
            cardCaboPP.Controls.Add(rbPP5);
            cardCaboPP.Controls.Add(rbPP4);
            cardCaboPP.Controls.Add(rbPP3);
            cardCaboPP.Controls.Add(rbPP2);
            cardCaboPP.Controls.Add(rbPP1);
            cardCaboPP.Controls.Add(rbPP0);
            cardCaboPP.Location = new Point(14, 88);
            cardCaboPP.Name = "cardCaboPP";
            cardCaboPP.Size = new Size(256, 420);
            cardCaboPP.TabIndex = 1;
            // 
            // lblStatusCabo
            // 
            lblStatusCabo.Font = new Font("Segoe UI", 8.5F);
            lblStatusCabo.ForeColor = Color.FromArgb(76, 194, 255);
            lblStatusCabo.Location = new Point(16, 375);
            lblStatusCabo.Name = "lblStatusCabo";
            lblStatusCabo.Size = new Size(224, 30);
            lblStatusCabo.TabIndex = 9;
            lblStatusCabo.Text = "Ativo: Circuito Aberto (∞ Ω)";
            // 
            // lblSubtituloPP
            // 
            lblSubtituloPP.AutoSize = true;
            lblSubtituloPP.Font = new Font("Segoe UI", 8F);
            lblSubtituloPP.ForeColor = Color.FromArgb(160, 160, 160);
            lblSubtituloPP.Location = new Point(16, 36);
            lblSubtituloPP.Name = "lblSubtituloPP";
            lblSubtituloPP.Size = new Size(166, 13);
            lblSubtituloPP.TabIndex = 8;
            lblSubtituloPP.Text = "Resistores PP-PE (IEC 62196-2)";
            // 
            // lblTituloPP
            // 
            lblTituloPP.AutoSize = true;
            lblTituloPP.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            lblTituloPP.ForeColor = Color.FromArgb(255, 255, 255);
            lblTituloPP.Location = new Point(14, 14);
            lblTituloPP.Name = "lblTituloPP";
            lblTituloPP.Size = new Size(177, 19);
            lblTituloPP.TabIndex = 7;
            lblTituloPP.Text = "🔌 Emulador de Cabo (PP)";
            // 
            // rbPP6
            // 
            rbPP6.AutoSize = true;
            rbPP6.Font = new Font("Segoe UI", 9F);
            rbPP6.ForeColor = Color.FromArgb(239, 68, 68);
            rbPP6.Location = new Point(18, 308);
            rbPP6.Name = "rbPP6";
            rbPP6.Size = new Size(155, 19);
            rbPP6.TabIndex = 6;
            rbPP6.TabStop = true;
            rbPP6.Text = "6: Falha / Curto (47 Ω)";
            rbPP6.UseVisualStyleBackColor = true;
            rbPP6.CheckedChanged += rbPP_CheckedChanged;
            // 
            // rbPP5
            // 
            rbPP5.AutoSize = true;
            rbPP5.Font = new Font("Segoe UI", 9F);
            rbPP5.ForeColor = Color.FromArgb(255, 255, 255);
            rbPP5.Location = new Point(18, 268);
            rbPP5.Name = "rbPP5";
            rbPP5.Size = new Size(149, 19);
            rbPP5.TabIndex = 5;
            rbPP5.TabStop = true;
            rbPP5.Text = "5: Cabo 63 A (100 Ω)";
            rbPP5.UseVisualStyleBackColor = true;
            rbPP5.CheckedChanged += rbPP_CheckedChanged;
            // 
            // rbPP4
            // 
            rbPP4.AutoSize = true;
            rbPP4.Font = new Font("Segoe UI", 9F);
            rbPP4.ForeColor = Color.FromArgb(255, 255, 255);
            rbPP4.Location = new Point(18, 228);
            rbPP4.Name = "rbPP4";
            rbPP4.Size = new Size(149, 19);
            rbPP4.TabIndex = 4;
            rbPP4.TabStop = true;
            rbPP4.Text = "4: Cabo 32 A (220 Ω)";
            rbPP4.UseVisualStyleBackColor = true;
            rbPP4.CheckedChanged += rbPP_CheckedChanged;
            // 
            // rbPP3
            // 
            rbPP3.AutoSize = true;
            rbPP3.Font = new Font("Segoe UI", 9F);
            rbPP3.ForeColor = Color.FromArgb(255, 255, 255);
            rbPP3.Location = new Point(18, 188);
            rbPP3.Name = "rbPP3";
            rbPP3.Size = new Size(149, 19);
            rbPP3.TabIndex = 3;
            rbPP3.TabStop = true;
            rbPP3.Text = "3: Cabo 20 A (680 Ω)";
            rbPP3.UseVisualStyleBackColor = true;
            rbPP3.CheckedChanged += rbPP_CheckedChanged;
            // 
            // rbPP2
            // 
            rbPP2.AutoSize = true;
            rbPP2.Font = new Font("Segoe UI", 9F);
            rbPP2.ForeColor = Color.FromArgb(255, 255, 255);
            rbPP2.Location = new Point(18, 148);
            rbPP2.Name = "rbPP2";
            rbPP2.Size = new Size(155, 19);
            rbPP2.TabIndex = 2;
            rbPP2.TabStop = true;
            rbPP2.Text = "2: Cabo 13 A (1500 Ω)";
            rbPP2.UseVisualStyleBackColor = true;
            rbPP2.CheckedChanged += rbPP_CheckedChanged;
            // 
            // rbPP1
            // 
            rbPP1.AutoSize = true;
            rbPP1.Font = new Font("Segoe UI", 9F);
            rbPP1.ForeColor = Color.FromArgb(255, 255, 255);
            rbPP1.Location = new Point(18, 108);
            rbPP1.Name = "rbPP1";
            rbPP1.Size = new Size(183, 19);
            rbPP1.TabIndex = 1;
            rbPP1.TabStop = true;
            rbPP1.Text = "1: Desconectado (4700 Ω)";
            rbPP1.UseVisualStyleBackColor = true;
            rbPP1.CheckedChanged += rbPP_CheckedChanged;
            // 
            // rbPP0
            // 
            rbPP0.AutoSize = true;
            rbPP0.Checked = true;
            rbPP0.Font = new Font("Segoe UI", 9F);
            rbPP0.ForeColor = Color.FromArgb(255, 255, 255);
            rbPP0.Location = new Point(18, 68);
            rbPP0.Name = "rbPP0";
            rbPP0.Size = new Size(177, 19);
            rbPP0.TabIndex = 0;
            rbPP0.TabStop = true;
            rbPP0.Text = "0: Circuito Aberto (Nenhum)";
            rbPP0.UseVisualStyleBackColor = true;
            rbPP0.CheckedChanged += rbPP_CheckedChanged;
            // 
            // cardVeiculoCP
            // 
            cardVeiculoCP.Controls.Add(lblStatusVeiculo);
            cardVeiculoCP.Controls.Add(lblSubtituloCP);
            cardVeiculoCP.Controls.Add(lblTituloCP);
            cardVeiculoCP.Controls.Add(rbCPE);
            cardVeiculoCP.Controls.Add(rbCPD);
            cardVeiculoCP.Controls.Add(rbCPC);
            cardVeiculoCP.Controls.Add(rbCPB);
            cardVeiculoCP.Controls.Add(rbCPA);
            cardVeiculoCP.Location = new Point(282, 88);
            cardVeiculoCP.Name = "cardVeiculoCP";
            cardVeiculoCP.Size = new Size(256, 420);
            cardVeiculoCP.TabIndex = 2;
            // 
            // lblStatusVeiculo
            // 
            lblStatusVeiculo.Font = new Font("Segoe UI", 8.5F);
            lblStatusVeiculo.ForeColor = Color.FromArgb(76, 194, 255);
            lblStatusVeiculo.Location = new Point(16, 375);
            lblStatusVeiculo.Name = "lblStatusVeiculo";
            lblStatusVeiculo.Size = new Size(224, 30);
            lblStatusVeiculo.TabIndex = 7;
            lblStatusVeiculo.Text = "Ativo: Estado A (+12V)";
            // 
            // lblSubtituloCP
            // 
            lblSubtituloCP.AutoSize = true;
            lblSubtituloCP.Font = new Font("Segoe UI", 8F);
            lblSubtituloCP.ForeColor = Color.FromArgb(160, 160, 160);
            lblSubtituloCP.Location = new Point(16, 36);
            lblSubtituloCP.Name = "lblSubtituloCP";
            lblSubtituloCP.Size = new Size(149, 13);
            lblSubtituloCP.TabIndex = 6;
            lblSubtituloCP.Text = "Tensão PWM (IEC 61851-1)";
            // 
            // lblTituloCP
            // 
            lblTituloCP.AutoSize = true;
            lblTituloCP.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            lblTituloCP.ForeColor = Color.FromArgb(255, 255, 255);
            lblTituloCP.Location = new Point(14, 14);
            lblTituloCP.Name = "lblTituloCP";
            lblTituloCP.Size = new Size(189, 19);
            lblTituloCP.TabIndex = 5;
            lblTituloCP.Text = "🚗 Emulador de Veículo (CP)";
            // 
            // rbCPE
            // 
            rbCPE.AutoSize = true;
            rbCPE.Font = new Font("Segoe UI", 9F);
            rbCPE.ForeColor = Color.FromArgb(239, 68, 68);
            rbCPE.Location = new Point(18, 228);
            rbCPE.Name = "rbCPE";
            rbCPE.Size = new Size(198, 19);
            rbCPE.TabIndex = 4;
            rbCPE.TabStop = true;
            rbCPE.Text = "Estado E (0V) - Falha / Curto";
            rbCPE.UseVisualStyleBackColor = true;
            rbCPE.CheckedChanged += rbCP_CheckedChanged;
            // 
            // rbCPD
            // 
            rbCPD.AutoSize = true;
            rbCPD.Font = new Font("Segoe UI", 9F);
            rbCPD.ForeColor = Color.FromArgb(255, 255, 255);
            rbCPD.Location = new Point(18, 188);
            rbCPD.Name = "rbCPD";
            rbCPD.Size = new Size(207, 19);
            rbCPD.TabIndex = 3;
            rbCPD.TabStop = true;
            rbCPD.Text = "Estado D (+3V) - C/ Ventilação";
            rbCPD.UseVisualStyleBackColor = true;
            rbCPD.CheckedChanged += rbCP_CheckedChanged;
            // 
            // rbCPC
            // 
            rbCPC.AutoSize = true;
            rbCPC.Font = new Font("Segoe UI", 9F);
            rbCPC.ForeColor = Color.FromArgb(255, 255, 255);
            rbCPC.Location = new Point(18, 148);
            rbCPC.Name = "rbCPC";
            rbCPC.Size = new Size(198, 19);
            rbCPC.TabIndex = 2;
            rbCPC.TabStop = true;
            rbCPC.Text = "Estado C (+6V) - Carregando";
            rbCPC.UseVisualStyleBackColor = true;
            rbCPC.CheckedChanged += rbCP_CheckedChanged;
            // 
            // rbCPB
            // 
            rbCPB.AutoSize = true;
            rbCPB.Font = new Font("Segoe UI", 9F);
            rbCPB.ForeColor = Color.FromArgb(255, 255, 255);
            rbCPB.Location = new Point(18, 108);
            rbCPB.Name = "rbCPB";
            rbCPB.Size = new Size(195, 19);
            rbCPB.TabIndex = 1;
            rbCPB.TabStop = true;
            rbCPB.Text = "Estado B (+9V) - Conectado";
            rbCPB.UseVisualStyleBackColor = true;
            rbCPB.CheckedChanged += rbCP_CheckedChanged;
            // 
            // rbCPA
            // 
            rbCPA.AutoSize = true;
            rbCPA.Checked = true;
            rbCPA.Font = new Font("Segoe UI", 9F);
            rbCPA.ForeColor = Color.FromArgb(255, 255, 255);
            rbCPA.Location = new Point(18, 68);
            rbCPA.Name = "rbCPA";
            rbCPA.Size = new Size(207, 19);
            rbCPA.TabIndex = 0;
            rbCPA.TabStop = true;
            rbCPA.Text = "Estado A (+12V) - Desconectado";
            rbCPA.UseVisualStyleBackColor = true;
            rbCPA.CheckedChanged += rbCP_CheckedChanged;
            // 
            // cardTerminal
            // 
            cardTerminal.Controls.Add(btnLimpar);
            cardTerminal.Controls.Add(btnEnviar);
            cardTerminal.Controls.Add(txtComando);
            cardTerminal.Controls.Add(txtTerminal);
            cardTerminal.Controls.Add(lblTituloTerminal);
            cardTerminal.Location = new Point(550, 88);
            cardTerminal.Name = "cardTerminal";
            cardTerminal.Size = new Size(276, 420);
            cardTerminal.TabIndex = 3;
            // 
            // btnLimpar
            // 
            btnLimpar.CornerRadius = 4;
            btnLimpar.Font = new Font("Segoe UI", 8.5F);
            btnLimpar.Location = new Point(207, 375);
            btnLimpar.Name = "btnLimpar";
            btnLimpar.Size = new Size(55, 28);
            btnLimpar.TabIndex = 4;
            btnLimpar.Text = "Limpar";
            btnLimpar.Click += btnLimpar_Click;
            // 
            // btnEnviar
            // 
            btnEnviar.IsAccent = true;
            btnEnviar.CornerRadius = 4;
            btnEnviar.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnEnviar.Location = new Point(148, 375);
            btnEnviar.Name = "btnEnviar";
            btnEnviar.Size = new Size(54, 28);
            btnEnviar.TabIndex = 3;
            btnEnviar.Text = "Enviar";
            btnEnviar.Click += btnEnviar_Click;
            // 
            // txtComando
            // 
            txtComando.BackColor = Color.FromArgb(30, 30, 30);
            txtComando.BorderStyle = BorderStyle.FixedSingle;
            txtComando.Font = new Font("Segoe UI", 9F);
            txtComando.ForeColor = Color.White;
            txtComando.Location = new Point(14, 378);
            txtComando.Name = "txtComando";
            txtComando.PlaceholderText = "Comando...";
            txtComando.Size = new Size(128, 23);
            txtComando.TabIndex = 2;
            txtComando.KeyDown += txtComando_KeyDown;
            // 
            // txtTerminal
            // 
            txtTerminal.BackColor = Color.FromArgb(16, 16, 16);
            txtTerminal.BorderStyle = BorderStyle.FixedSingle;
            txtTerminal.Font = new Font("Consolas", 8.5F);
            txtTerminal.ForeColor = Color.FromArgb(200, 200, 200);
            txtTerminal.Location = new Point(14, 40);
            txtTerminal.Multiline = true;
            txtTerminal.Name = "txtTerminal";
            txtTerminal.ReadOnly = true;
            txtTerminal.ScrollBars = ScrollBars.Vertical;
            txtTerminal.Size = new Size(248, 325);
            txtTerminal.TabIndex = 1;
            // 
            // lblTituloTerminal
            // 
            lblTituloTerminal.AutoSize = true;
            lblTituloTerminal.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            lblTituloTerminal.ForeColor = Color.FromArgb(255, 255, 255);
            lblTituloTerminal.Location = new Point(14, 14);
            lblTituloTerminal.Name = "lblTituloTerminal";
            lblTituloTerminal.Size = new Size(128, 19);
            lblTituloTerminal.TabIndex = 0;
            lblTituloTerminal.Text = "📟 Monitor Serial";
            // 
            // panelFooter
            // 
            panelFooter.Controls.Add(lblFooter);
            panelFooter.Dock = DockStyle.Bottom;
            panelFooter.Location = new Point(0, 520);
            panelFooter.Name = "panelFooter";
            panelFooter.Size = new Size(840, 35);
            panelFooter.TabIndex = 4;
            // 
            // lblFooter
            // 
            lblFooter.AutoSize = true;
            lblFooter.Font = new Font("Segoe UI", 8.5F);
            lblFooter.ForeColor = Color.FromArgb(160, 160, 160);
            lblFooter.Location = new Point(14, 10);
            lblFooter.Name = "lblFooter";
            lblFooter.Size = new Size(335, 15);
            lblFooter.TabIndex = 0;
            lblFooter.Text = "Emulador de Bancada Modo 3 (IEC 61851-1 / IEC 62196-2) • .NET 10";
            // 
            // FormBancada
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(32, 32, 32);
            ClientSize = new Size(840, 555);
            Controls.Add(panelFooter);
            Controls.Add(cardTerminal);
            Controls.Add(cardVeiculoCP);
            Controls.Add(cardCaboPP);
            Controls.Add(cardConexao);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "FormBancada";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Bancada de Testes EVSE (IEC 61851-1 / IEC 62196-2)";
            cardConexao.ResumeLayout(false);
            cardConexao.PerformLayout();
            cardCaboPP.ResumeLayout(false);
            cardCaboPP.PerformLayout();
            cardVeiculoCP.ResumeLayout(false);
            cardVeiculoCP.PerformLayout();
            cardTerminal.ResumeLayout(false);
            cardTerminal.PerformLayout();
            panelFooter.ResumeLayout(false);
            panelFooter.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private DevSandbox.UI.Common.FluentCard cardConexao;
        private Label lblPortas;
        private ComboBox cboPortas;
        private DevSandbox.UI.Common.FluentButton btnAtualizarPortas;
        private Label lblBaudRate;
        private ComboBox cboBaudRate;
        private DevSandbox.UI.Common.FluentButton btnConectar;
        private Panel pnlStatusLed;
        private Label lblStatus;
        private DevSandbox.UI.Common.FluentCard cardCaboPP;
        private Label lblTituloPP;
        private Label lblSubtituloPP;
        private RadioButton rbPP0;
        private RadioButton rbPP1;
        private RadioButton rbPP2;
        private RadioButton rbPP3;
        private RadioButton rbPP4;
        private RadioButton rbPP5;
        private RadioButton rbPP6;
        private Label lblStatusCabo;
        private DevSandbox.UI.Common.FluentCard cardVeiculoCP;
        private Label lblTituloCP;
        private Label lblSubtituloCP;
        private RadioButton rbCPA;
        private RadioButton rbCPB;
        private RadioButton rbCPC;
        private RadioButton rbCPD;
        private RadioButton rbCPE;
        private Label lblStatusVeiculo;
        private DevSandbox.UI.Common.FluentCard cardTerminal;
        private Label lblTituloTerminal;
        private TextBox txtTerminal;
        private TextBox txtComando;
        private DevSandbox.UI.Common.FluentButton btnEnviar;
        private DevSandbox.UI.Common.FluentButton btnLimpar;
        private Panel panelFooter;
        private Label lblFooter;
    }
}

