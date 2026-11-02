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
            lblNotaPP = new Label();
            lblFirmwarePP = new Label();
            btnStatusPP = new DevSandbox.UI.Common.FluentButton();
            cardVeiculoCP = new DevSandbox.UI.Common.FluentCard();
            lblStatusVeiculo = new Label();
            lblSubtituloCP = new Label();
            lblTituloCP = new Label();
            rbCPE = new RadioButton();
            rbCPD = new RadioButton();
            rbCPC = new RadioButton();
            rbCPB = new RadioButton();
            rbCPA = new RadioButton();
            pnlVariacaoCP = new Panel();
            lblTituloVariacao = new Label();
            btnVarSup = new DevSandbox.UI.Common.FluentButton();
            btnVarNom = new DevSandbox.UI.Common.FluentButton();
            btnVarInf = new DevSandbox.UI.Common.FluentButton();
            pnlRelesCP = new DevSandbox.UI.Common.FluentCard();
            lblTituloReles = new Label();
            ledD30_PE = new DevSandbox.UI.Common.LedIndicator();
            ledD31_Falha = new DevSandbox.UI.Common.LedIndicator();
            ledD29_BHab = new DevSandbox.UI.Common.LedIndicator();
            ledD28_BSel = new DevSandbox.UI.Common.LedIndicator();
            ledD27_CDHab = new DevSandbox.UI.Common.LedIndicator();
            ledD26_CDSel = new DevSandbox.UI.Common.LedIndicator();
            ledD25_CHab = new DevSandbox.UI.Common.LedIndicator();
            ledD24_CSel = new DevSandbox.UI.Common.LedIndicator();
            ledD23_DHab = new DevSandbox.UI.Common.LedIndicator();
            ledD22_DSel = new DevSandbox.UI.Common.LedIndicator();
            lblTensaoEsperada = new Label();
            lblFirmwareCP = new Label();
            btnStatusCP = new DevSandbox.UI.Common.FluentButton();
            cardTerminal = new DevSandbox.UI.Common.FluentCard();
            lblTituloTerminal = new Label();
            btnDebugStack = new DevSandbox.UI.Common.FluentButton();
            btnHelp = new DevSandbox.UI.Common.FluentButton();
            btnLimpar = new DevSandbox.UI.Common.FluentButton();
            txtTerminal = new TextBox();
            txtComando = new TextBox();
            btnEnviar = new DevSandbox.UI.Common.FluentButton();
            chkAutoScroll = new CheckBox();
            panelFooter = new Panel();
            lblFooter = new Label();
            cardConexao.SuspendLayout();
            cardCaboPP.SuspendLayout();
            cardVeiculoCP.SuspendLayout();
            pnlVariacaoCP.SuspendLayout();
            pnlRelesCP.SuspendLayout();
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
            cardConexao.Location = new Point(14, 10);
            cardConexao.Name = "cardConexao";
            cardConexao.Size = new Size(852, 64);
            cardConexao.TabIndex = 0;
            //
            // lblStatus
            //
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 9F);
            lblStatus.ForeColor = Color.FromArgb(160, 160, 160);
            lblStatus.Location = new Point(485, 32);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(82, 15);
            lblStatus.TabIndex = 7;
            lblStatus.Text = "Desconectado";
            //
            // pnlStatusLed
            //
            pnlStatusLed.BackColor = Color.FromArgb(239, 68, 68);
            pnlStatusLed.Location = new Point(465, 34);
            pnlStatusLed.Name = "pnlStatusLed";
            pnlStatusLed.Size = new Size(12, 12);
            pnlStatusLed.TabIndex = 6;
            //
            // btnConectar
            //
            btnConectar.IsAccent = true;
            btnConectar.CornerRadius = 6;
            btnConectar.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnConectar.Location = new Point(340, 22);
            btnConectar.Name = "btnConectar";
            btnConectar.Size = new Size(110, 32);
            btnConectar.TabIndex = 4;
            btnConectar.Text = "Conectar";
            btnConectar.Click += btnConectar_Click;
            //
            // cboBaudRate
            //
            cboBaudRate.DropDownStyle = ComboBoxStyle.DropDownList;
            cboBaudRate.Font = new Font("Segoe UI", 9F);
            cboBaudRate.FormattingEnabled = true;
            cboBaudRate.Location = new Point(220, 27);
            cboBaudRate.Name = "cboBaudRate";
            cboBaudRate.Size = new Size(100, 23);
            cboBaudRate.TabIndex = 3;
            //
            // lblBaudRate
            //
            lblBaudRate.AutoSize = true;
            lblBaudRate.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblBaudRate.ForeColor = Color.FromArgb(160, 160, 160);
            lblBaudRate.Location = new Point(220, 9);
            lblBaudRate.Name = "lblBaudRate";
            lblBaudRate.Size = new Size(67, 15);
            lblBaudRate.TabIndex = 5;
            lblBaudRate.Text = "BAUD RATE";
            //
            // btnAtualizarPortas
            //
            btnAtualizarPortas.CornerRadius = 4;
            btnAtualizarPortas.Font = new Font("Segoe UI", 9F);
            btnAtualizarPortas.Location = new Point(170, 25);
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
            cboPortas.Location = new Point(16, 27);
            cboPortas.Name = "cboPortas";
            cboPortas.Size = new Size(148, 23);
            cboPortas.TabIndex = 1;
            //
            // lblPortas
            //
            lblPortas.AutoSize = true;
            lblPortas.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblPortas.ForeColor = Color.FromArgb(160, 160, 160);
            lblPortas.Location = new Point(16, 9);
            lblPortas.Name = "lblPortas";
            lblPortas.Size = new Size(77, 15);
            lblPortas.TabIndex = 0;
            lblPortas.Text = "&PORTA SERIAL";
            //
            // cardCaboPP
            //
            cardCaboPP.Controls.Add(btnStatusPP);
            cardCaboPP.Controls.Add(lblFirmwarePP);
            cardCaboPP.Controls.Add(lblNotaPP);
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
            cardCaboPP.Location = new Point(14, 82);
            cardCaboPP.Name = "cardCaboPP";
            cardCaboPP.Size = new Size(268, 485);
            cardCaboPP.TabIndex = 1;
            //
            // lblTituloPP
            //
            lblTituloPP.AutoSize = true;
            lblTituloPP.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            lblTituloPP.ForeColor = Color.FromArgb(255, 255, 255);
            lblTituloPP.Location = new Point(14, 12);
            lblTituloPP.Name = "lblTituloPP";
            lblTituloPP.Size = new Size(177, 19);
            lblTituloPP.TabIndex = 20;
            lblTituloPP.Text = "🔌 Emulador de Cabo (PP)";
            //
            // lblSubtituloPP
            //
            lblSubtituloPP.AutoSize = true;
            lblSubtituloPP.Font = new Font("Segoe UI", 8F);
            lblSubtituloPP.ForeColor = Color.FromArgb(160, 160, 160);
            lblSubtituloPP.Location = new Point(16, 34);
            lblSubtituloPP.Name = "lblSubtituloPP";
            lblSubtituloPP.Size = new Size(166, 13);
            lblSubtituloPP.TabIndex = 21;
            lblSubtituloPP.Text = "IEC 62196-2 (Resistores PP-PE)";
            //
            // rbPP0
            //
            rbPP0.AutoSize = true;
            rbPP0.Checked = true;
            rbPP0.Font = new Font("Segoe UI", 9F);
            rbPP0.ForeColor = Color.FromArgb(255, 255, 255);
            rbPP0.Location = new Point(18, 58);
            rbPP0.Name = "rbPP0";
            rbPP0.Size = new Size(177, 19);
            rbPP0.TabIndex = 0;
            rbPP0.TabStop = true;
            rbPP0.Text = "0: Circuito Aberto (∞ Ω)";
            rbPP0.UseVisualStyleBackColor = true;
            //
            // rbPP1
            //
            rbPP1.AutoSize = true;
            rbPP1.Font = new Font("Segoe UI", 9F);
            rbPP1.ForeColor = Color.FromArgb(255, 255, 255);
            rbPP1.Location = new Point(18, 86);
            rbPP1.Name = "rbPP1";
            rbPP1.Size = new Size(183, 19);
            rbPP1.TabIndex = 1;
            rbPP1.Text = "1: Desconectado (4700 Ω) *";
            rbPP1.UseVisualStyleBackColor = true;
            //
            // rbPP2
            //
            rbPP2.AutoSize = true;
            rbPP2.Font = new Font("Segoe UI", 9F);
            rbPP2.ForeColor = Color.FromArgb(255, 255, 255);
            rbPP2.Location = new Point(18, 114);
            rbPP2.Name = "rbPP2";
            rbPP2.Size = new Size(155, 19);
            rbPP2.TabIndex = 2;
            rbPP2.Text = "2: Cabo 13 A (1500 Ω)";
            rbPP2.UseVisualStyleBackColor = true;
            //
            // rbPP3
            //
            rbPP3.AutoSize = true;
            rbPP3.Font = new Font("Segoe UI", 9F);
            rbPP3.ForeColor = Color.FromArgb(255, 255, 255);
            rbPP3.Location = new Point(18, 142);
            rbPP3.Name = "rbPP3";
            rbPP3.Size = new Size(149, 19);
            rbPP3.TabIndex = 3;
            rbPP3.Text = "3: Cabo 20 A (680 Ω)";
            rbPP3.UseVisualStyleBackColor = true;
            //
            // rbPP4
            //
            rbPP4.AutoSize = true;
            rbPP4.Font = new Font("Segoe UI", 9F);
            rbPP4.ForeColor = Color.FromArgb(255, 255, 255);
            rbPP4.Location = new Point(18, 170);
            rbPP4.Name = "rbPP4";
            rbPP4.Size = new Size(149, 19);
            rbPP4.TabIndex = 4;
            rbPP4.Text = "4: Cabo 32 A (220 Ω)";
            rbPP4.UseVisualStyleBackColor = true;
            //
            // rbPP5
            //
            rbPP5.AutoSize = true;
            rbPP5.Font = new Font("Segoe UI", 9F);
            rbPP5.ForeColor = Color.FromArgb(255, 255, 255);
            rbPP5.Location = new Point(18, 198);
            rbPP5.Name = "rbPP5";
            rbPP5.Size = new Size(149, 19);
            rbPP5.TabIndex = 5;
            rbPP5.Text = "5: Cabo 63 A (100 Ω)";
            rbPP5.UseVisualStyleBackColor = true;
            //
            // rbPP6
            //
            rbPP6.AutoSize = true;
            rbPP6.Font = new Font("Segoe UI", 9F);
            rbPP6.ForeColor = Color.FromArgb(239, 68, 68);
            rbPP6.Location = new Point(18, 226);
            rbPP6.Name = "rbPP6";
            rbPP6.Size = new Size(155, 19);
            rbPP6.TabIndex = 6;
            rbPP6.Text = "6: Falha / Curto (47 Ω)";
            rbPP6.UseVisualStyleBackColor = true;
            //
            // lblNotaPP
            //
            lblNotaPP.Font = new Font("Segoe UI", 7.5F, FontStyle.Italic);
            lblNotaPP.ForeColor = Color.FromArgb(160, 160, 160);
            lblNotaPP.Location = new Point(18, 256);
            lblNotaPP.Name = "lblNotaPP";
            lblNotaPP.Size = new Size(234, 26);
            lblNotaPP.TabIndex = 22;
            lblNotaPP.Text = "* Valor da bancada, fora da tabela IEC 62196-2";
            //
            // lblStatusCabo
            //
            lblStatusCabo.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblStatusCabo.ForeColor = Color.FromArgb(76, 194, 255);
            lblStatusCabo.Location = new Point(16, 292);
            lblStatusCabo.Name = "lblStatusCabo";
            lblStatusCabo.Size = new Size(238, 34);
            lblStatusCabo.TabIndex = 23;
            lblStatusCabo.Text = "Ativo: Circuito Aberto (∞ Ω)";
            //
            // lblFirmwarePP
            //
            lblFirmwarePP.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            lblFirmwarePP.ForeColor = Color.FromArgb(160, 160, 160);
            lblFirmwarePP.Location = new Point(16, 330);
            lblFirmwarePP.Name = "lblFirmwarePP";
            lblFirmwarePP.Size = new Size(238, 18);
            lblFirmwarePP.TabIndex = 24;
            lblFirmwarePP.Text = "Firmware: —";
            //
            // btnStatusPP
            //
            btnStatusPP.CornerRadius = 4;
            btnStatusPP.Font = new Font("Segoe UI", 8.5F);
            btnStatusPP.Location = new Point(16, 358);
            btnStatusPP.Name = "btnStatusPP";
            btnStatusPP.Size = new Size(110, 28);
            btnStatusPP.TabIndex = 7;
            btnStatusPP.Text = "🔄 Status PP";
            btnStatusPP.Click += btnStatusPP_Click;
            //
            // cardVeiculoCP
            //
            cardVeiculoCP.Controls.Add(btnStatusCP);
            cardVeiculoCP.Controls.Add(lblFirmwareCP);
            cardVeiculoCP.Controls.Add(lblTensaoEsperada);
            cardVeiculoCP.Controls.Add(lblStatusVeiculo);
            cardVeiculoCP.Controls.Add(pnlRelesCP);
            cardVeiculoCP.Controls.Add(pnlVariacaoCP);
            cardVeiculoCP.Controls.Add(lblSubtituloCP);
            cardVeiculoCP.Controls.Add(lblTituloCP);
            cardVeiculoCP.Controls.Add(rbCPE);
            cardVeiculoCP.Controls.Add(rbCPD);
            cardVeiculoCP.Controls.Add(rbCPC);
            cardVeiculoCP.Controls.Add(rbCPB);
            cardVeiculoCP.Controls.Add(rbCPA);
            cardVeiculoCP.Location = new Point(294, 82);
            cardVeiculoCP.Name = "cardVeiculoCP";
            cardVeiculoCP.Size = new Size(276, 485);
            cardVeiculoCP.TabIndex = 2;
            //
            // lblTituloCP
            //
            lblTituloCP.AutoSize = true;
            lblTituloCP.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            lblTituloCP.ForeColor = Color.FromArgb(255, 255, 255);
            lblTituloCP.Location = new Point(14, 12);
            lblTituloCP.Name = "lblTituloCP";
            lblTituloCP.Size = new Size(189, 19);
            lblTituloCP.TabIndex = 20;
            lblTituloCP.Text = "🚗 Emulador de Veículo (CP)";
            //
            // lblSubtituloCP
            //
            lblSubtituloCP.AutoSize = true;
            lblSubtituloCP.Font = new Font("Segoe UI", 8F);
            lblSubtituloCP.ForeColor = Color.FromArgb(160, 160, 160);
            lblSubtituloCP.Location = new Point(16, 34);
            lblSubtituloCP.Name = "lblSubtituloCP";
            lblSubtituloCP.Size = new Size(149, 13);
            lblSubtituloCP.TabIndex = 21;
            lblSubtituloCP.Text = "IEC 61851-1 (Sinalização PWM)";
            //
            // rbCPA
            //
            rbCPA.AutoSize = true;
            rbCPA.Checked = true;
            rbCPA.Font = new Font("Segoe UI", 9F);
            rbCPA.ForeColor = Color.FromArgb(255, 255, 255);
            rbCPA.Location = new Point(18, 54);
            rbCPA.Name = "rbCPA";
            rbCPA.Size = new Size(207, 19);
            rbCPA.TabIndex = 0;
            rbCPA.TabStop = true;
            rbCPA.Text = "A: Desconectado (+12 V)";
            rbCPA.UseVisualStyleBackColor = true;
            //
            // rbCPB
            //
            rbCPB.AutoSize = true;
            rbCPB.Font = new Font("Segoe UI", 9F);
            rbCPB.ForeColor = Color.FromArgb(255, 255, 255);
            rbCPB.Location = new Point(18, 82);
            rbCPB.Name = "rbCPB";
            rbCPB.Size = new Size(195, 19);
            rbCPB.TabIndex = 1;
            rbCPB.Text = "B: Conectado / Standby (+9 V)";
            rbCPB.UseVisualStyleBackColor = true;
            //
            // rbCPC
            //
            rbCPC.AutoSize = true;
            rbCPC.Font = new Font("Segoe UI", 9F);
            rbCPC.ForeColor = Color.FromArgb(255, 255, 255);
            rbCPC.Location = new Point(18, 110);
            rbCPC.Name = "rbCPC";
            rbCPC.Size = new Size(198, 19);
            rbCPC.TabIndex = 2;
            rbCPC.Text = "C: Carga Normal (+6 V)";
            rbCPC.UseVisualStyleBackColor = true;
            //
            // rbCPD
            //
            rbCPD.AutoSize = true;
            rbCPD.Font = new Font("Segoe UI", 9F);
            rbCPD.ForeColor = Color.FromArgb(255, 255, 255);
            rbCPD.Location = new Point(18, 138);
            rbCPD.Name = "rbCPD";
            rbCPD.Size = new Size(207, 19);
            rbCPD.TabIndex = 3;
            rbCPD.Text = "D: Carga c/ Ventilação (+3 V)";
            rbCPD.UseVisualStyleBackColor = true;
            //
            // rbCPE
            //
            rbCPE.AutoSize = true;
            rbCPE.Font = new Font("Segoe UI", 9F);
            rbCPE.ForeColor = Color.FromArgb(239, 68, 68);
            rbCPE.Location = new Point(18, 166);
            rbCPE.Name = "rbCPE";
            rbCPE.Size = new Size(198, 19);
            rbCPE.TabIndex = 4;
            rbCPE.Text = "E: Falha / Curto (+1 V)";
            rbCPE.UseVisualStyleBackColor = true;
            //
            // pnlVariacaoCP
            //
            pnlVariacaoCP.BackColor = Color.Transparent;
            pnlVariacaoCP.Controls.Add(lblTituloVariacao);
            pnlVariacaoCP.Controls.Add(btnVarInf);
            pnlVariacaoCP.Controls.Add(btnVarNom);
            pnlVariacaoCP.Controls.Add(btnVarSup);
            pnlVariacaoCP.Location = new Point(12, 189);
            pnlVariacaoCP.Name = "pnlVariacaoCP";
            pnlVariacaoCP.Size = new Size(252, 44);
            pnlVariacaoCP.TabIndex = 5;
            //
            // lblTituloVariacao
            //
            lblTituloVariacao.AutoSize = true;
            lblTituloVariacao.Font = new Font("Segoe UI", 7F, FontStyle.Bold);
            lblTituloVariacao.ForeColor = Color.FromArgb(160, 160, 160);
            lblTituloVariacao.Location = new Point(4, 0);
            lblTituloVariacao.Name = "lblTituloVariacao";
            lblTituloVariacao.TabIndex = 3;
            lblTituloVariacao.Text = "TOLERÂNCIA (ESTADOS B, C, D):";
            //
            // btnVarSup
            //
            btnVarSup.CornerRadius = 4;
            btnVarSup.Font = new Font("Segoe UI", 8F);
            btnVarSup.Location = new Point(4, 15);
            btnVarSup.Name = "btnVarSup";
            btnVarSup.Size = new Size(78, 28);
            btnVarSup.TabIndex = 0;
            btnVarSup.Text = "+ Sup";
            btnVarSup.Click += btnVariacao_Click;
            //
            // btnVarNom
            //
            btnVarNom.CornerRadius = 4;
            btnVarNom.Font = new Font("Segoe UI", 8F);
            btnVarNom.Location = new Point(87, 15);
            btnVarNom.Name = "btnVarNom";
            btnVarNom.Size = new Size(78, 28);
            btnVarNom.TabIndex = 1;
            btnVarNom.Text = "• Nom •";
            btnVarNom.Click += btnVariacao_Click;
            //
            // btnVarInf
            //
            btnVarInf.CornerRadius = 4;
            btnVarInf.Font = new Font("Segoe UI", 8F);
            btnVarInf.Location = new Point(170, 15);
            btnVarInf.Name = "btnVarInf";
            btnVarInf.Size = new Size(78, 28);
            btnVarInf.TabIndex = 2;
            btnVarInf.Text = "− Inf";
            btnVarInf.Click += btnVariacao_Click;
            //
            // pnlRelesCP
            //
            pnlRelesCP.Controls.Add(lblTituloReles);
            pnlRelesCP.Controls.Add(ledD30_PE);
            pnlRelesCP.Controls.Add(ledD31_Falha);
            pnlRelesCP.Controls.Add(ledD29_BHab);
            pnlRelesCP.Controls.Add(ledD28_BSel);
            pnlRelesCP.Controls.Add(ledD27_CDHab);
            pnlRelesCP.Controls.Add(ledD26_CDSel);
            pnlRelesCP.Controls.Add(ledD25_CHab);
            pnlRelesCP.Controls.Add(ledD24_CSel);
            pnlRelesCP.Controls.Add(ledD23_DHab);
            pnlRelesCP.Controls.Add(ledD22_DSel);
            pnlRelesCP.Location = new Point(12, 235);
            pnlRelesCP.Name = "pnlRelesCP";
            pnlRelesCP.Size = new Size(252, 102);
            pnlRelesCP.TabIndex = 6;
            //
            // lblTituloReles
            //
            lblTituloReles.AutoSize = true;
            lblTituloReles.BackColor = Color.Transparent;
            lblTituloReles.Font = new Font("Segoe UI", 7F, FontStyle.Bold);
            lblTituloReles.ForeColor = Color.FromArgb(160, 160, 160);
            lblTituloReles.Location = new Point(6, 2);
            lblTituloReles.Name = "lblTituloReles";
            lblTituloReles.TabIndex = 0;
            lblTituloReles.Text = "RELÉS CP ATIVOS (HARDWARE):";
            //
            // LEDs dos relés (2 colunas x 5 linhas de 16 px, passo de 17 px)
            //
            ledD30_PE.Location = new Point(8, 16);
            ledD30_PE.Name = "ledD30_PE";
            ledD30_PE.Size = new Size(116, 16);
            ledD30_PE.Text = "D30: PE";
            ledD31_Falha.Location = new Point(130, 16);
            ledD31_Falha.Name = "ledD31_Falha";
            ledD31_Falha.Size = new Size(116, 16);
            ledD31_Falha.Text = "D31: falha";
            ledD29_BHab.Location = new Point(8, 33);
            ledD29_BHab.Name = "ledD29_BHab";
            ledD29_BHab.Size = new Size(116, 16);
            ledD29_BHab.Text = "D29: B-hab";
            ledD28_BSel.Location = new Point(130, 33);
            ledD28_BSel.Name = "ledD28_BSel";
            ledD28_BSel.Size = new Size(116, 16);
            ledD28_BSel.Text = "D28: B-sel";
            ledD27_CDHab.Location = new Point(8, 50);
            ledD27_CDHab.Name = "ledD27_CDHab";
            ledD27_CDHab.Size = new Size(116, 16);
            ledD27_CDHab.Text = "D27: CD-hab";
            ledD26_CDSel.Location = new Point(130, 50);
            ledD26_CDSel.Name = "ledD26_CDSel";
            ledD26_CDSel.Size = new Size(116, 16);
            ledD26_CDSel.Text = "D26: CD-sel";
            ledD25_CHab.Location = new Point(8, 67);
            ledD25_CHab.Name = "ledD25_CHab";
            ledD25_CHab.Size = new Size(116, 16);
            ledD25_CHab.Text = "D25: C-hab";
            ledD24_CSel.Location = new Point(130, 67);
            ledD24_CSel.Name = "ledD24_CSel";
            ledD24_CSel.Size = new Size(116, 16);
            ledD24_CSel.Text = "D24: C-sel";
            ledD23_DHab.Location = new Point(8, 84);
            ledD23_DHab.Name = "ledD23_DHab";
            ledD23_DHab.Size = new Size(116, 16);
            ledD23_DHab.Text = "D23: D-hab";
            ledD22_DSel.Location = new Point(130, 84);
            ledD22_DSel.Name = "ledD22_DSel";
            ledD22_DSel.Size = new Size(116, 16);
            ledD22_DSel.Text = "D22: D-sel";
            //
            // lblStatusVeiculo
            //
            lblStatusVeiculo.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblStatusVeiculo.ForeColor = Color.FromArgb(76, 194, 255);
            lblStatusVeiculo.Location = new Point(16, 343);
            lblStatusVeiculo.Name = "lblStatusVeiculo";
            lblStatusVeiculo.Size = new Size(246, 34);
            lblStatusVeiculo.TabIndex = 22;
            lblStatusVeiculo.Text = "Ativo: AX";
            //
            // lblTensaoEsperada
            //
            lblTensaoEsperada.Font = new Font("Segoe UI", 8F);
            lblTensaoEsperada.ForeColor = Color.FromArgb(160, 160, 160);
            lblTensaoEsperada.Location = new Point(16, 378);
            lblTensaoEsperada.Name = "lblTensaoEsperada";
            lblTensaoEsperada.Size = new Size(246, 16);
            lblTensaoEsperada.TabIndex = 23;
            lblTensaoEsperada.Text = "Tensão nominal: +12 V";
            //
            // lblFirmwareCP
            //
            lblFirmwareCP.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            lblFirmwareCP.ForeColor = Color.FromArgb(160, 160, 160);
            lblFirmwareCP.Location = new Point(16, 396);
            lblFirmwareCP.Name = "lblFirmwareCP";
            lblFirmwareCP.Size = new Size(246, 34);
            lblFirmwareCP.TabIndex = 24;
            lblFirmwareCP.Text = "Firmware: —";
            //
            // btnStatusCP
            //
            btnStatusCP.CornerRadius = 4;
            btnStatusCP.Font = new Font("Segoe UI", 8.5F);
            btnStatusCP.Location = new Point(16, 438);
            btnStatusCP.Name = "btnStatusCP";
            btnStatusCP.Size = new Size(110, 28);
            btnStatusCP.TabIndex = 7;
            btnStatusCP.Text = "🔄 Status CP";
            btnStatusCP.Click += btnStatusCP_Click;
            //
            // cardTerminal
            //
            cardTerminal.Controls.Add(chkAutoScroll);
            cardTerminal.Controls.Add(btnEnviar);
            cardTerminal.Controls.Add(txtComando);
            cardTerminal.Controls.Add(txtTerminal);
            cardTerminal.Controls.Add(btnLimpar);
            cardTerminal.Controls.Add(btnHelp);
            cardTerminal.Controls.Add(btnDebugStack);
            cardTerminal.Controls.Add(lblTituloTerminal);
            cardTerminal.Location = new Point(582, 82);
            cardTerminal.Name = "cardTerminal";
            cardTerminal.Size = new Size(284, 485);
            cardTerminal.TabIndex = 3;
            //
            // lblTituloTerminal
            //
            lblTituloTerminal.AutoSize = true;
            lblTituloTerminal.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            lblTituloTerminal.ForeColor = Color.FromArgb(255, 255, 255);
            lblTituloTerminal.Location = new Point(14, 12);
            lblTituloTerminal.Name = "lblTituloTerminal";
            lblTituloTerminal.Size = new Size(128, 19);
            lblTituloTerminal.TabIndex = 20;
            lblTituloTerminal.Text = "📟 Monitor && Diagnóstico";
            //
            // btnDebugStack
            //
            btnDebugStack.CornerRadius = 4;
            btnDebugStack.Font = new Font("Segoe UI", 8.5F);
            btnDebugStack.Location = new Point(14, 38);
            btnDebugStack.Name = "btnDebugStack";
            btnDebugStack.Size = new Size(88, 26);
            btnDebugStack.TabIndex = 0;
            btnDebugStack.Text = "⚡ Stack";
            btnDebugStack.Click += btnDebugStack_Click;
            //
            // btnHelp
            //
            btnHelp.CornerRadius = 4;
            btnHelp.Font = new Font("Segoe UI", 8.5F);
            btnHelp.Location = new Point(106, 38);
            btnHelp.Name = "btnHelp";
            btnHelp.Size = new Size(78, 26);
            btnHelp.TabIndex = 1;
            btnHelp.Text = "❓ Help";
            btnHelp.Click += btnHelp_Click;
            //
            // btnLimpar
            //
            btnLimpar.CornerRadius = 4;
            btnLimpar.Font = new Font("Segoe UI", 8.5F);
            btnLimpar.Location = new Point(200, 38);
            btnLimpar.Name = "btnLimpar";
            btnLimpar.Size = new Size(70, 26);
            btnLimpar.TabIndex = 2;
            btnLimpar.Text = "🗑 Limpar";
            btnLimpar.Click += btnLimpar_Click;
            //
            // txtTerminal
            //
            txtTerminal.BackColor = Color.FromArgb(16, 16, 16);
            txtTerminal.BorderStyle = BorderStyle.FixedSingle;
            txtTerminal.Font = new Font("Consolas", 8.5F);
            txtTerminal.ForeColor = Color.FromArgb(34, 197, 94);
            txtTerminal.Location = new Point(14, 68);
            txtTerminal.Multiline = true;
            txtTerminal.Name = "txtTerminal";
            txtTerminal.ReadOnly = true;
            txtTerminal.ScrollBars = ScrollBars.Vertical;
            txtTerminal.Size = new Size(256, 335);
            txtTerminal.TabIndex = 3;
            //
            // txtComando
            //
            txtComando.BackColor = Color.FromArgb(30, 30, 30);
            txtComando.BorderStyle = BorderStyle.FixedSingle;
            txtComando.Font = new Font("Segoe UI", 9F);
            txtComando.ForeColor = Color.White;
            txtComando.Location = new Point(14, 412);
            txtComando.Name = "txtComando";
            txtComando.PlaceholderText = "Comando serial...";
            txtComando.Size = new Size(188, 26);
            txtComando.TabIndex = 4;
            txtComando.KeyDown += txtComando_KeyDown;
            //
            // btnEnviar
            //
            btnEnviar.IsAccent = true;
            btnEnviar.CornerRadius = 4;
            btnEnviar.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnEnviar.Location = new Point(206, 411);
            btnEnviar.Name = "btnEnviar";
            btnEnviar.Size = new Size(64, 28);
            btnEnviar.TabIndex = 5;
            btnEnviar.Text = "Enviar";
            btnEnviar.Click += btnEnviar_Click;
            //
            // chkAutoScroll
            //
            chkAutoScroll.AutoSize = true;
            chkAutoScroll.Checked = true;
            chkAutoScroll.CheckState = CheckState.Checked;
            chkAutoScroll.Font = new Font("Segoe UI", 7.5F);
            chkAutoScroll.ForeColor = Color.FromArgb(160, 160, 160);
            chkAutoScroll.Location = new Point(16, 448);
            chkAutoScroll.Name = "chkAutoScroll";
            chkAutoScroll.TabIndex = 6;
            chkAutoScroll.Text = "Rolar automaticamente ao final";
            chkAutoScroll.UseVisualStyleBackColor = true;
            chkAutoScroll.CheckedChanged += chkAutoScroll_CheckedChanged;
            //
            // panelFooter
            //
            panelFooter.Controls.Add(lblFooter);
            panelFooter.Dock = DockStyle.Bottom;
            panelFooter.Location = new Point(0, 572);
            panelFooter.Name = "panelFooter";
            panelFooter.Size = new Size(880, 28);
            panelFooter.TabIndex = 4;
            //
            // lblFooter
            //
            lblFooter.AutoSize = true;
            lblFooter.Font = new Font("Segoe UI", 8.5F);
            lblFooter.ForeColor = Color.FromArgb(160, 160, 160);
            lblFooter.Location = new Point(14, 6);
            lblFooter.Name = "lblFooter";
            lblFooter.TabIndex = 0;
            lblFooter.Text = "Emulador de Bancada Modo 3 (IEC 61851-1 / IEC 62196-2) • .NET 10 • Firmware AVR EasyEDA Mega";
            //
            // FormBancada
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(32, 32, 32);
            ClientSize = new Size(880, 600);
            Controls.Add(panelFooter);
            Controls.Add(cardTerminal);
            Controls.Add(cardVeiculoCP);
            Controls.Add(cardCaboPP);
            Controls.Add(cardConexao);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "FormBancada";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Bancada de Testes EVSE (IEC 61851-1 / IEC 62196-2) — v2.0";
            cardConexao.ResumeLayout(false);
            cardConexao.PerformLayout();
            cardCaboPP.ResumeLayout(false);
            cardCaboPP.PerformLayout();
            pnlVariacaoCP.ResumeLayout(false);
            pnlVariacaoCP.PerformLayout();
            pnlRelesCP.ResumeLayout(false);
            pnlRelesCP.PerformLayout();
            cardVeiculoCP.ResumeLayout(false);
            cardVeiculoCP.PerformLayout();
            cardTerminal.ResumeLayout(false);
            cardTerminal.PerformLayout();
            panelFooter.ResumeLayout(false);
            panelFooter.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        // --- Barra de Conexão Superior ---
        private DevSandbox.UI.Common.FluentCard cardConexao;
        private Label lblPortas;
        private ComboBox cboPortas;
        private DevSandbox.UI.Common.FluentButton btnAtualizarPortas;
        private Label lblBaudRate;
        private ComboBox cboBaudRate;
        private DevSandbox.UI.Common.FluentButton btnConectar;
        private Panel pnlStatusLed;
        private Label lblStatus;

        // --- Coluna 1: Emulador de Cabo PP ---
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
        private Label lblNotaPP;
        private Label lblStatusCabo;
        private Label lblFirmwarePP;
        private DevSandbox.UI.Common.FluentButton btnStatusPP;

        // --- Coluna 2: Emulador de Veículo CP ---
        private DevSandbox.UI.Common.FluentCard cardVeiculoCP;
        private Label lblTituloCP;
        private Label lblSubtituloCP;
        private RadioButton rbCPA;
        private RadioButton rbCPB;
        private RadioButton rbCPC;
        private RadioButton rbCPD;
        private RadioButton rbCPE;
        private Panel pnlVariacaoCP;
        private Label lblTituloVariacao;
        private DevSandbox.UI.Common.FluentButton btnVarSup;
        private DevSandbox.UI.Common.FluentButton btnVarNom;
        private DevSandbox.UI.Common.FluentButton btnVarInf;

        // --- Painel dos 10 Relés do CP ---
        private DevSandbox.UI.Common.FluentCard pnlRelesCP;
        private Label lblTituloReles;
        private DevSandbox.UI.Common.LedIndicator ledD30_PE;
        private DevSandbox.UI.Common.LedIndicator ledD31_Falha;
        private DevSandbox.UI.Common.LedIndicator ledD29_BHab;
        private DevSandbox.UI.Common.LedIndicator ledD28_BSel;
        private DevSandbox.UI.Common.LedIndicator ledD27_CDHab;
        private DevSandbox.UI.Common.LedIndicator ledD26_CDSel;
        private DevSandbox.UI.Common.LedIndicator ledD25_CHab;
        private DevSandbox.UI.Common.LedIndicator ledD24_CSel;
        private DevSandbox.UI.Common.LedIndicator ledD23_DHab;
        private DevSandbox.UI.Common.LedIndicator ledD22_DSel;

        private Label lblStatusVeiculo;
        private Label lblTensaoEsperada;
        private Label lblFirmwareCP;
        private DevSandbox.UI.Common.FluentButton btnStatusCP;

        // --- Coluna 3: Monitor Serial e Diagnóstico ---
        private DevSandbox.UI.Common.FluentCard cardTerminal;
        private Label lblTituloTerminal;
        private DevSandbox.UI.Common.FluentButton btnDebugStack;
        private DevSandbox.UI.Common.FluentButton btnHelp;
        private DevSandbox.UI.Common.FluentButton btnLimpar;
        private TextBox txtTerminal;
        private TextBox txtComando;
        private DevSandbox.UI.Common.FluentButton btnEnviar;
        private CheckBox chkAutoScroll;

        // --- Rodapé ---
        private Panel panelFooter;
        private Label lblFooter;
    }
}
