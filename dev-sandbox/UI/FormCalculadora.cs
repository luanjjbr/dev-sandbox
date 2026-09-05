using dev_sandbox.Core;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace dev_sandbox
{
    public partial class FormCalculadora : Form
    {
        private readonly CalculadoraHex _calculadora = new CalculadoraHex();

        public FormCalculadora()
        {
            InitializeComponent();
        }

        private void FormCalculadora_Load(object sender, EventArgs e)
        {
            lblResultado.Text = "Resultado: -";
        }

        private void btnConverter_Click(object sender, EventArgs e)
        {
            try
            {
                string entradaHex = txtHex.Text.Trim();

                // Chama a regra de negócio testada unitariamente
                int resultadoDecimal = _calculadora.HexParaDecimal(entradaHex);

                // Exibe o resultado formatado
                lblResultado.Text = $"Decimal: {resultadoDecimal}";
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (FormatException)
            {
                MessageBox.Show("Valor Hexadecimal inválido! Use apenas números de 0-9 e letras de A-F.",
                                "Erro de Formatação", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (OverflowException)
            {
                MessageBox.Show("O número digitado é muito grande para um inteiro de 32 bits.",
                                "Erro de Limite", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void lblResultado_Click(object sender, EventArgs e)
        {
            // Evento gerado pelo clique na label (pode permanecer vazio)
        }

    }
}