using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DevSandbox.UI.Common
{
    /// <summary>
    /// Lista suspensa Nordic (substitui o ComboBox nativo, que não segue o tema): caixa desenhada com chevron e
    /// menu suspenso com as cores do tema. Teclado: setas trocam o item; Alt+↓, F4, Enter ou Espaço abrem a lista.
    /// </summary>
    public class SeletorNordic : ControleNordic
    {
        private readonly List<object> _itens = new();
        private int _indice = -1;
        private bool _hover;
        private Func<object, string> _formatar = o => o.ToString() ?? "";
        private bool _mono;

        public event EventHandler? SelectedIndexChanged;

        public SeletorNordic() : base(selecionavel: true)
        {
            Size = new Size(160, 32);
            Cursor = Cursors.Hand;
            Font = ThemeManager.FonteUi;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<object, string> Formatar
        {
            get => _formatar;
            set
            {
                _formatar = value;
                Invalidate();
            }
        }

        /// <summary>Texto em fonte monoespaçada (fim de linha).</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Mono
        {
            get => _mono;
            set
            {
                _mono = value;
                Font = value ? ThemeManager.FonteMono : ThemeManager.FonteUi;
            }
        }

        public IReadOnlyList<object> Itens => _itens;

        public void DefinirItens(IEnumerable<object> itens)
        {
            _itens.Clear();
            _itens.AddRange(itens);
            _indice = -1;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex
        {
            get => _indice;
            set
            {
                int novo = value >= -1 && value < _itens.Count ? value : -1;
                if (novo == _indice) return;
                _indice = novo;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object? SelectedItem
        {
            get => _indice >= 0 ? _itens[_indice] : null;
            set => SelectedIndex = value is null ? -1 : _itens.IndexOf(value);
        }

        /// <summary>Texto do item selecionado (vazio sem seleção).</summary>
        public string TextoSelecionado => SelectedItem is { } o ? _formatar(o) : "";

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || !Enabled) return;
            Focus();
            AbrirLista();
        }

        protected override bool IsInputKey(Keys keyData) =>
            (keyData & Keys.KeyCode) is Keys.Up or Keys.Down or Keys.Home or Keys.End || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!Enabled || _itens.Count == 0) return;

            if (e.KeyCode == Keys.F4 || (e.Alt && e.KeyCode == Keys.Down) || e.KeyCode is Keys.Enter or Keys.Space)
            {
                AbrirLista();
                e.Handled = e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Down) { SelectedIndex = Math.Min(_itens.Count - 1, _indice + 1); e.Handled = true; }
            else if (e.KeyCode == Keys.Up) { SelectedIndex = Math.Max(0, _indice - 1); e.Handled = true; }
            else if (e.KeyCode == Keys.Home) { SelectedIndex = 0; e.Handled = true; }
            else if (e.KeyCode == Keys.End) { SelectedIndex = _itens.Count - 1; e.Handled = true; }
        }

        /// <summary>Lista aberta no momento (null se fechada).</summary>
        internal ListaSuspensa? ListaAberta { get; private set; }

        private void AbrirLista()
        {
            if (_itens.Count == 0) return;

            var textos = new string[_itens.Count];
            for (int i = 0; i < textos.Length; i++) textos[i] = _formatar(_itens[i]);

            var lista = new ListaSuspensa(textos, _indice, Font, Width);
            var host = new ToolStripControlHost(lista)
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AutoSize = false,
                Size = lista.Size
            };
            var dropdown = new ToolStripDropDown
            {
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                AutoSize = true,
                DropShadowEnabled = true
            };
            dropdown.Items.Add(host);

            lista.Escolhido += (_, indice) =>
            {
                SelectedIndex = indice;
                dropdown.Close(ToolStripDropDownCloseReason.ItemClicked);
                Focus();
            };
            lista.Cancelado += (_, _) =>
            {
                dropdown.Close(ToolStripDropDownCloseReason.Keyboard);
                Focus();
            };

            ListaAberta = lista;
            dropdown.Closed += (_, _) =>
            {
                if (ReferenceEquals(ListaAberta, lista)) ListaAberta = null;
                BeginInvoke(new Action(dropdown.Dispose));
            };
            dropdown.Show(this, new Point(0, Height + Px(2)));
            lista.Focus();
        }

        /// <summary>
        /// Lista desenhada do dropdown: cada linha ocupa a largura inteira (hover em acento suave, item marcado em
        /// acento), borda forte de 1 px e, acima de 10 linhas, rolagem pela roda/teclado com polegar fino.
        /// Teclado: ↑/↓/Home/End movem, Enter/Espaço escolhem, Esc fecha.
        /// </summary>
        internal sealed class ListaSuspensa : ControleNordic
        {
            private const int LinhasVisiveis = 10;
            private readonly string[] _textos;
            private readonly int _marcado;
            private readonly int _alturaLinha;
            private int _hover;
            private int _topo;

            public event EventHandler<int>? Escolhido;
            public event EventHandler? Cancelado;

            public ListaSuspensa(string[] textos, int marcado, Font fonte, int larguraMinima) : base(selecionavel: true)
            {
                _textos = textos;
                _marcado = marcado;
                Font = fonte;
                _alturaLinha = ThemeManager.Px(this, 28);

                int largura = larguraMinima;
                foreach (string t in textos)
                {
                    largura = Math.Max(largura, TextRenderer.MeasureText(t, fonte).Width + ThemeManager.Px(this, 32));
                }
                Size = new Size(largura, Visiveis * _alturaLinha + 2);
                Hover = Math.Max(0, marcado);
            }

            /// <summary>Linha com o destaque de hover/teclado.</summary>
            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public int Hover
            {
                get => _hover;
                set
                {
                    _hover = Math.Clamp(value, 0, _textos.Length - 1);
                    if (_hover < _topo) _topo = _hover;
                    else if (_hover >= _topo + Visiveis) _topo = _hover - Visiveis + 1;
                    Invalidate();
                }
            }

            private int Visiveis => Math.Min(LinhasVisiveis, _textos.Length);

            private int LinhaEm(Point p)
            {
                if (p.Y < 1) return -1;
                int i = _topo + (p.Y - 1) / _alturaLinha;
                return i < _textos.Length ? i : -1;
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                int i = LinhaEm(e.Location);
                if (i >= 0 && i != _hover)
                {
                    _hover = i;
                    Invalidate();
                }
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                int i = LinhaEm(e.Location);
                if (e.Button == MouseButtons.Left && i >= 0) Escolhido?.Invoke(this, i);
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                base.OnMouseWheel(e);
                int max = Math.Max(0, _textos.Length - Visiveis);
                _topo = Math.Clamp(_topo - Math.Sign(e.Delta) * 3, 0, max);
                Invalidate();
            }

            protected override bool IsInputKey(Keys keyData) =>
                (keyData & Keys.KeyCode) is Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.Enter or Keys.Escape
                || base.IsInputKey(keyData);

            protected override void OnKeyDown(KeyEventArgs e)
            {
                base.OnKeyDown(e);
                switch (e.KeyCode)
                {
                    case Keys.Down: Hover = _hover + 1; break;
                    case Keys.Up: Hover = _hover - 1; break;
                    case Keys.Home: Hover = 0; break;
                    case Keys.End: Hover = _textos.Length - 1; break;
                    case Keys.Enter:
                    case Keys.Space:
                        Escolhido?.Invoke(this, _hover);
                        break;
                    case Keys.Escape:
                        Cancelado?.Invoke(this, EventArgs.Empty);
                        break;
                    default:
                        return;
                }
                e.Handled = e.SuppressKeyPress = true;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.Clear(ThemeManager.FundoCard);

                bool rolagem = _textos.Length > Visiveis;
                int larguraTexto = Width - 2 - (rolagem ? Px(8) : 0);

                for (int v = 0; v < Visiveis; v++)
                {
                    int i = _topo + v;
                    var linha = new Rectangle(1, 1 + v * _alturaLinha, Width - 2, _alturaLinha);
                    if (i == _hover)
                    {
                        using var b = new SolidBrush(ThemeManager.AcentoSuave);
                        g.FillRectangle(b, linha);
                    }

                    TextRenderer.DrawText(g, _textos[i], Font,
                        new Rectangle(Px(12), linha.Y, Math.Max(0, larguraTexto - Px(12)), linha.Height),
                        i == _marcado ? ThemeManager.Acento : ThemeManager.TextoPrincipal,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                        TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                }

                if (rolagem)
                {
                    ThemeManager.PrepararAntialias(g);
                    int area = Height - 2;
                    int altura = Math.Max(Px(20), area * Visiveis / _textos.Length);
                    int y = 1 + (area - altura) * _topo / Math.Max(1, _textos.Length - Visiveis);
                    var polegar = new Rectangle(Width - Px(7), y + Px(2), Px(4), Math.Max(Px(8), altura - Px(4)));
                    ThemeManager.DesenharCaixa(g, polegar, Px(2), ThemeManager.BordaForte, null);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                }

                using var pen = new Pen(ThemeManager.BordaForte);
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            LimparComFundoDoPai(g);
            ThemeManager.PrepararAntialias(g);

            int raio = Px(ThemeManager.RaioMedio);
            Color borda = Focused ? ThemeManager.BordaFoco : _hover && Enabled ? ThemeManager.TextoSecundario : ThemeManager.BordaForte;
            ThemeManager.DesenharCaixa(g, ClientRectangle, raio, Focused ? ThemeManager.FundoCard : ThemeManager.FundoInput, borda);

            int chevron = Px(10);
            int margem = Px(10);
            var areaTexto = new Rectangle(margem, 0, Math.Max(0, Width - margem * 2 - chevron - Px(4)), Height);
            TextRenderer.DrawText(g, TextoSelecionado, Font, areaTexto,
                Enabled ? ThemeManager.TextoPrincipal : ThemeManager.TextoMudo,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            float cx = Width - margem - chevron / 2f;
            float cy = Height / 2f;
            using var pen = new Pen(Enabled ? ThemeManager.TextoSecundario : ThemeManager.TextoMudo, 1.6f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            g.DrawLines(pen, new PointF[] { new(cx - chevron / 2f, cy - chevron / 4f), new(cx, cy + chevron / 4f), new(cx + chevron / 2f, cy - chevron / 4f) });

            if (MostrarFoco) ThemeManager.DesenharFoco(g, ClientRectangle, raio);
        }
    }
}
