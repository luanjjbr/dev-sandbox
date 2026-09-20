namespace DevSandbox.UI.Forms
{
    partial class MainForm
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
            lblTitulo = new Label();
            lblSubtitulo = new Label();
            panelHeader = new Panel();
            cardSupervisorio = new DevSandbox.UI.Common.FluentCard();
            lblDescSupervisorio = new Label();
            lblTituloSupervisorio = new Label();
            btnAbrirSupervisorio = new DevSandbox.UI.Common.FluentButton();
            cardPomodoro = new DevSandbox.UI.Common.FluentCard();
            lblDescPomodoro = new Label();
            lblTituloPomodoro = new Label();
            btnAbrirPomodoro = new DevSandbox.UI.Common.FluentButton();
            cardCalculadora = new DevSandbox.UI.Common.FluentCard();
            lblDescCalculadora = new Label();
            lblTituloCalculadora = new Label();
            btnAbrirCalculadora = new DevSandbox.UI.Common.FluentButton();
            cardBancada = new DevSandbox.UI.Common.FluentCard();
            lblDescBancada = new Label();
            lblTituloBancada = new Label();
            btnAbrirBancada = new DevSandbox.UI.Common.FluentButton();
            panelFooter = new Panel();
            lblFooter = new Label();
            panelHeader.SuspendLayout();
            cardSupervisorio.SuspendLayout();
            cardPomodoro.SuspendLayout();
            cardCalculadora.SuspendLayout();
            cardBancada.SuspendLayout();
            panelFooter.SuspendLayout();
            SuspendLayout();
            // 
            // panelHeader
            // 
            panelHeader.Controls.Add(lblTitulo);
            panelHeader.Controls.Add(lblSubtitulo);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.BackColor = Color.FromArgb(32, 32, 32);
            panelHeader.ForeColor = Color.FromArgb(255, 255, 255);
            panelHeader.Location = new Point(0, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(977, 85);
            panelHeader.TabIndex = 0;
            // 
            // lblTitulo
            // 
            lblTitulo.AutoSize = true;
            lblTitulo.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblTitulo.ForeColor = Color.FromArgb(255, 255, 255);
            lblTitulo.Location = new Point(24, 18);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new Size(187, 32);
            lblTitulo.TabIndex = 0;
            lblTitulo.Text = "🧪 dev-sandbox";
            // 
            // lblSubtitulo
            // 
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.Font = new Font("Segoe UI", 9.5F);
            lblSubtitulo.ForeColor = Color.FromArgb(160, 160, 160);
            lblSubtitulo.Location = new Point(28, 52);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.Size = new Size(295, 17);
            lblSubtitulo.TabIndex = 1;
            lblSubtitulo.Text = "Central de Utilitários, Comunicação e Ferramentas";
            // 
            // cardSupervisorio
            // 
            cardSupervisorio.Controls.Add(lblDescSupervisorio);
            cardSupervisorio.Controls.Add(lblTituloSupervisorio);
            cardSupervisorio.Controls.Add(btnAbrirSupervisorio);
            cardSupervisorio.CornerRadius = 8;
            cardSupervisorio.EnableHoverEffect = true;
            cardSupervisorio.Cursor = Cursors.Hand;
            cardSupervisorio.Location = new Point(28, 105);
            cardSupervisorio.Name = "cardSupervisorio";
            cardSupervisorio.Size = new Size(210, 245);
            cardSupervisorio.TabIndex = 1;
            // 
            // lblDescSupervisorio
            // 
            lblDescSupervisorio.Font = new Font("Segoe UI", 9F);
            lblDescSupervisorio.ForeColor = Color.FromArgb(160, 160, 160);
            lblDescSupervisorio.Location = new Point(14, 55);
            lblDescSupervisorio.Name = "lblDescSupervisorio";
            lblDescSupervisorio.Size = new Size(182, 115);
            lblDescSupervisorio.TabIndex = 2;
            lblDescSupervisorio.Text = "Monitor de comunicação serial (UART/COM) em tempo real, com watchdog e auto-reconexão.";
            // 
            // lblTituloSupervisorio
            // 
            lblTituloSupervisorio.AutoSize = true;
            lblTituloSupervisorio.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblTituloSupervisorio.ForeColor = Color.FromArgb(255, 255, 255);
            lblTituloSupervisorio.Location = new Point(14, 18);
            lblTituloSupervisorio.Name = "lblTituloSupervisorio";
            lblTituloSupervisorio.Size = new Size(160, 21);
            lblTituloSupervisorio.TabIndex = 0;
            lblTituloSupervisorio.Text = "🔌 Supervisório COM";
            // 
            // btnAbrirSupervisorio
            // 
            btnAbrirSupervisorio.IsAccent = true;
            btnAbrirSupervisorio.CornerRadius = 6;
            btnAbrirSupervisorio.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnAbrirSupervisorio.Location = new Point(14, 185);
            btnAbrirSupervisorio.Name = "btnAbrirSupervisorio";
            btnAbrirSupervisorio.Size = new Size(182, 38);
            btnAbrirSupervisorio.TabIndex = 1;
            btnAbrirSupervisorio.Text = "Abrir Supervisório";
            btnAbrirSupervisorio.Click += btnAbrirSupervisorio_Click;
            // 
            // cardPomodoro
            // 
            cardPomodoro.Controls.Add(lblDescPomodoro);
            cardPomodoro.Controls.Add(lblTituloPomodoro);
            cardPomodoro.Controls.Add(btnAbrirPomodoro);
            cardPomodoro.CornerRadius = 8;
            cardPomodoro.EnableHoverEffect = true;
            cardPomodoro.Cursor = Cursors.Hand;
            cardPomodoro.Location = new Point(265, 105);
            cardPomodoro.Name = "cardPomodoro";
            cardPomodoro.Size = new Size(210, 245);
            cardPomodoro.TabIndex = 2;
            // 
            // lblDescPomodoro
            // 
            lblDescPomodoro.Font = new Font("Segoe UI", 9F);
            lblDescPomodoro.ForeColor = Color.FromArgb(160, 160, 160);
            lblDescPomodoro.Location = new Point(14, 55);
            lblDescPomodoro.Name = "lblDescPomodoro";
            lblDescPomodoro.Size = new Size(182, 115);
            lblDescPomodoro.TabIndex = 2;
            lblDescPomodoro.Text = "Gerenciador de tempo e produtividade para ciclos de foco e descanso com alertas sonoros.";
            // 
            // lblTituloPomodoro
            // 
            lblTituloPomodoro.AutoSize = true;
            lblTituloPomodoro.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblTituloPomodoro.ForeColor = Color.FromArgb(255, 255, 255);
            lblTituloPomodoro.Location = new Point(14, 18);
            lblTituloPomodoro.Name = "lblTituloPomodoro";
            lblTituloPomodoro.Size = new Size(162, 21);
            lblTituloPomodoro.TabIndex = 0;
            lblTituloPomodoro.Text = "⏱️ Pomodoro Timer";
            // 
            // btnAbrirPomodoro
            // 
            btnAbrirPomodoro.IsAccent = true;
            btnAbrirPomodoro.CornerRadius = 6;
            btnAbrirPomodoro.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnAbrirPomodoro.Location = new Point(14, 185);
            btnAbrirPomodoro.Name = "btnAbrirPomodoro";
            btnAbrirPomodoro.Size = new Size(182, 38);
            btnAbrirPomodoro.TabIndex = 1;
            btnAbrirPomodoro.Text = "Abrir Pomodoro";
            btnAbrirPomodoro.Click += btnAbrirPomodoro_Click;
            // 
            // cardCalculadora
            // 
            cardCalculadora.Controls.Add(lblDescCalculadora);
            cardCalculadora.Controls.Add(lblTituloCalculadora);
            cardCalculadora.Controls.Add(btnAbrirCalculadora);
            cardCalculadora.CornerRadius = 8;
            cardCalculadora.EnableHoverEffect = true;
            cardCalculadora.Cursor = Cursors.Hand;
            cardCalculadora.Location = new Point(502, 105);
            cardCalculadora.Name = "cardCalculadora";
            cardCalculadora.Size = new Size(210, 245);
            cardCalculadora.TabIndex = 3;
            // 
            // lblDescCalculadora
            // 
            lblDescCalculadora.Font = new Font("Segoe UI", 9F);
            lblDescCalculadora.ForeColor = Color.FromArgb(160, 160, 160);
            lblDescCalculadora.Location = new Point(14, 55);
            lblDescCalculadora.Name = "lblDescCalculadora";
            lblDescCalculadora.Size = new Size(182, 115);
            lblDescCalculadora.TabIndex = 2;
            lblDescCalculadora.Text = "Conversão bidirecional entre bases Hexadecimal, Decimal e Binário de 64 bits com cópia rápida.";
            // 
            // lblTituloCalculadora
            // 
            lblTituloCalculadora.AutoSize = true;
            lblTituloCalculadora.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblTituloCalculadora.ForeColor = Color.FromArgb(255, 255, 255);
            lblTituloCalculadora.Location = new Point(14, 18);
            lblTituloCalculadora.Name = "lblTituloCalculadora";
            lblTituloCalculadora.Size = new Size(149, 21);
            lblTituloCalculadora.TabIndex = 0;
            lblTituloCalculadora.Text = "🔢 Calculadora Hex";
            // 
            // btnAbrirCalculadora
            // 
            btnAbrirCalculadora.IsAccent = true;
            btnAbrirCalculadora.CornerRadius = 6;
            btnAbrirCalculadora.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnAbrirCalculadora.Location = new Point(14, 185);
            btnAbrirCalculadora.Name = "btnAbrirCalculadora";
            btnAbrirCalculadora.Size = new Size(182, 38);
            btnAbrirCalculadora.TabIndex = 1;
            btnAbrirCalculadora.Text = "Abrir Calculadora";
            btnAbrirCalculadora.Click += btnAbrirCalculadora_Click;
            // 
            // cardBancada
            // 
            cardBancada.Controls.Add(lblDescBancada);
            cardBancada.Controls.Add(lblTituloBancada);
            cardBancada.Controls.Add(btnAbrirBancada);
            cardBancada.CornerRadius = 8;
            cardBancada.EnableHoverEffect = true;
            cardBancada.Cursor = Cursors.Hand;
            cardBancada.Location = new Point(739, 105);
            cardBancada.Name = "cardBancada";
            cardBancada.Size = new Size(210, 245);
            cardBancada.TabIndex = 4;
            // 
            // lblDescBancada
            // 
            lblDescBancada.Font = new Font("Segoe UI", 9F);
            lblDescBancada.ForeColor = Color.FromArgb(160, 160, 160);
            lblDescBancada.Location = new Point(14, 55);
            lblDescBancada.Name = "lblDescBancada";
            lblDescBancada.Size = new Size(182, 115);
            lblDescBancada.TabIndex = 2;
            lblDescBancada.Text = "Emulador de bancada para estações de recarga Modo 3. Controle de cabo (PP) e estados do veículo (CP).";
            // 
            // lblTituloBancada
            // 
            lblTituloBancada.AutoSize = true;
            lblTituloBancada.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblTituloBancada.ForeColor = Color.FromArgb(255, 255, 255);
            lblTituloBancada.Location = new Point(14, 18);
            lblTituloBancada.Name = "lblTituloBancada";
            lblTituloBancada.Size = new Size(160, 21);
            lblTituloBancada.TabIndex = 0;
            lblTituloBancada.Text = "⚡ Bancada EVSE";
            // 
            // btnAbrirBancada
            // 
            btnAbrirBancada.IsAccent = true;
            btnAbrirBancada.CornerRadius = 6;
            btnAbrirBancada.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnAbrirBancada.Location = new Point(14, 185);
            btnAbrirBancada.Name = "btnAbrirBancada";
            btnAbrirBancada.Size = new Size(182, 38);
            btnAbrirBancada.TabIndex = 1;
            btnAbrirBancada.Text = "Abrir Bancada";
            btnAbrirBancada.Click += btnAbrirBancada_Click;
            // 
            // panelFooter
            // 
            panelFooter.Controls.Add(lblFooter);
            panelFooter.Dock = DockStyle.Bottom;
            panelFooter.BackColor = Color.FromArgb(32, 32, 32);
            panelFooter.ForeColor = Color.FromArgb(160, 160, 160);
            panelFooter.Location = new Point(0, 375);
            panelFooter.Name = "panelFooter";
            panelFooter.Size = new Size(977, 35);
            panelFooter.TabIndex = 5;
            // 
            // lblFooter
            // 
            lblFooter.AutoSize = true;
            lblFooter.Font = new Font("Segoe UI", 8.5F);
            lblFooter.ForeColor = Color.FromArgb(160, 160, 160);
            lblFooter.Location = new Point(28, 10);
            lblFooter.Name = "lblFooter";
            lblFooter.Size = new Size(250, 15);
            lblFooter.TabIndex = 0;
            lblFooter.Text = ".NET 10.0 • Windows 11 Fluent Design Edition";
            // 
            // MainForm
            // 
            BackColor = Color.FromArgb(32, 32, 32);
            ForeColor = Color.FromArgb(255, 255, 255);
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(977, 410);
            Controls.Add(panelFooter);
            Controls.Add(cardBancada);
            Controls.Add(cardCalculadora);
            Controls.Add(cardPomodoro);
            Controls.Add(cardSupervisorio);
            Controls.Add(panelHeader);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "dev-sandbox | Central de Utilitários";
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            cardSupervisorio.ResumeLayout(false);
            cardSupervisorio.PerformLayout();
            cardPomodoro.ResumeLayout(false);
            cardPomodoro.PerformLayout();
            cardCalculadora.ResumeLayout(false);
            cardCalculadora.PerformLayout();
            cardBancada.ResumeLayout(false);
            cardBancada.PerformLayout();
            panelFooter.ResumeLayout(false);
            panelFooter.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel panelHeader;
        private Label lblTitulo;
        private Label lblSubtitulo;
        private DevSandbox.UI.Common.FluentCard cardSupervisorio;
        private Label lblTituloSupervisorio;
        private Label lblDescSupervisorio;
        private DevSandbox.UI.Common.FluentButton btnAbrirSupervisorio;
        private DevSandbox.UI.Common.FluentCard cardPomodoro;
        private Label lblTituloPomodoro;
        private Label lblDescPomodoro;
        private DevSandbox.UI.Common.FluentButton btnAbrirPomodoro;
        private DevSandbox.UI.Common.FluentCard cardCalculadora;
        private Label lblTituloCalculadora;
        private Label lblDescCalculadora;
        private DevSandbox.UI.Common.FluentButton btnAbrirCalculadora;
        private DevSandbox.UI.Common.FluentCard cardBancada;
        private Label lblTituloBancada;
        private Label lblDescBancada;
        private DevSandbox.UI.Common.FluentButton btnAbrirBancada;
        private Panel panelFooter;
        private Label lblFooter;
    }
}
