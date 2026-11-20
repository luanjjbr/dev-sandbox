using System;
using System.Drawing;
using System.Windows.Forms;
using DevSandbox.UI.Common;

namespace DevSandbox.UI.Telas
{
    /// <summary>
    /// Base das telas (abas) da janela principal: fundo do app no tema atual, margem de 24 px e rolagem vertical
    /// quando o conteúdo não cabe. Cada tela monta o próprio layout em código.
    /// </summary>
    public class TelaBase : UserControl
    {
        public TelaBase()
        {
            AutoScaleMode = AutoScaleMode.Inherit;
            DoubleBuffered = true;
            AutoScroll = true;
            Padding = new Padding(24);
            BackColor = ThemeManager.FundoApp;
            Font = ThemeManager.FonteUi;
            ThemeManager.TemaMudou += AoMudarTema;
        }

        private void AoMudarTema(object? sender, EventArgs e)
        {
            if (IsDisposed) return;
            BackColor = ThemeManager.FundoApp;
            Invalidate(true);
        }

        /// <summary>Chamado quando a aba fica visível (ex.: foco no campo principal).</summary>
        public virtual void AoAtivar()
        {
        }

        protected int Px(int px) => ThemeManager.Px(this, px);

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.TemaMudou -= AoMudarTema;
            base.Dispose(disposing);
        }

        // ------------------------------------------------------------------ Fábrica de controles

        protected static RotuloNordic Rotulo(string texto, PapelTexto papel = PapelTexto.Principal, Font? fonte = null, bool autoSize = true) =>
            new()
            {
                Text = texto,
                Papel = papel,
                Font = fonte ?? ThemeManager.FonteUi,
                AutoSize = autoSize,
                Margin = Padding.Empty
            };

        /// <summary>Rótulo de campo (caixa alta, 11,5 px semibold, secundário) como o form-label do protótipo.</summary>
        protected static RotuloNordic RotuloCampo(string texto) =>
            Rotulo(texto.ToUpperInvariant(), PapelTexto.Secundario, ThemeManager.FonteRotulo);

        protected static BotaoNordic Botao(string texto, EstiloBotao estilo, Icone icone = Icone.Nenhum, EventHandler? clique = null)
        {
            var b = new BotaoNordic { Text = texto, Estilo = estilo, Icone = icone, Margin = Padding.Empty };
            if (clique is not null) b.Click += clique;
            return b;
        }

        /// <summary>TableLayoutPanel transparente com colunas/linhas em percentual ou absolutas.</summary>
        protected static TableLayoutPanel Grade(int colunas, int linhas)
        {
            var t = new TableLayoutPanel
            {
                ColumnCount = colunas,
                RowCount = linhas,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            return t;
        }

        protected static Panel PainelTransparente() => new() { BackColor = Color.Transparent, Margin = Padding.Empty };
    }
}
