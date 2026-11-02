using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DevSandbox.UI.Common
{
    /// <summary>
    /// Indicador LED com texto (painel de relés da Bancada v2, seção 5.3 de docs/design/bancada-v2.md):
    /// círculo de 9 px antialiased; desligado em cinza, ligado em verde com anel claro e, em
    /// <see cref="Perigo"/>, ligado em vermelho (relé de falha/curto).
    /// </summary>
    public class LedIndicator : Control
    {
        private const int DiametroLed = 9;
        private const int EspacoTexto = 6;

        private static readonly Color FundoDesligado = Color.FromArgb(56, 56, 56);   // #383838
        private static readonly Color BordaDesligado = Color.FromArgb(72, 72, 72);   // #484848
        private static readonly Color AnelVerde = Color.FromArgb(74, 222, 128);      // #4ADE80
        private static readonly Color AnelVermelho = Color.FromArgb(248, 113, 113);  // #F87171

        private readonly Font _fonteNormal = new("Segoe UI", 8F, FontStyle.Regular);
        private readonly Font _fonteLigado = new("Segoe UI", 8F, FontStyle.Bold);
        private bool _ligado;
        private bool _perigo;

        public LedIndicator()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
        }

        /// <summary>Relé acionado (confirmado pelo firmware).</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Ligado
        {
            get => _ligado;
            set
            {
                if (_ligado == value) return;
                _ligado = value;
                Invalidate();
            }
        }

        /// <summary>Quando ligado, acende em vermelho (ex.: relé D31 de falha/curto).</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Perigo
        {
            get => _perigo;
            set
            {
                _perigo = value;
                Invalidate();
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            Invalidate();
            base.OnTextChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(ThemeManager.ObterCorFundoEfetiva(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            int y = (Height - DiametroLed) / 2;
            var circulo = new RectangleF(2, y, DiametroLed, DiametroLed);

            Color preenchimento, borda, texto;
            float espessura;
            if (_ligado)
            {
                preenchimento = _perigo ? ThemeManager.VermelhoPerigo : ThemeManager.VerdeTerminal;
                borda = _perigo ? AnelVermelho : AnelVerde;
                texto = _perigo ? ThemeManager.VermelhoPerigo : ThemeManager.TextoPrincipal;
                espessura = 1.5f;
            }
            else
            {
                preenchimento = FundoDesligado;
                borda = BordaDesligado;
                texto = ThemeManager.TextoDesabilitado;
                espessura = 1f;
            }

            using (var brush = new SolidBrush(preenchimento))
            {
                g.FillEllipse(brush, circulo);
            }
            using (var pen = new Pen(borda, espessura))
            {
                g.DrawEllipse(pen, circulo);
            }

            var areaTexto = new Rectangle(2 + DiametroLed + EspacoTexto, 0, Math.Max(0, Width - DiametroLed - EspacoTexto - 2), Height);
            TextRenderer.DrawText(g, Text, _ligado ? _fonteLigado : _fonteNormal, areaTexto, texto,
                                  TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix |
                                  TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fonteNormal.Dispose();
                _fonteLigado.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
