using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DevSandbox.UI.Common
{
    /// <summary>
    /// Matriz de 64 bits (4 linhas × 16) num único HWND: MSB [63] no topo à esquerda, LSB [0] embaixo à direita.
    /// Cada célula mostra o índice e o valor; clique (ou Espaço com foco) pede a inversão do bit via
    /// <see cref="BitClicado"/> — a operação em si é do Core (<c>OperacoesBits.InverterBit</c>).
    /// Teclado: setas movem o cursor de célula.
    /// </summary>
    public class BitMatrixControl : ControleNordic
    {
        private const int Linhas = 4;
        private const int Colunas = 16;

        private ulong _valor;
        private int _hover = -1;
        private int _cursor = 63;

        /// <summary>Bit (0–63) clicado pelo usuário.</summary>
        public event EventHandler<int>? BitClicado;

        public BitMatrixControl() : base(selecionavel: true)
        {
            Size = new Size(900, 180);
            Cursor = Cursors.Hand;
            AccessibleRole = AccessibleRole.Table;
            AccessibleName = "Matriz de 64 bits";
        }

        /// <summary>Valor exibido (só apresentação; quem calcula é o Core).</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ulong Valor
        {
            get => _valor;
            set
            {
                if (_valor == value) return;
                _valor = value;
                Invalidate();
            }
        }

        private int LarguraRotulo => Px(84);
        private int Gap => Px(4);
        private int GapLinha => Px(6);

        private Rectangle Celula(int bit)
        {
            int linha = (63 - bit) / Colunas;
            int coluna = (63 - bit) % Colunas;
            int x0 = LarguraRotulo + Px(8);
            int largura = (Width - x0 - Gap * (Colunas - 1)) / Colunas;
            int altura = (Height - GapLinha * (Linhas - 1)) / Linhas;
            return new Rectangle(x0 + coluna * (largura + Gap), linha * (altura + GapLinha), largura, altura);
        }

        private int BitEm(Point p)
        {
            for (int bit = 0; bit < 64; bit++)
            {
                if (Celula(bit).Contains(p)) return bit;
            }
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int h = BitEm(e.Location);
            if (h == _hover) return;
            if (_hover >= 0) Invalidate(Celula(_hover));
            _hover = h;
            if (_hover >= 0) Invalidate(Celula(_hover));
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_hover >= 0) Invalidate(Celula(_hover));
            _hover = -1;
            base.OnMouseLeave(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;
            int bit = BitEm(e.Location);
            if (bit < 0) return;
            Focus();
            _cursor = bit;
            BitClicado?.Invoke(this, bit);
        }

        protected override bool IsInputKey(Keys keyData) =>
            (keyData & Keys.KeyCode) is Keys.Left or Keys.Right or Keys.Up or Keys.Down || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            int novo = e.KeyCode switch
            {
                Keys.Left when _cursor < 63 && (63 - _cursor) % Colunas != 0 => _cursor + 1,
                Keys.Right when _cursor > 0 && (63 - _cursor) % Colunas != Colunas - 1 => _cursor - 1,
                Keys.Up when _cursor + Colunas <= 63 => _cursor + Colunas,
                Keys.Down when _cursor - Colunas >= 0 => _cursor - Colunas,
                _ => _cursor
            };
            if (novo != _cursor)
            {
                _cursor = novo;
                Invalidate();
                e.Handled = true;
            }
            else if (e.KeyCode is Keys.Space or Keys.Enter)
            {
                BitClicado?.Invoke(this, _cursor);
                e.Handled = e.SuppressKeyPress = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            LimparComFundoDoPai(g);
            ThemeManager.PrepararAntialias(g);

            const TextFormatFlags centro = TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;

            for (int linha = 0; linha < Linhas; linha++)
            {
                int msb = 63 - linha * Colunas;
                Rectangle primeira = Celula(msb);
                var areaRotulo = new Rectangle(0, primeira.Y, LarguraRotulo, primeira.Height);
                TextRenderer.DrawText(g, $"BITS {msb}–{msb - 15}", ThemeManager.FonteMonoPequena, areaRotulo, ThemeManager.TextoSecundario,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            }

            for (int bit = 63; bit >= 0; bit--)
            {
                Rectangle r = Celula(bit);
                if (!e.ClipRectangle.IntersectsWith(r)) continue;

                bool ligado = ((_valor >> bit) & 1UL) == 1UL;
                bool hover = bit == _hover;
                Color fundo = ligado ? ThemeManager.AcentoSuave : hover ? ThemeManager.FundoHover : ThemeManager.FundoInput;
                Color borda = ligado || hover ? ThemeManager.Acento : ThemeManager.BordaForte;
                ThemeManager.DesenharCaixa(g, r, Px(ThemeManager.RaioPequeno), fundo, borda);

                int hIndice = ThemeManager.FonteMonoPequena.Height;
                int hValor = ThemeManager.FonteMonoForte.Height;
                int topo = r.Y + (r.Height - hIndice - hValor) / 2;
                TextRenderer.DrawText(g, bit.ToString(System.Globalization.CultureInfo.InvariantCulture), ThemeManager.FonteMonoPequena,
                    new Rectangle(r.X, topo, r.Width, hIndice), ligado ? ThemeManager.Acento : ThemeManager.TextoMudo, centro);
                TextRenderer.DrawText(g, ligado ? "1" : "0", ThemeManager.FonteMonoForte,
                    new Rectangle(r.X, topo + hIndice, r.Width, hValor), ligado ? ThemeManager.Acento : ThemeManager.TextoSecundario, centro);

                if (MostrarFoco && bit == _cursor) ThemeManager.DesenharFoco(g, r, Px(ThemeManager.RaioPequeno));
            }
        }
    }

    /// <summary>
    /// Anel de progresso do Pomodoro (traço de 10 px, ponta arredondada) que esvazia com o tempo, com o tempo e o
    /// rótulo do estado no centro. O valor vem de <c>PomodoroEngine.Progresso</c> (1 → 0).
    /// </summary>
    public class ProgressRingControl : ControleNordic
    {
        private double _progresso = 1.0;
        private string _rotulo = "";
        private PapelTexto _papelCor = PapelTexto.Acento;

        public ProgressRingControl() : base(selecionavel: false)
        {
            Size = new Size(250, 250);
            AccessibleRole = AccessibleRole.ProgressBar;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double Progresso
        {
            get => _progresso;
            set
            {
                double v = double.IsNaN(value) ? 0 : Math.Clamp(value, 0, 1);
                if (Math.Abs(v - _progresso) < 0.0005) return;
                _progresso = v;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Rotulo
        {
            get => _rotulo;
            set
            {
                _rotulo = value ?? "";
                Invalidate();
            }
        }

        /// <summary>Cor do arco e do rótulo (acento no foco, sucesso nas pausas, secundário pausado).</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PapelTexto PapelCor
        {
            get => _papelCor;
            set
            {
                _papelCor = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            LimparComFundoDoPai(g);
            ThemeManager.PrepararAntialias(g);

            float espessura = Px(10);
            float lado = Math.Min(Width, Height) - espessura - 2;
            var r = new RectangleF((Width - lado) / 2f, (Height - lado) / 2f, lado, lado);

            using (var trilho = new Pen(ThemeManager.BordaSutil, espessura))
            {
                g.DrawEllipse(trilho, r);
            }

            if (_progresso > 0)
            {
                using var arco = new Pen(RotuloNordic.CorDoPapel(_papelCor), espessura)
                {
                    StartCap = System.Drawing.Drawing2D.LineCap.Round,
                    EndCap = System.Drawing.Drawing2D.LineCap.Round
                };
                g.DrawArc(arco, r, -90f, (float)(360 * _progresso));
            }

            int hTempo = ThemeManager.FonteRelogio.Height;
            int hRotulo = ThemeManager.FonteRotulo.Height;
            int topo = (Height - hTempo - hRotulo) / 2;
            const TextFormatFlags f = TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
            TextRenderer.DrawText(g, Text, ThemeManager.FonteRelogio, new Rectangle(0, topo, Width, hTempo), ThemeManager.TextoPrincipal, f);
            TextRenderer.DrawText(g, _rotulo.ToUpperInvariant(), ThemeManager.FonteRotulo, new Rectangle(0, topo + hTempo, Width, hRotulo),
                RotuloNordic.CorDoPapel(_papelCor), f);
        }
    }
}
