namespace dev_sandbox.UI
{
    partial class FormSupervisorio
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            groupBox1 = new GroupBox();
            btnEnviar = new Button();
            txtEnviar = new TextBox();
            txtHistorico = new TextBox();
            btnAtualizarPortas = new Button();
            lblStatus = new Label();
            prgStatus = new ProgressBar();
            label2 = new Label();
            btnConectar = new Button();
            cboBaudRate = new ComboBox();
            cboPortas = new ComboBox();
            label1 = new Label();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(btnEnviar);
            groupBox1.Controls.Add(txtEnviar);
            groupBox1.Controls.Add(txtHistorico);
            groupBox1.Controls.Add(btnAtualizarPortas);
            groupBox1.Controls.Add(lblStatus);
            groupBox1.Controls.Add(prgStatus);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(btnConectar);
            groupBox1.Controls.Add(cboBaudRate);
            groupBox1.Controls.Add(cboPortas);
            groupBox1.Controls.Add(label1);
            groupBox1.Location = new Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(289, 426);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "groupBox1";
            // 
            // btnEnviar
            // 
            btnEnviar.Location = new Point(202, 397);
            btnEnviar.Name = "btnEnviar";
            btnEnviar.Size = new Size(75, 23);
            btnEnviar.TabIndex = 10;
            btnEnviar.Text = "Enviar";
            btnEnviar.UseVisualStyleBackColor = true;
            btnEnviar.Click += btnEnviar_Click;
            // 
            // txtEnviar
            // 
            txtEnviar.Location = new Point(6, 397);
            txtEnviar.Name = "txtEnviar";
            txtEnviar.Size = new Size(190, 23);
            txtEnviar.TabIndex = 9;
            // 
            // txtHistorico
            // 
            txtHistorico.Location = new Point(6, 109);
            txtHistorico.Multiline = true;
            txtHistorico.Name = "txtHistorico";
            txtHistorico.Size = new Size(271, 282);
            txtHistorico.TabIndex = 8;
            // 
            // btnAtualizarPortas
            // 
            btnAtualizarPortas.Location = new Point(202, 22);
            btnAtualizarPortas.Name = "btnAtualizarPortas";
            btnAtualizarPortas.Size = new Size(75, 23);
            btnAtualizarPortas.TabIndex = 7;
            btnAtualizarPortas.Text = "Atualizar";
            btnAtualizarPortas.UseVisualStyleBackColor = true;
            btnAtualizarPortas.Click += btnAtualizarPortas_Click;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(202, 84);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(38, 15);
            lblStatus.TabIndex = 6;
            lblStatus.Text = "label3";
            // 
            // prgStatus
            // 
            prgStatus.Location = new Point(75, 80);
            prgStatus.Name = "prgStatus";
            prgStatus.Size = new Size(121, 23);
            prgStatus.TabIndex = 5;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(6, 55);
            label2.Name = "label2";
            label2.Size = new Size(63, 15);
            label2.TabIndex = 4;
            label2.Text = "Baud Rate:";
            // 
            // btnConectar
            // 
            btnConectar.Location = new Point(202, 50);
            btnConectar.Name = "btnConectar";
            btnConectar.Size = new Size(75, 23);
            btnConectar.TabIndex = 3;
            btnConectar.Text = "Conectar";
            btnConectar.UseVisualStyleBackColor = true;
            btnConectar.Click += btnConectar_Click;
            // 
            // cboBaudRate
            // 
            cboBaudRate.FormattingEnabled = true;
            cboBaudRate.Location = new Point(75, 51);
            cboBaudRate.Name = "cboBaudRate";
            cboBaudRate.Size = new Size(121, 23);
            cboBaudRate.TabIndex = 2;
            cboBaudRate.Text = "9600";
            // 
            // cboPortas
            // 
            cboPortas.FormattingEnabled = true;
            cboPortas.Location = new Point(75, 22);
            cboPortas.Name = "cboPortas";
            cboPortas.Size = new Size(121, 23);
            cboPortas.TabIndex = 1;
            cboPortas.Text = "COM10";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(6, 26);
            label1.Name = "label1";
            label1.Size = new Size(46, 15);
            label1.TabIndex = 0;
            label1.Text = "Portas: ";
            // 
            // FormSupervisorio
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(groupBox1);
            Name = "FormSupervisorio";
            Text = "Supervisorio";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBox1;
        private Label label2;
        private Button btnConectar;
        private ComboBox cboBaudRate;
        private ComboBox cboPortas;
        private Label label1;
        private ProgressBar prgStatus;
        private Label lblStatus;
        private Button btnAtualizarPortas;
        private Button btnEnviar;
        private TextBox txtEnviar;
        private TextBox txtHistorico;
    }
}