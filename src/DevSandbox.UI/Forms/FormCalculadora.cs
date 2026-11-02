using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
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

        private void btnConverterHex_Click(object sender, EventArgs e) => Converter(BaseNumerica.Hexadecimal);
        private void btnConverterDec_Click(object sender, EventArgs e) => Converter(BaseNumerica.Decimal);
        private void btnConverterBin_Click(object sender, EventArgs e) => Converter(BaseNumerica.Binario);

        /// <summary>
        /// Converte a entrada na base informada ou, com <c>null</c>, detecta a base no Core (tecla Enter).
        /// Toda a regra de parsing (prefixos, separadores, sinal, limites de 64 bits) fica na CalculadoraHex.
        /// </summary>
        private void Converter(BaseNumerica? baseEscolhida)
        {
            string entrada = txtEntrada.Text;
            try
            {
                long valor = baseEscolhida is BaseNumerica b
                    ? _calculadora.Converter(entrada, b)
                    : _calculadora.ConverterEntrada(entrada, out _);

                ExibirResultados(_calculadora.DecimalParaHex(valor), valor, _calculadora.DecimalParaBinario(valor));
            }
            catch (ArgumentException)
            {
                MessageBox.Show("Entrada sem dígitos: digite um valor para converter.",
                                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (FormatException ex)
            {
                // Mensagens do Core: negativo, hífen fora de lugar ou dígito inválido para a base
                MessageBox.Show(ex.Message, "Erro de Formatação", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (OverflowException)
            {
                MessageBox.Show("O valor excede a capacidade de 64 bits.",
                                "Erro de Limite", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExibirResultados(string hex, long dec, string bin)
        {
            _rawHex = $"0x{hex}";
            _rawDec = dec.ToString(CultureInfo.InvariantCulture); // valor copiado: sem separador de milhar
            _rawBin = bin;

            lblHexValor.Text = _rawHex;
            lblDecimalValor.Text = dec.ToString("N0", CultureInfo.CurrentCulture); // exibição: separador do usuário
            lblBinarioValor.Text = QuebrarBinarioEm32Bits(_calculadora.FormatarBinario(bin));
        }

        /// <summary>
        /// Apresentação: com mais de 32 bits, quebra a linha na fronteira dos 32 bits menos significativos
        /// (8 nibbles), garantindo no máximo 2 linhas de 39 caracteres para um valor de 64 bits.
        /// </summary>
        private static string QuebrarBinarioEm32Bits(string formatado)
        {
            string[] grupos = formatado.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            const int gruposPorLinha = 8;
            if (grupos.Length <= gruposPorLinha)
                return formatado;

            int corte = grupos.Length - gruposPorLinha;
            return string.Join(' ', grupos, 0, corte) + Environment.NewLine +
                   string.Join(' ', grupos, corte, gruposPorLinha);
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

        // Botões com feedback "Copiado!" em andamento: evita reentrância (duplo clique) por botão
        private readonly HashSet<FluentButton> _copiando = new();

        private bool FormularioAtivo => !this.IsDisposed && !this.Disposing && this.IsHandleCreated;

        private async Task CopiarParaClipboard(string valor, FluentButton botao)
        {
            if (string.IsNullOrWhiteSpace(valor) || valor == "-")
                return;

            // Reentrância: se o feedback ainda está na tela, só recopia o valor, sem empilhar outro ciclo
            if (_copiando.Contains(botao))
            {
                TentarCopiar(valor);
                return;
            }

            if (!TentarCopiar(valor))
                return;

            _copiando.Add(botao);
            string textoOriginal = botao.Text;
            try
            {
                botao.Text = "✓ Copiado!";
                botao.CustomAccentColor = ThemeManager.VerdeEscuro;
                botao.IsAccent = true;

                await Task.Delay(1200);
            }
            finally
            {
                _copiando.Remove(botao);

                // O formulário pode ter sido fechado durante o Delay
                if (FormularioAtivo && !botao.IsDisposed)
                {
                    botao.Text = textoOriginal;
                    botao.IsAccent = false;
                    botao.CustomAccentColor = null;
                }
            }
        }

        private static bool TentarCopiar(string valor)
        {
            try
            {
                Clipboard.SetText(valor);
                return true;
            }
            catch (ExternalException)
            {
                // Área de transferência ocupada por outro processo do Windows
                return false;
            }
        }

        private void txtEntrada_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Converter(null); // Base detectada pelo Core (0x → hex, 0b → bin, letras A–F → hex, senão decimal)
                e.SuppressKeyPress = true;
            }
        }
    }
}
