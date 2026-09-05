namespace dev_sandbox
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            groupBox1 = new GroupBox();
            btnTempo05 = new Button();
            btnIniciar = new Button();
            numCiclos = new NumericUpDown();
            btnTempo15 = new Button();
            btnTempo25 = new Button();
            lblTempo = new Label();
            timerPomodoro = new System.Windows.Forms.Timer(components);
            menuStrip1 = new MenuStrip();
            calculadoraToolStripMenuItem = new ToolStripMenuItem();
            superToolStripMenuItem = new ToolStripMenuItem();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numCiclos).BeginInit();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(btnTempo05);
            groupBox1.Controls.Add(btnIniciar);
            groupBox1.Controls.Add(numCiclos);
            groupBox1.Controls.Add(btnTempo15);
            groupBox1.Controls.Add(btnTempo25);
            groupBox1.Controls.Add(lblTempo);
            groupBox1.Location = new Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(352, 186);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Pomodoro";
            // 
            // btnTempo05
            // 
            btnTempo05.Location = new Point(94, 86);
            btnTempo05.Name = "btnTempo05";
            btnTempo05.Size = new Size(75, 23);
            btnTempo05.TabIndex = 6;
            btnTempo05.Text = "05:00";
            btnTempo05.UseVisualStyleBackColor = true;
            btnTempo05.Click += btnTempo05_Click;
            // 
            // btnIniciar
            // 
            btnIniciar.Location = new Point(94, 118);
            btnIniciar.Name = "btnIniciar";
            btnIniciar.Size = new Size(75, 23);
            btnIniciar.TabIndex = 5;
            btnIniciar.Text = "Iniciar";
            btnIniciar.UseVisualStyleBackColor = true;
            btnIniciar.Click += btnIniciar_Click;
            // 
            // numCiclos
            // 
            numCiclos.BackColor = SystemColors.Window;
            numCiclos.Location = new Point(13, 118);
            numCiclos.Maximum = new decimal(new int[] { 30, 0, 0, 0 });
            numCiclos.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numCiclos.Name = "numCiclos";
            numCiclos.Size = new Size(75, 23);
            numCiclos.TabIndex = 4;
            numCiclos.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // btnTempo15
            // 
            btnTempo15.Location = new Point(94, 54);
            btnTempo15.Name = "btnTempo15";
            btnTempo15.Size = new Size(75, 23);
            btnTempo15.TabIndex = 3;
            btnTempo15.Text = "15:00";
            btnTempo15.UseVisualStyleBackColor = true;
            btnTempo15.Click += btnTempo15_Click;
            // 
            // btnTempo25
            // 
            btnTempo25.Location = new Point(94, 22);
            btnTempo25.Name = "btnTempo25";
            btnTempo25.Size = new Size(75, 23);
            btnTempo25.TabIndex = 2;
            btnTempo25.Text = "25:00";
            btnTempo25.UseVisualStyleBackColor = true;
            btnTempo25.Click += btnTempo25_Click;
            // 
            // lblTempo
            // 
            lblTempo.AutoSize = true;
            lblTempo.Font = new Font("Segoe UI", 25F, FontStyle.Bold);
            lblTempo.Location = new Point(224, 66);
            lblTempo.Name = "lblTempo";
            lblTempo.Size = new Size(109, 46);
            lblTempo.TabIndex = 1;
            lblTempo.Text = "25:00";
            // 
            // menuStrip1
            // 
            menuStrip1.Dock = DockStyle.Bottom;
            menuStrip1.Items.AddRange(new ToolStripItem[] { calculadoraToolStripMenuItem, superToolStripMenuItem });
            menuStrip1.Location = new Point(0, 436);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(800, 24);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // calculadoraToolStripMenuItem
            // 
            calculadoraToolStripMenuItem.Name = "calculadoraToolStripMenuItem";
            calculadoraToolStripMenuItem.Size = new Size(82, 20);
            calculadoraToolStripMenuItem.Text = "Calculadora";
            calculadoraToolStripMenuItem.Click += calculadoraToolStripMenuItem_Click;
            // 
            // superToolStripMenuItem
            // 
            superToolStripMenuItem.Name = "superToolStripMenuItem";
            superToolStripMenuItem.Size = new Size(84, 20);
            superToolStripMenuItem.Text = "Supervisorio";
            superToolStripMenuItem.Click += superToolStripMenuItem_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 460);
            Controls.Add(groupBox1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "dev-sandbox";
            Load += Form1_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numCiclos).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private GroupBox groupBox1;
        private Button btnTempo05;
        private Button btnIniciar;
        private NumericUpDown numCiclos;
        private Button btnTempo15;
        private Button btnTempo25;
        private Label lblTempo;
        private System.Windows.Forms.Timer timerPomodoro;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem calculadoraToolStripMenuItem;
        private ToolStripMenuItem superToolStripMenuItem;
    }
}
