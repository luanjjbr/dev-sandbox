using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DevSandbox.UI.Common
{
    /// <summary>
    /// Caixa de texto no estilo Windows 11 Fluent Design (WinUI 3 Dark).
    /// Apresenta fundo escuro profundo, cantos arredondados e linha inferior de destaque em ciano (#4CC2FF) quando focada.
    /// </summary>
    public class FluentTextBox : UserControl
    {
        private readonly TextBox _innerBox;
        private bool _isFocused;
        private int _cornerRadius = 5;
        private string _placeholder = "";

        public event KeyEventHandler? InnerKeyDown;
        public event EventHandler? InnerTextChanged;

        public FluentTextBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            this.BackColor = Color.Transparent;
            this.Size = new Size(250, 36);
            this.Padding = new Padding(10, 8, 10, 8);

            _innerBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = ThemeManager.FundoInput,
                ForeColor = ThemeManager.TextoPrincipal,
                Font = ThemeManager.FonteEntrada,
                TabStop = true,
                Dock = DockStyle.Fill
            };

            _innerBox.GotFocus += (s, e) =>
            {
                _isFocused = true;
                Invalidate();
            };

            _innerBox.LostFocus += (s, e) =>
            {
                _isFocused = false;
                Invalidate();
            };

            _innerBox.KeyDown += (s, e) => InnerKeyDown?.Invoke(this, e);
            _innerBox.TextChanged += (s, e) =>
            {
                InnerTextChanged?.Invoke(this, e);
                Invalidate();
            };

            this.Controls.Add(_innerBox);
            this.TabStop = true; // O Tab entra no container e o foco é repassado à caixa interna
            this.Cursor = Cursors.IBeam;
            this.Click += (s, e) => _innerBox.Focus();
        }

        [AllowNull]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override string Text
        {
            get => _innerBox.Text;
            set
            {
                _innerBox.Text = value ?? "";
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string PlaceholderText
        {
            get => _placeholder;
            set
            {
                _placeholder = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public HorizontalAlignment TextAlign
        {
            get => _innerBox.TextAlign;
            set => _innerBox.TextAlign = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ReadOnly
        {
            get => _innerBox.ReadOnly;
            set
            {
                _innerBox.ReadOnly = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Font InnerFont
        {
            get => _innerBox.Font;
            set
            {
                _innerBox.Font = value;
                Invalidate();
            }
        }

        // Tab/Shift+Tab ou clique no container: o foco vai sempre para a caixa interna
        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            if (!_innerBox.Focused && _innerBox.CanFocus)
            {
                _innerBox.Focus();
            }
        }

        public void SelectAll() => _innerBox.SelectAll();
        public void Clear() => _innerBox.Clear();
        public new bool Focus() => _innerBox.Focus();

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Color corPai = ThemeManager.ObterCorFundoEfetiva(this);
            g.Clear(corPai);

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // Preenchimento do container
            using (GraphicsPath path = ThemeManager.CriarRetanguloArredondado(rect, _cornerRadius))
            {
                using (var brush = new SolidBrush(ThemeManager.FundoInput))
                {
                    g.FillPath(brush, path);
                }

                Color bordaGeral = _isFocused ? Color.FromArgb(70, 70, 70) : ThemeManager.BordaInput;
                using (var pen = new Pen(bordaGeral, 1f))
                {
                    g.DrawPath(pen, path);
                }
            }

            // Destaque inferior (Assinatura visual do Windows 11 Fluent)
            if (_isFocused)
            {
                using var penAccent = new Pen(ThemeManager.CorAcento, 2.5f);
                int y = Height - 2;
                g.DrawLine(penAccent, _cornerRadius, y, Width - _cornerRadius, y);
            }
            else
            {
                using var penSubtle = new Pen(Color.FromArgb(90, 90, 90), 1f);
                int y = Height - 1;
                g.DrawLine(penSubtle, _cornerRadius, y, Width - _cornerRadius, y);
            }

            // Placeholder quando vazio e sem foco
            if (string.IsNullOrEmpty(_innerBox.Text) && !string.IsNullOrEmpty(_placeholder) && !_isFocused)
            {
                TextRenderer.DrawText(g, _placeholder, _innerBox.Font,
                    new Rectangle(Padding.Left, Padding.Top, Width - Padding.Horizontal, Height - Padding.Vertical),
                    ThemeManager.TextoDesabilitado,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }

            base.OnPaint(e);
        }
    }
}
