using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DevSandbox.UI.Common
{
    public enum TipoSelo
    {
        Neutro,
        Sucesso,
        Aviso,
        Perigo,
        Acento
    }

    /// <summary>Desenho dos selos (badges em pílula) e das cores de cada tipo.</summary>
    public static class Selos
    {
        public static (Color Fundo, Color Texto, Color Borda) Cores(TipoSelo tipo) => tipo switch
        {
            TipoSelo.Sucesso => (ThemeManager.SucessoFundo, ThemeManager.Sucesso, ThemeManager.Sucesso),
            TipoSelo.Aviso => (ThemeManager.AvisoFundo, ThemeManager.Aviso, ThemeManager.Aviso),
            TipoSelo.Perigo => (ThemeManager.PerigoFundo, ThemeManager.Perigo, ThemeManager.Perigo),
            TipoSelo.Acento => (ThemeManager.AcentoSuave, ThemeManager.Acento, ThemeManager.BordaForte),
            _ => (ThemeManager.FundoInput, ThemeManager.TextoSecundario, ThemeManager.BordaSutil)
        };

        public static Size Medir(Graphics g, string texto, Control dono)
        {
            Size m = TextRenderer.MeasureText(g, texto, ThemeManager.FonteRotulo, Size.Empty,
                                              TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            return new Size(m.Width + ThemeManager.Px(dono, 16), m.Height + ThemeManager.Px(dono, 4));
        }

        /// <summary>Desenha o selo com o canto direito em <paramref name="direita"/>, centrado em <paramref name="centroY"/>.</summary>
        public static Rectangle DesenharAlinhadoADireita(Graphics g, string texto, TipoSelo tipo, int direita, int centroY, Control dono)
        {
            Size tam = Medir(g, texto, dono);
            var r = new Rectangle(direita - tam.Width, centroY - tam.Height / 2, tam.Width, tam.Height);
            Desenhar(g, texto, tipo, r);
            return r;
        }

        public static void Desenhar(Graphics g, string texto, TipoSelo tipo, Rectangle r)
        {
            (Color fundo, Color cor, Color borda) = Cores(tipo);
            ThemeManager.DesenharCaixa(g, r, r.Height / 2, fundo, borda);
            TextRenderer.DrawText(g, texto, ThemeManager.FonteRotulo, r, cor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }
    }

    /// <summary>Selo isolado (pílula com texto curto).</summary>
    public class SeloNordic : ControleNordic
    {
        private TipoSelo _tipo;

        public SeloNordic() : base(selecionavel: false)
        {
            Height = 22;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TipoSelo Tipo
        {
            get => _tipo;
            set
            {
                _tipo = value;
                Invalidate();
            }
        }

        /// <summary>Largura preferida para o texto atual.</summary>
        public int LarguraPreferida()
        {
            using Graphics g = CreateGraphics();
            return Selos.Medir(g, Text, this).Width + 2;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            LimparComFundoDoPai(e.Graphics);
            ThemeManager.PrepararAntialias(e.Graphics);
            if (string.IsNullOrEmpty(Text)) return;
            Selos.DesenharAlinhadoADireita(e.Graphics, Text, _tipo, Width - 1, Height / 2, this);
        }
    }

    /// <summary>
    /// Cartão Nordic: superfície branca/slate, borda sutil de 1 px, raio 6 e, opcionalmente, cabeçalho com
    /// título, selo à direita e linha divisória (o conteúdo começa abaixo dele pelo Padding).
    /// </summary>
    public class CardNordic : Panel
    {
        private string _titulo = "";
        private string _selo = "";
        private TipoSelo _tipoSelo;
        private int _alturaCabecalho = 36;
        private bool _clicavel;
        private bool _hover;
        private Padding _paddingConteudo = new(16);

        public CardNordic()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            TabStop = false;
            BackColor = ThemeManager.FundoCard;
            AtualizarPadding();
            ThemeManager.TemaMudou += AoMudarTema;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Titulo
        {
            get => _titulo;
            set
            {
                _titulo = value ?? "";
                AtualizarPadding();
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Selo
        {
            get => _selo;
            set
            {
                _selo = value ?? "";
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TipoSelo TipoSelo
        {
            get => _tipoSelo;
            set
            {
                _tipoSelo = value;
                Invalidate();
            }
        }

        /// <summary>Ícone opcional antes do título.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Icone IconeTitulo { get; set; }

        /// <summary>Dica do selo (ex.: "Relés monitoram o circuito; alteração somente via firmware").</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? DicaSelo { get; set; }

        /// <summary>Padding do conteúdo, sem contar o cabeçalho.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Padding PaddingConteudo
        {
            get => _paddingConteudo;
            set
            {
                _paddingConteudo = value;
                AtualizarPadding();
            }
        }

        /// <summary>Cartão clicável (aba Início): hover destaca a borda em acento.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Clicavel
        {
            get => _clicavel;
            set
            {
                _clicavel = value;
                Cursor = value ? Cursors.Hand : Cursors.Default;
            }
        }

        private int AlturaCabecalho => string.IsNullOrEmpty(_titulo) ? 0 : ThemeManager.Px(this, _alturaCabecalho);

        private void AtualizarPadding()
        {
            Padding = new Padding(_paddingConteudo.Left, _paddingConteudo.Top + AlturaCabecalho,
                                  _paddingConteudo.Right, _paddingConteudo.Bottom);
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            AtualizarPadding();
        }

        private void AoMudarTema(object? sender, EventArgs e)
        {
            if (IsDisposed) return;
            BackColor = ThemeManager.FundoCard;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            if (_clicavel)
            {
                _hover = true;
                Invalidate();
            }
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            // O mouse pode ter saído para um filho: só apaga se saiu do cartão de verdade
            if (_clicavel && !ClientRectangle.Contains(PointToClient(MousePosition)))
            {
                _hover = false;
                Invalidate();
            }
            base.OnMouseLeave(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Tudo é pintado em OnPaint
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(ThemeManager.ObterCorFundoEfetiva(this));
            ThemeManager.PrepararAntialias(g);

            int raio = ThemeManager.Px(this, ThemeManager.RaioMedio);
            Color borda = _clicavel && (_hover || (ContainsFocus && ShowFocusCues)) ? ThemeManager.Acento : ThemeManager.BordaSutil;
            ThemeManager.DesenharCaixa(g, ClientRectangle, raio, ThemeManager.FundoCard, borda);

            int h = AlturaCabecalho;
            if (h > 0)
            {
                int esq = ThemeManager.Px(this, 16);
                int dir = Width - ThemeManager.Px(this, 16);
                int centro = ThemeManager.Px(this, 8) + (h - ThemeManager.Px(this, 8)) / 2;
                int larguraSelo = 0;
                if (!string.IsNullOrEmpty(_selo))
                {
                    Rectangle r = Selos.DesenharAlinhadoADireita(g, _selo, _tipoSelo, dir, centro, this);
                    larguraSelo = r.Width + ThemeManager.Px(this, 8);
                }

                int xTitulo = esq;
                if (IconeTitulo != Icone.Nenhum)
                {
                    int tam = ThemeManager.Px(this, 16);
                    Icones.Desenhar(g, IconeTitulo, new RectangleF(esq, centro - tam / 2f, tam, tam), ThemeManager.Acento);
                    xTitulo += tam + ThemeManager.Px(this, 8);
                }

                TextRenderer.DrawText(g, _titulo, ThemeManager.FonteTituloCard,
                    new Rectangle(xTitulo, ThemeManager.Px(this, 8), Math.Max(0, dir - xTitulo - larguraSelo), h - ThemeManager.Px(this, 8)),
                    ThemeManager.TextoPrincipal,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                using var pen = new Pen(ThemeManager.BordaSutil);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                g.DrawLine(pen, esq, h, dir, h);
            }

            base.OnPaint(e);
        }

        protected override void OnEnter(EventArgs e)
        {
            if (_clicavel) Invalidate();
            base.OnEnter(e);
        }

        protected override void OnLeave(EventArgs e)
        {
            if (_clicavel) Invalidate();
            base.OnLeave(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.TemaMudou -= AoMudarTema;
            base.Dispose(disposing);
        }
    }
}
