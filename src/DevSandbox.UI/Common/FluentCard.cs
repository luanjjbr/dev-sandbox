using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DevSandbox.UI.Common
{
    /// <summary>
    /// Cartão/Painel com cantos arredondados e borda sutil no padrão Windows 11 Fluent Design (WinUI 3 Dark).
    /// </summary>
    public class FluentCard : Panel
    {
        private int _cornerRadius = 8;
        private Color _borderColor = ThemeManager.BordaCard;
        private Color _cardColor = ThemeManager.FundoCard;
        private bool _enableHoverEffect = false;
        private bool _isHovered = false;

        public FluentCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            this.BackColor = _cardColor;
            this.Padding = new Padding(16);
            this.TabStop = false; // Container: o foco de teclado vai para os controles internos
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
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                _borderColor = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color CardColor
        {
            get => _cardColor;
            set
            {
                _cardColor = value;
                this.BackColor = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableHoverEffect
        {
            get => _enableHoverEffect;
            set
            {
                _enableHoverEffect = value;
                Invalidate();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            if (_enableHoverEffect)
            {
                _isHovered = true;
                this.BackColor = ThemeManager.FundoCardHover;
                Invalidate();
            }
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_enableHoverEffect)
            {
                _isHovered = false;
                this.BackColor = _cardColor;
                Invalidate();
            }
            base.OnMouseLeave(e);
        }

        // Cartões clicáveis (EnableHoverEffect) destacam a borda quando um controle interno
        // recebe foco pelo teclado, indicando qual cartão está ativo na navegação por Tab.
        protected override void OnEnter(EventArgs e)
        {
            if (_enableHoverEffect) Invalidate();
            base.OnEnter(e);
        }

        protected override void OnLeave(EventArgs e)
        {
            if (_enableHoverEffect) Invalidate();
            base.OnLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Color corPai = ThemeManager.ObterCorFundoEfetiva(this);
            g.Clear(corPai);

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fundo = (_enableHoverEffect && _isHovered) ? ThemeManager.FundoCardHover : _cardColor;

            using (GraphicsPath path = ThemeManager.CriarRetanguloArredondado(rect, _cornerRadius))
            {
                using (var brush = new SolidBrush(fundo))
                {
                    g.FillPath(brush, path);
                }

                bool focoInterno = _enableHoverEffect && ContainsFocus && ShowFocusCues;
                using (var pen = new Pen(focoInterno ? ThemeManager.CorAcento : _borderColor, focoInterno ? 1.5f : 1f))
                {
                    g.DrawPath(pen, path);
                }
            }

            base.OnPaint(e);
        }
    }
}
