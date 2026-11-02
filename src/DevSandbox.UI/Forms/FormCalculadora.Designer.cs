namespace DevSandbox.UI.Forms
{
    partial class FormCalculadora
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
            lblEntrada = new Label();
            txtEntrada = new DevSandbox.UI.Common.FluentTextBox();
            btnConverterHex = new DevSandbox.UI.Common.FluentButton();
            btnConverterDec = new DevSandbox.UI.Common.FluentButton();
            btnConverterBin = new DevSandbox.UI.Common.FluentButton();
            cardResultados = new DevSandbox.UI.Common.FluentCard();
            lblTituloResultados = new Label();
            btnCopiarBin = new DevSandbox.UI.Common.FluentButton();
            btnCopiarDec = new DevSandbox.UI.Common.FluentButton();
            btnCopiarHex = new DevSandbox.UI.Common.FluentButton();
            lblBinarioValor = new Label();
            lblBinarioTitulo = new Label();
            lblDecimalValor = new Label();
            lblDecimalTitulo = new Label();
            lblHexValor = new Label();
            lblHexTitulo = new Label();
            cardResultados.SuspendLayout();
            SuspendLayout();
            // 
            // lblEntrada
            // 
            lblEntrada.AutoSize = true;
            lblEntrada.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblEntrada.ForeColor = Color.FromArgb(255, 255, 255);
            lblEntrada.Location = new Point(16, 18);
            lblEntrada.Name = "lblEntrada";
            lblEntrada.Size = new Size(167, 17);
            lblEntrada.TabIndex = 0;
            lblEntrada.Text = "Digite o valor de entrada:";
            // 
            // txtEntrada
            // 
            txtEntrada.BackColor = Color.Transparent;
            txtEntrada.Location = new Point(16, 42);
            txtEntrada.Name = "txtEntrada";
            txtEntrada.Padding = new Padding(10, 8, 10, 8);
            txtEntrada.Size = new Size(245, 34);
            txtEntrada.TabIndex = 1;
            // 
            // btnConverterHex
            // 
            btnConverterHex.BackColor = Color.Transparent;
            btnConverterHex.FlatStyle = FlatStyle.Flat;
            btnConverterHex.Font = new Font("Segoe UI", 9F);
            btnConverterHex.Location = new Point(270, 42);
            btnConverterHex.Name = "btnConverterHex";
            btnConverterHex.Size = new Size(82, 34);
            btnConverterHex.TabIndex = 2;
            btnConverterHex.Text = "É Hex (16)";
            btnConverterHex.UseVisualStyleBackColor = false;
            btnConverterHex.Click += btnConverterHex_Click;
            // 
            // btnConverterDec
            // 
            btnConverterDec.BackColor = Color.Transparent;
            btnConverterDec.FlatStyle = FlatStyle.Flat;
            btnConverterDec.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnConverterDec.Location = new Point(358, 42);
            btnConverterDec.Name = "btnConverterDec";
            btnConverterDec.Size = new Size(82, 34);
            btnConverterDec.TabIndex = 3;
            btnConverterDec.Text = "É Dec (10)";
            btnConverterDec.UseVisualStyleBackColor = false;
            btnConverterDec.Click += btnConverterDec_Click;
            // 
            // btnConverterBin
            // 
            btnConverterBin.BackColor = Color.Transparent;
            btnConverterBin.FlatStyle = FlatStyle.Flat;
            btnConverterBin.Font = new Font("Segoe UI", 9F);
            btnConverterBin.Location = new Point(446, 42);
            btnConverterBin.Name = "btnConverterBin";
            btnConverterBin.Size = new Size(82, 34);
            btnConverterBin.TabIndex = 4;
            btnConverterBin.Text = "É Bin (2)";
            btnConverterBin.UseVisualStyleBackColor = false;
            btnConverterBin.Click += btnConverterBin_Click;
            // 
            // cardResultados
            // 
            cardResultados.BackColor = Color.Transparent;
            cardResultados.Controls.Add(lblTituloResultados);
            cardResultados.Controls.Add(btnCopiarBin);
            cardResultados.Controls.Add(btnCopiarDec);
            cardResultados.Controls.Add(btnCopiarHex);
            cardResultados.Controls.Add(lblBinarioValor);
            cardResultados.Controls.Add(lblBinarioTitulo);
            cardResultados.Controls.Add(lblDecimalValor);
            cardResultados.Controls.Add(lblDecimalTitulo);
            cardResultados.Controls.Add(lblHexValor);
            cardResultados.Controls.Add(lblHexTitulo);
            cardResultados.Location = new Point(16, 92);
            cardResultados.Name = "cardResultados";
            cardResultados.Padding = new Padding(16);
            cardResultados.Size = new Size(512, 195);
            cardResultados.TabIndex = 5;
            // 
            // lblTituloResultados
            // 
            lblTituloResultados.AutoSize = true;
            lblTituloResultados.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblTituloResultados.ForeColor = Color.FromArgb(160, 160, 160);
            lblTituloResultados.Location = new Point(14, 10);
            lblTituloResultados.Name = "lblTituloResultados";
            lblTituloResultados.Size = new Size(193, 17);
            lblTituloResultados.TabIndex = 9;
            lblTituloResultados.Text = "RESULTADOS DA CONVERSÃO";
            // 
            // btnCopiarBin
            // 
            btnCopiarBin.BackColor = Color.Transparent;
            btnCopiarBin.FlatStyle = FlatStyle.Flat;
            btnCopiarBin.Font = new Font("Segoe UI", 8.5F);
            btnCopiarBin.Location = new Point(420, 125);
            btnCopiarBin.Name = "btnCopiarBin";
            btnCopiarBin.Size = new Size(78, 28);
            btnCopiarBin.TabIndex = 8;
            btnCopiarBin.Text = "📋 Copiar";
            btnCopiarBin.UseVisualStyleBackColor = false;
            btnCopiarBin.Click += btnCopiarBin_Click;
            // 
            // btnCopiarDec
            // 
            btnCopiarDec.BackColor = Color.Transparent;
            btnCopiarDec.FlatStyle = FlatStyle.Flat;
            btnCopiarDec.Font = new Font("Segoe UI", 8.5F);
            btnCopiarDec.Location = new Point(420, 80);
            btnCopiarDec.Name = "btnCopiarDec";
            btnCopiarDec.Size = new Size(78, 28);
            btnCopiarDec.TabIndex = 7;
            btnCopiarDec.Text = "📋 Copiar";
            btnCopiarDec.UseVisualStyleBackColor = false;
            btnCopiarDec.Click += btnCopiarDec_Click;
            // 
            // btnCopiarHex
            // 
            btnCopiarHex.BackColor = Color.Transparent;
            btnCopiarHex.FlatStyle = FlatStyle.Flat;
            btnCopiarHex.Font = new Font("Segoe UI", 8.5F);
            btnCopiarHex.Location = new Point(420, 36);
            btnCopiarHex.Name = "btnCopiarHex";
            btnCopiarHex.Size = new Size(78, 28);
            btnCopiarHex.TabIndex = 6;
            btnCopiarHex.Text = "📋 Copiar";
            btnCopiarHex.UseVisualStyleBackColor = false;
            btnCopiarHex.Click += btnCopiarHex_Click;
            // 
            // lblBinarioValor
            // 
            lblBinarioValor.Font = new Font("Consolas", 10F);
            lblBinarioValor.ForeColor = Color.FromArgb(76, 194, 255);
            lblBinarioValor.Location = new Point(110, 128);
            lblBinarioValor.Name = "lblBinarioValor";
            lblBinarioValor.Size = new Size(305, 55);
            lblBinarioValor.TabIndex = 5;
            lblBinarioValor.Text = "-";
            // 
            // lblBinarioTitulo
            // 
            lblBinarioTitulo.AutoSize = true;
            lblBinarioTitulo.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblBinarioTitulo.ForeColor = Color.FromArgb(160, 160, 160);
            lblBinarioTitulo.Location = new Point(14, 128);
            lblBinarioTitulo.Name = "lblBinarioTitulo";
            lblBinarioTitulo.Size = new Size(49, 15);
            lblBinarioTitulo.TabIndex = 4;
            lblBinarioTitulo.Text = "Binário:";
            // 
            // lblDecimalValor
            // 
            lblDecimalValor.AutoSize = true;
            lblDecimalValor.Font = new Font("Consolas", 11F, FontStyle.Bold);
            lblDecimalValor.ForeColor = Color.FromArgb(255, 255, 255);
            lblDecimalValor.Location = new Point(110, 84);
            lblDecimalValor.Name = "lblDecimalValor";
            lblDecimalValor.Size = new Size(16, 18);
            lblDecimalValor.TabIndex = 3;
            lblDecimalValor.Text = "-";
            // 
            // lblDecimalTitulo
            // 
            lblDecimalTitulo.AutoSize = true;
            lblDecimalTitulo.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblDecimalTitulo.ForeColor = Color.FromArgb(160, 160, 160);
            lblDecimalTitulo.Location = new Point(14, 84);
            lblDecimalTitulo.Name = "lblDecimalTitulo";
            lblDecimalTitulo.Size = new Size(55, 15);
            lblDecimalTitulo.TabIndex = 2;
            lblDecimalTitulo.Text = "Decimal:";
            // 
            // lblHexValor
            // 
            lblHexValor.AutoSize = true;
            lblHexValor.Font = new Font("Consolas", 11F, FontStyle.Bold);
            lblHexValor.ForeColor = Color.FromArgb(76, 194, 255);
            lblHexValor.Location = new Point(110, 40);
            lblHexValor.Name = "lblHexValor";
            lblHexValor.Size = new Size(16, 18);
            lblHexValor.TabIndex = 1;
            lblHexValor.Text = "-";
            // 
            // lblHexTitulo
            // 
            lblHexTitulo.AutoSize = true;
            lblHexTitulo.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblHexTitulo.ForeColor = Color.FromArgb(160, 160, 160);
            lblHexTitulo.Location = new Point(14, 40);
            lblHexTitulo.Name = "lblHexTitulo";
            lblHexTitulo.Size = new Size(82, 15);
            lblHexTitulo.TabIndex = 0;
            lblHexTitulo.Text = "Hexadecimal:";
            // 
            // FormCalculadora
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(32, 32, 32);
            ClientSize = new Size(544, 305);
            Controls.Add(cardResultados);
            Controls.Add(btnConverterBin);
            Controls.Add(btnConverterDec);
            Controls.Add(btnConverterHex);
            Controls.Add(txtEntrada);
            Controls.Add(lblEntrada);
            ForeColor = Color.FromArgb(255, 255, 255);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "FormCalculadora";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Conversor de Bases Numéricas (Hex / Dec / Bin)";
            cardResultados.ResumeLayout(false);
            cardResultados.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblEntrada;
        private DevSandbox.UI.Common.FluentTextBox txtEntrada;
        private DevSandbox.UI.Common.FluentButton btnConverterHex;
        private DevSandbox.UI.Common.FluentButton btnConverterDec;
        private DevSandbox.UI.Common.FluentButton btnConverterBin;
        private DevSandbox.UI.Common.FluentCard cardResultados;
        private Label lblTituloResultados;
        private Label lblHexTitulo;
        private Label lblHexValor;
        private Label lblDecimalTitulo;
        private Label lblDecimalValor;
        private Label lblBinarioTitulo;
        private Label lblBinarioValor;
        private DevSandbox.UI.Common.FluentButton btnCopiarHex;
        private DevSandbox.UI.Common.FluentButton btnCopiarDec;
        private DevSandbox.UI.Common.FluentButton btnCopiarBin;
    }
}
