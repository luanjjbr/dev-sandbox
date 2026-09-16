using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DevSandbox.UI.Common
{
    /// <summary>
    /// Chave de alternância (Toggle Switch) no estilo Windows 11 Fluent Design.
    /// Exatamente igual ao controle de On/Off presente no WinUI 3.
    /// </summary>
    public class FluentToggle : Control
    {
        private bool _checked = false;
        public event EventHandler? CheckedChanged;

        public FluentToggle()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            this.BackColor = Color.Transparent;
            this.Size = new Size(42, 22);
            this.Cursor = Cursors.Hand;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    CheckedChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        protected override void OnClick(EventArgs e)
        {
            Checked = !Checked;
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Color corPai = ThemeManager.ObterCorFundoEfetiva(this);
            g.Clear(corPai);

            int raio = Height / 2;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using (GraphicsPath path = ThemeManager.CriarRetanguloArredondado(rect, raio))
            {
                if (_checked)
                {
                    // Ativo: Fundo Ciano #4CC2FF com thumb escuro à direita
                    using (var brush = new SolidBrush(ThemeManager.CorAcento))
                    {
                        g.FillPath(brush, path);
                    }

                    int thumbSize = Height - 8;
                    int thumbX = Width - thumbSize - 4;
                    int thumbY = (Height - thumbSize) / 2;

                    using var thumbBrush = new SolidBrush(Color.Black);
                    g.FillEllipse(thumbBrush, thumbX, thumbY, thumbSize, thumbSize);
                }
                else
                {
                    // Inativo: Fundo escuro com contorno cinza e thumb cinza à esquerda
                    using (var brush = new SolidBrush(ThemeManager.FundoJanela))
                    {
                        g.FillPath(brush, path);
                    }

                    using (var pen = new Pen(Color.FromArgb(140, 140, 140), 1.2f))
                    {
                        g.DrawPath(pen, path);
                    }

                    int thumbSize = Height - 10;
                    int thumbX = 5;
                    int thumbY = (Height - thumbSize) / 2;

                    using var thumbBrush = new SolidBrush(Color.FromArgb(200, 200, 200));
                    g.FillEllipse(thumbBrush, thumbX, thumbY, thumbSize, thumbSize);
                }
            }
        }
    }
}
