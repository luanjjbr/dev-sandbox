using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DevSandbox.UI.Common
{
    /// <summary>
    /// Barra de abas do cabeçalho (Início, Supervisório, Bancada, Calculadora, Pomodoro): ícone + texto,
    /// aba ativa em acento com sublinhado de 2 px. Teclado: ←/→ navegam; Ctrl+Tab é tratado pela janela.
    /// </summary>
    public class BarraAbas : ControleNordic
    {
        private readonly List<(string Texto, Icone Icone)> _abas = new();
        private readonly List<Rectangle> _areas = new();
        private int _ativa;
        private int _hover = -1;

        public event EventHandler<int>? AbaSelecionada;

        public BarraAbas() : base(selecionavel: true)
        {
            Height = 40;
            AccessibleRole = AccessibleRole.PageTabList;
            AccessibleName = "Telas";
        }

        public void Adicionar(string texto, Icone icone)
        {
            _abas.Add((texto, icone));
            Invalidate();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Ativa
        {
            get => _ativa;
            set
            {
                if (value < 0 || value >= _abas.Count || value == _ativa) return;
                _ativa = value;
                Invalidate();
                AbaSelecionada?.Invoke(this, value);
            }
        }

        private void Medir(Graphics g)
        {
            _areas.Clear();
            int x = Px(16);
            foreach ((string texto, Icone _) in _abas)
            {
                Size m = TextRenderer.MeasureText(g, texto, ThemeManager.FonteUiForte, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                int largura = Px(16) + Px(16) + Px(8) + m.Width + Px(16);
                _areas.Add(new Rectangle(x, 0, largura, Height));
                x += largura + Px(4);
            }
        }

        private int AbaEm(Point p)
        {
            for (int i = 0; i < _areas.Count; i++)
            {
                if (_areas[i].Contains(p)) return i;
            }
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int h = AbaEm(e.Location);
            Cursor = h >= 0 ? Cursors.Hand : Cursors.Default;
            if (h != _hover)
            {
                _hover = h;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = -1;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int i = AbaEm(e.Location);
            if (i >= 0) Ativa = i;
        }

        protected override bool IsInputKey(Keys keyData) =>
            (keyData & Keys.KeyCode) is Keys.Left or Keys.Right || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Left) { Ativa = Math.Max(0, _ativa - 1); e.Handled = true; }
            else if (e.KeyCode == Keys.Right) { Ativa = Math.Min(_abas.Count - 1, _ativa + 1); e.Handled = true; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(ThemeManager.FundoCabecalho);
            ThemeManager.PrepararAntialias(g);
            Medir(g);

            for (int i = 0; i < _abas.Count; i++)
            {
                Rectangle r = _areas[i];
                bool ativa = i == _ativa;
                if (!ativa && i == _hover)
                {
                    using var b = new SolidBrush(ThemeManager.FundoHover);
                    g.FillRectangle(b, r);
                }

                Color cor = ativa ? ThemeManager.Acento : i == _hover ? ThemeManager.TextoPrincipal : ThemeManager.TextoSecundario;
                int icone = Px(16);
                Icones.Desenhar(g, _abas[i].Icone, new RectangleF(r.X + Px(16), (Height - icone) / 2f, icone, icone), cor);
                TextRenderer.DrawText(g, _abas[i].Texto, ativa ? ThemeManager.FonteUiForte : ThemeManager.FonteUi,
                    new Rectangle(r.X + Px(16) + icone + Px(8), 0, r.Width, Height), cor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

                if (ativa)
                {
                    using var b = new SolidBrush(ThemeManager.Acento);
                    g.FillRectangle(b, r.X, Height - Px(2) - 1, r.Width, Px(2));
                }

                if (ativa && MostrarFoco) ThemeManager.DesenharFoco(g, Rectangle.Inflate(r, 0, -Px(4)), Px(ThemeManager.RaioMedio));
            }

            using var pen = new Pen(ThemeManager.BordaSutil);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            g.DrawLine(pen, 0, Height - 1, Width, Height - 1);
        }
    }
}
