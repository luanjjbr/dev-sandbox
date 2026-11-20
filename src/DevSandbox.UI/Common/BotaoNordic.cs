using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DevSandbox.UI.Common
{
    public enum EstiloBotao
    {
        Primario,
        Secundario,
        Perigo,
        /// <summary>Feedback curto de sucesso (ex.: "Copiado").</summary>
        Sucesso
    }

    /// <summary>
    /// Botão do visual Nordic: primário (cobalto), secundário (cartão + borda forte) ou perigo (rubi),
    /// com ícone opcional desenhado em GDI+ e anel de foco para teclado.
    /// </summary>
    public class BotaoNordic : Button
    {
        private bool _hover;
        private bool _pressionado;
        private EstiloBotao _estilo = EstiloBotao.Secundario;
        private Icone _icone;

        public BotaoNordic()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Font = ThemeManager.FonteBotao;
            Size = new Size(110, 32);
            UseMnemonic = false;
            ThemeManager.TemaMudou += AoMudarTema;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public EstiloBotao Estilo
        {
            get => _estilo;
            set
            {
                _estilo = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Icone Icone
        {
            get => _icone;
            set
            {
                _icone = value;
                Invalidate();
            }
        }

        private void AoMudarTema(object? sender, EventArgs e)
        {
            if (!IsDisposed) Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _pressionado = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            if (mevent.Button == MouseButtons.Left)
            {
                _pressionado = true;
                Invalidate();
            }
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _pressionado = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.Clear(ThemeManager.ObterCorFundoEfetiva(this));
            ThemeManager.PrepararAntialias(g);

            (Color fundo, Color texto, Color? borda) = Cores();
            var area = new Rectangle(0, _pressionado ? 1 : 0, Width, Height - 1);
            ThemeManager.DesenharCaixa(g, area, ThemeManager.Px(this, ThemeManager.RaioMedio), fundo, borda);

            int tamIcone = ThemeManager.Px(this, 14);
            int gap = ThemeManager.Px(this, 6);
            Size medida = TextRenderer.MeasureText(g, Text, Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            bool temTexto = !string.IsNullOrEmpty(Text);
            int larguraTotal = (_icone != Icone.Nenhum ? tamIcone + (temTexto ? gap : 0) : 0) + (temTexto ? medida.Width : 0);
            int x = Math.Max(ThemeManager.Px(this, 6), (Width - larguraTotal) / 2);

            if (_icone != Icone.Nenhum)
            {
                Icones.Desenhar(g, _icone, new RectangleF(x, area.Y + (area.Height - tamIcone) / 2f, tamIcone, tamIcone), texto);
                x += tamIcone + gap;
            }

            if (temTexto)
            {
                var areaTexto = new Rectangle(x, area.Y, Math.Max(0, Width - x - ThemeManager.Px(this, 4)), area.Height);
                TextRenderer.DrawText(g, Text, Font, areaTexto, texto,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            }

            if (Focused && ShowFocusCues && Enabled)
            {
                ThemeManager.DesenharFoco(g, new Rectangle(0, 0, Width, Height), ThemeManager.Px(this, ThemeManager.RaioMedio));
            }
        }

        private (Color Fundo, Color Texto, Color? Borda) Cores()
        {
            if (!Enabled)
            {
                return (ThemeManager.FundoMudo, ThemeManager.TextoMudo, ThemeManager.BordaSutil);
            }

            return _estilo switch
            {
                EstiloBotao.Primario => (_hover ? ThemeManager.AcentoHover : ThemeManager.Acento,
                                         ThemeManager.TextoSobreAcento, ThemeManager.AcentoHover),
                EstiloBotao.Perigo => (_hover ? ThemeManager.PerigoHover : ThemeManager.Perigo,
                                       Color.White, ThemeManager.PerigoHover),
                EstiloBotao.Sucesso => (ThemeManager.Sucesso, Color.White, ThemeManager.Sucesso),
                _ => (_hover ? ThemeManager.FundoHover : ThemeManager.FundoCard, ThemeManager.TextoPrincipal,
                      _hover ? ThemeManager.TextoSecundario : ThemeManager.BordaForte)
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.TemaMudou -= AoMudarTema;
            base.Dispose(disposing);
        }
    }
}
