using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevSandbox.Core.Converters;
using DevSandbox.UI.Common;

namespace DevSandbox.UI.Forms
{
    public partial class FormCalculadora : Form
    {
        private readonly CalculadoraHex _calculadora = new();
        private string _rawHex = "";
        private string _rawDec = "";
        private string _rawBin = "";

        public FormCalculadora()
        {
            InitializeComponent();

            ThemeManager.AplicarBarraDeTituloEscura(this);
            AplicarTema();

            txtEntrada.InnerKeyDown += txtEntrada_KeyDown;
        }

        private void AplicarTema()
        {
            this.BackColor = ThemeManager.FundoJanela;
            this.ForeColor = ThemeManager.TextoPrincipal;

            lblEntrada.ForeColor = ThemeManager.TextoPrincipal;
            lblTituloResultados.ForeColor = ThemeManager.TextoSecundario;

            cardResultados.CardColor = ThemeManager.FundoCard;
            cardResultados.BorderColor = ThemeManager.BordaCard;

            // Cores dos valores de resultado
            lblHexValor.ForeColor = ThemeManager.CorAcento;
            lblDecimalValor.ForeColor = ThemeManager.TextoPrincipal;
            lblBinarioValor.ForeColor = ThemeManager.CorAcento;
        }

        private void btnConverterHex_Click(object sender, EventArgs e)
        {
            try
            {
                string entrada = txtEntrada.Text;
                long decimalValor = _calculadora.HexParaDecimal(entrada);
                string hexFormatado = _calculadora.DecimalParaHex(decimalValor);
                string binario = _calculadora.DecimalParaBinario(decimalValor);

                ExibirResultados(hexFormatado, decimalValor, binario);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (FormatException)
            {
                MessageBox.Show("Valor Hexadecimal inválido! Use apenas caracteres de 0-9 e A-F.",
                                "Erro de Formatação", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (OverflowException)
            {
                MessageBox.Show("O número digitado é muito grande para um inteiro de 64 bits.",
                                "Erro de Limite", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnConverterDec_Click(object sender, EventArgs e)
        {
            try
            {
                string entrada = txtEntrada.Text.Trim().Replace(" ", "").Replace("_", "");
                if (string.IsNullOrWhiteSpace(entrada))
                {
                    MessageBox.Show("Digite um valor decimal.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                long decimalValor;
                if (entrada.StartsWith("-"))
                {
                    decimalValor = long.Parse(entrada);
                }
                else
                {
                    ulong unsignedVal = ulong.Parse(entrada);
                    decimalValor = unchecked((long)unsignedVal);
                }

                string hexFormatado = _calculadora.DecimalParaHex(decimalValor);
                string binario = _calculadora.DecimalParaBinario(decimalValor);

                ExibirResultados(hexFormatado, decimalValor, binario);
            }
            catch (FormatException)
            {
                MessageBox.Show("Valor Decimal inválido! Digite apenas dígitos numéricos de 0 a 9.",
                                "Erro de Formatação", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (OverflowException)
            {
                MessageBox.Show("O número digitado é muito grande para um inteiro de 64 bits.",
                                "Erro de Limite", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnConverterBin_Click(object sender, EventArgs e)
        {
            try
            {
                string entrada = txtEntrada.Text;
                long decimalValor = _calculadora.BinarioParaDecimal(entrada);
                string hexFormatado = _calculadora.DecimalParaHex(decimalValor);
                string binario = _calculadora.DecimalParaBinario(decimalValor);

                ExibirResultados(hexFormatado, decimalValor, binario);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (FormatException)
            {
                MessageBox.Show("Valor Binário inválido! Use apenas dígitos 0 ou 1.",
                                "Erro de Formatação", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (OverflowException)
            {
                MessageBox.Show("O valor binário excede a capacidade de 64 bits.",
                                "Erro de Limite", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExibirResultados(string hex, long dec, string bin)
        {
            _rawHex = $"0x{hex}";
            _rawDec = dec.ToString();
            _rawBin = bin;

            lblHexValor.Text = _rawHex;
            lblDecimalValor.Text = dec.ToString("N0");
            lblBinarioValor.Text = _calculadora.FormatarBinario(bin);
        }

        private async void btnCopiarHex_Click(object sender, EventArgs e)
        {
            await CopiarParaClipboard(_rawHex, btnCopiarHex);
        }

        private async void btnCopiarDec_Click(object sender, EventArgs e)
        {
            await CopiarParaClipboard(_rawDec, btnCopiarDec);
        }

        private async void btnCopiarBin_Click(object sender, EventArgs e)
        {
            await CopiarParaClipboard(_rawBin, btnCopiarBin);
        }

        private async Task CopiarParaClipboard(string valor, FluentButton botao)
        {
            if (string.IsNullOrWhiteSpace(valor) || valor == "-")
                return;

            try
            {
                Clipboard.SetText(valor);
                string textoOriginal = botao.Text;
                botao.Text = "✓ Copiado!";
                botao.CustomAccentColor = ThemeManager.VerdeEscuro;
                botao.IsAccent = true;
                await Task.Delay(1200);
                botao.Text = textoOriginal;
                botao.IsAccent = false;
                botao.CustomAccentColor = null;
            }
            catch
            {
                // Silencia concorrência com a área de transferência do Windows
            }
        }

        private void txtEntrada_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string texto = txtEntrada.Text.Trim();
                if (texto.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || ContemLetrasHex(texto))
                {
                    btnConverterHex_Click(this, EventArgs.Empty);
                }
                else if (texto.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
                {
                    btnConverterBin_Click(this, EventArgs.Empty);
                }
                else
                {
                    btnConverterDec_Click(this, EventArgs.Empty);
                }
                e.SuppressKeyPress = true;
            }
        }

        private static bool ContemLetrasHex(string texto)
        {
            foreach (char c in texto)
            {
                if ((c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')) return true;
            }
            return false;
        }
    }
}
