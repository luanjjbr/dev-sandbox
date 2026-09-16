namespace DevSandbox.UI.Forms
{
    partial class FormPomodoro
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
            lblTempo = new Label();
            lblStatusCiclo = new Label();
            cardControles = new DevSandbox.UI.Common.FluentCard();
            btnIniciar = new DevSandbox.UI.Common.FluentButton();
            btnPausar = new DevSandbox.UI.Common.FluentButton();
            btnTempo25 = new DevSandbox.UI.Common.FluentButton();
            btnTempo15 = new DevSandbox.UI.Common.FluentButton();
            btnTempo05 = new DevSandbox.UI.Common.FluentButton();
            lblCustomMinutos = new Label();
            numCiclos = new NumericUpDown();
            cardControles.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numCiclos).BeginInit();
            SuspendLayout();
            // 
            // lblTempo
            // 
            lblTempo.Font = new Font("Segoe UI", 56F, FontStyle.Bold);
            lblTempo.ForeColor = Color.FromArgb(76, 194, 255);
            lblTempo.Location = new Point(12, 12);
            lblTempo.Name = "lblTempo";
            lblTempo.Size = new Size(410, 95);
            lblTempo.TabIndex = 0;
            lblTempo.Text = "25:00";
            lblTempo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblStatusCiclo
            // 
            lblStatusCiclo.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblStatusCiclo.ForeColor = Color.FromArgb(160, 160, 160);
            lblStatusCiclo.Location = new Point(12, 107);
            lblStatusCiclo.Name = "lblStatusCiclo";
            lblStatusCiclo.Size = new Size(410, 24);
            lblStatusCiclo.TabIndex = 1;
            lblStatusCiclo.Text = "Pronto para iniciar";
            lblStatusCiclo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // cardControles
            // 
            cardControles.Controls.Add(btnIniciar);
            cardControles.Controls.Add(btnPausar);
            cardControles.Controls.Add(btnTempo25);
            cardControles.Controls.Add(btnTempo15);
            cardControles.Controls.Add(btnTempo05);
            cardControles.Controls.Add(lblCustomMinutos);
            cardControles.Controls.Add(numCiclos);
            cardControles.CornerRadius = 8;
            cardControles.Location = new Point(16, 142);
            cardControles.Name = "cardControles";
            cardControles.Size = new Size(406, 105);
            cardControles.TabIndex = 2;
            // 
            // btnIniciar
            // 
            btnIniciar.IsAccent = true;
            btnIniciar.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnIniciar.Location = new Point(292, 14);
            btnIniciar.Name = "btnIniciar";
            btnIniciar.Size = new Size(98, 34);
            btnIniciar.TabIndex = 0;
            btnIniciar.Text = "Iniciar";
            btnIniciar.Click += btnIniciar_Click;
            // 
            // btnPausar
            // 
            btnPausar.Enabled = false;
            btnPausar.Font = new Font("Segoe UI", 9F);
            btnPausar.Location = new Point(292, 54);
            btnPausar.Name = "btnPausar";
            btnPausar.Size = new Size(98, 34);
            btnPausar.TabIndex = 1;
            btnPausar.Text = "Pausar";
            btnPausar.Click += btnPausar_Click;
            // 
            // btnTempo25
            // 
            btnTempo25.Font = new Font("Segoe UI", 9F);
            btnTempo25.Location = new Point(16, 14);
            btnTempo25.Name = "btnTempo25";
            btnTempo25.Size = new Size(80, 32);
            btnTempo25.TabIndex = 2;
            btnTempo25.Text = "25m Foco";
            btnTempo25.Click += btnTempo25_Click;
            // 
            // btnTempo15
            // 
            btnTempo15.Font = new Font("Segoe UI", 9F);
            btnTempo15.Location = new Point(104, 14);
            btnTempo15.Name = "btnTempo15";
            btnTempo15.Size = new Size(80, 32);
            btnTempo15.TabIndex = 3;
            btnTempo15.Text = "15m Longo";
            btnTempo15.Click += btnTempo15_Click;
            // 
            // btnTempo05
            // 
            btnTempo05.Font = new Font("Segoe UI", 9F);
            btnTempo05.Location = new Point(192, 14);
            btnTempo05.Name = "btnTempo05";
            btnTempo05.Size = new Size(80, 32);
            btnTempo05.TabIndex = 4;
            btnTempo05.Text = "05m Curto";
            btnTempo05.Click += btnTempo05_Click;
            // 
            // lblCustomMinutos
            // 
            lblCustomMinutos.AutoSize = true;
            lblCustomMinutos.Font = new Font("Segoe UI", 9F);
            lblCustomMinutos.ForeColor = Color.FromArgb(160, 160, 160);
            lblCustomMinutos.Location = new Point(16, 62);
            lblCustomMinutos.Name = "lblCustomMinutos";
            lblCustomMinutos.Size = new Size(115, 15);
            lblCustomMinutos.TabIndex = 5;
            lblCustomMinutos.Text = "Minutos específicos:";
            // 
            // numCiclos
            // 
            numCiclos.BackColor = Color.FromArgb(30, 30, 30);
            numCiclos.BorderStyle = BorderStyle.FixedSingle;
            numCiclos.Font = new Font("Segoe UI", 9.5F);
            numCiclos.ForeColor = Color.FromArgb(255, 255, 255);
            numCiclos.Location = new Point(140, 58);
            numCiclos.Maximum = new decimal(new int[] { 120, 0, 0, 0 });
            numCiclos.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numCiclos.Name = "numCiclos";
            numCiclos.Size = new Size(70, 24);
            numCiclos.TabIndex = 6;
            numCiclos.Value = new decimal(new int[] { 25, 0, 0, 0 });
            numCiclos.ValueChanged += numCiclos_ValueChanged;
            // 
            // FormPomodoro
            // 
            BackColor = Color.FromArgb(32, 32, 32);
            ForeColor = Color.FromArgb(255, 255, 255);
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(438, 265);
            Controls.Add(cardControles);
            Controls.Add(lblStatusCiclo);
            Controls.Add(lblTempo);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "FormPomodoro";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Pomodoro Timer • Fluent";
            Load += FormPomodoro_Load;
            cardControles.ResumeLayout(false);
            cardControles.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numCiclos).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Label lblTempo;
        private Label lblStatusCiclo;
        private DevSandbox.UI.Common.FluentCard cardControles;
        private DevSandbox.UI.Common.FluentButton btnIniciar;
        private DevSandbox.UI.Common.FluentButton btnPausar;
        private DevSandbox.UI.Common.FluentButton btnTempo25;
        private DevSandbox.UI.Common.FluentButton btnTempo15;
        private DevSandbox.UI.Common.FluentButton btnTempo05;
        private Label lblCustomMinutos;
        private NumericUpDown numCiclos;
    }
}
