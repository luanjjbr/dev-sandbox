namespace dev_sandbox
{
    partial class FormCalculadora
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
            lblResultado = new Label();
            btnConverter = new Button();
            txtHex = new TextBox();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(lblResultado);
            groupBox1.Controls.Add(btnConverter);
            groupBox1.Controls.Add(txtHex);
            groupBox1.Location = new Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(294, 426);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Hex -> decimal";
            // 
            // lblResultado
            // 
            lblResultado.AutoSize = true;
            lblResultado.Location = new Point(12, 64);
            lblResultado.Name = "lblResultado";
            lblResultado.Size = new Size(70, 15);
            lblResultado.TabIndex = 2;
            lblResultado.Text = "Resultado: -";
            lblResultado.Click += lblResultado_Click;
            // 
            // btnConverter
            // 
            btnConverter.Location = new Point(112, 22);
            btnConverter.Name = "btnConverter";
            btnConverter.Size = new Size(75, 23);
            btnConverter.TabIndex = 1;
            btnConverter.Text = "Converter";
            btnConverter.UseVisualStyleBackColor = true;
            btnConverter.Click += btnConverter_Click;
            // 
            // txtHex
            // 
            txtHex.Location = new Point(12, 22);
            txtHex.Name = "txtHex";
            txtHex.Size = new Size(100, 23);
            txtHex.TabIndex = 0;
            // 
            // FormCalculadora
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(groupBox1);
            Name = "FormCalculadora";
            Text = "FormCalculadora";
            Load += FormCalculadora_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBox1;
        private Label lblResultado;
        private Button btnConverter;
        private TextBox txtHex;
    }
}