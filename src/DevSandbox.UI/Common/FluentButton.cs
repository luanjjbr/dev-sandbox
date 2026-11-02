using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DevSandbox.UI.Common
{
    /// <summary>
    /// Botão com estética Windows 11 Fluent Design (WinUI 3 Dark).
    /// Suporta cantos arredondados, modo Accent (Ciano #4CC2FF + texto escuro) e modo Secundário (Cinza #383838 + texto branco).
    /// </summary>
    public class FluentButton : Button
    {
        private bool _isHovered = false;
        private bool _isPressed = false;
        private bool _isAccent = false;
        private int _cornerRadius = 6;
        private Color? _customAccentColor;
        private Color? _customTextColor;

        public FluentButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.Selectable, true);

            this.TabStop = true;
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.Cursor = Cursors.Hand;
            this.Font = ThemeManager.FontePadrao;
            this.Size = new Size(110, 34);
            this.BackColor = Color.Transparent;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsAccent
        {
            get => _isAccent;
            set
            {
                _isAccent = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius
        {
            get => _cornerRadius;
            set
            {
                _cornerRadius = Math.Max(0, value);
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color? CustomAccentColor
        {
            get => _customAccentColor;
            set
            {
                _customAccentColor = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color? CustomTextColor
        {
            get => _customTextColor;
            set
            {
                _customTextColor = value;
                Invalidate();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            _isPressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            if (mevent.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _isPressed = false;
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
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Limpa o fundo com a cor do container pai para eliminar cantos pretos
            Color corPai = ThemeManager.ObterCorFundoEfetiva(this);
            g.Clear(corPai);

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // Determina a cor de fundo e texto
            Color corFundo;
            Color corTexto;
            Color corBorda;

            if (!Enabled)
            {
                corFundo = Color.FromArgb(40, 40, 40);
                corTexto = ThemeManager.TextoDesabilitado;
                corBorda = Color.FromArgb(50, 50, 50);
            }
            else if (_isAccent)
            {
                Color baseColor = _customAccentColor ?? ThemeManager.CorAcento;
                if (_isPressed)
                    corFundo = _customAccentColor.HasValue ? EscurecerCor(baseColor, 0.2f) : ThemeManager.CorAcentoPress;
                else if (_isHovered)
                    corFundo = _customAccentColor.HasValue ? ClarearCor(baseColor, 0.1f) : ThemeManager.CorAcentoHover;
                else
                    corFundo = baseColor;

                corTexto = _customTextColor ?? (_customAccentColor.HasValue ? Color.White : ThemeManager.CorAcentoTexto);
                corBorda = Color.Transparent;
            }
            else
            {
                if (_isPressed)
                    corFundo = ThemeManager.BotaoSecundarioPress;
                else if (_isHovered)
                    corFundo = ThemeManager.BotaoSecundarioHover;
                else
                    corFundo = ThemeManager.BotaoSecundarioFundo;

                corTexto = ThemeManager.TextoPrincipal;
                corBorda = Color.FromArgb(70, 70, 70);
            }

            using (GraphicsPath path = ThemeManager.CriarRetanguloArredondado(rect, _cornerRadius))
            {
                using (var brush = new SolidBrush(corFundo))
                {
                    g.FillPath(brush, path);
                }

                if (corBorda != Color.Transparent && !_isAccent)
                {
                    using var pen = new Pen(corBorda, 1f);
                    g.DrawPath(pen, path);
                }
            }

            // Desenho do Texto
            TextFormatFlags flags = TextFormatFlags.HorizontalCenter |
                                    TextFormatFlags.VerticalCenter |
                                    TextFormatFlags.SingleLine |
                                    TextFormatFlags.WordEllipsis;

            TextRenderer.DrawText(g, Text, Font, rect, corTexto, flags);

            // Indicador de foco: só aparece na navegação por teclado (ShowFocusCues)
            if (Focused && ShowFocusCues && Enabled)
            {
                ThemeManager.DesenharFoco(g, rect, _cornerRadius);
            }
        }

        private static Color ClarearCor(Color c, float fator)
        {
            return Color.FromArgb(
                c.A,
                Math.Min(255, (int)(c.R + (255 - c.R) * fator)),
                Math.Min(255, (int)(c.G + (255 - c.G) * fator)),
                Math.Min(255, (int)(c.B + (255 - c.B) * fator))
            );
        }

        private static Color EscurecerCor(Color c, float fator)
        {
            return Color.FromArgb(
                c.A,
                Math.Max(0, (int)(c.R * (1 - fator))),
                Math.Max(0, (int)(c.G * (1 - fator))),
                Math.Max(0, (int)(c.B * (1 - fator)))
            );
        }
    }
}
