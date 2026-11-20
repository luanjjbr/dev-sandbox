using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DevSandbox.UI.Common
{
    public enum EstadoLed
    {
        Apagado,
        Verde,
        Ambar,
        Vermelho
    }

    /// <summary>Desenho do LED do protótipo (ponto com anel e brilho quando aceso).</summary>
    public static class Leds
    {
        public static Color Cor(EstadoLed estado) => estado switch
        {
            EstadoLed.Verde => ThemeManager.Sucesso,
            EstadoLed.Ambar => ThemeManager.Aviso,
            EstadoLed.Vermelho => ThemeManager.Perigo,
            _ => ThemeManager.BordaForte
        };

        public static void Desenhar(Graphics g, RectangleF r, EstadoLed estado, Color fundo)
        {
            Color cor = Cor(estado);
            if (estado != EstadoLed.Apagado)
            {
                // Brilho (box-shadow 0 0 6px)
                for (int i = 3; i >= 1; i--)
                {
                    RectangleF halo = RectangleF.Inflate(r, i * 1.3f, i * 1.3f);
                    using var b = new SolidBrush(Color.FromArgb(28, cor));
                    g.FillEllipse(b, halo);
                }
            }

            using (var b = new SolidBrush(cor))
            {
                g.FillEllipse(b, r);
            }
            using (var anelInterno = new Pen(fundo, 1.5f))
            {
                g.DrawEllipse(anelInterno, RectangleF.Inflate(r, -0.75f, -0.75f));
            }
            using var anelExterno = new Pen(estado == EstadoLed.Apagado ? ThemeManager.BordaSutil : cor, 1f);
            g.DrawEllipse(anelExterno, RectangleF.Inflate(r, 0.5f, 0.5f));
        }
    }

    /// <summary>LED isolado (conexão, terminal).</summary>
    public class LedNordic : ControleNordic
    {
        private EstadoLed _estado;

        public LedNordic() : base(selecionavel: false)
        {
            Size = new Size(14, 14);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public EstadoLed Estado
        {
            get => _estado;
            set
            {
                if (_estado == value) return;
                _estado = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Color fundo = ThemeManager.ObterCorFundoEfetiva(this);
            e.Graphics.Clear(fundo);
            ThemeManager.PrepararAntialias(e.Graphics);
            float d = Px(10);
            Leds.Desenhar(e.Graphics, new RectangleF((Width - d) / 2f, (Height - d) / 2f, d, d), _estado, fundo);
        }
    }

    /// <summary>Chave liga/desliga (cápsula 34×18) com rótulo à direita: "Acompanhar o final".</summary>
    public class ChaveNordic : ControleNordic
    {
        private bool _ligada;
        private Color? _corTexto;

        public event EventHandler? CheckedChanged;

        public ChaveNordic() : base(selecionavel: true)
        {
            Size = new Size(170, 22);
            Cursor = Cursors.Hand;
            Font = ThemeManager.FontePequena;
            AccessibleRole = AccessibleRole.CheckButton;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Checked
        {
            get => _ligada;
            set
            {
                if (_ligada == value) return;
                _ligada = value;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Cor do rótulo (no cabeçalho escuro do terminal); null = texto secundário do tema.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color? CorTexto
        {
            get => _corTexto;
            set
            {
                _corTexto = value;
                Invalidate();
            }
        }

        protected override void OnClick(EventArgs e)
        {
            Focus();
            Checked = !Checked;
            base.OnClick(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space && Enabled)
            {
                Checked = !Checked;
                e.Handled = e.SuppressKeyPress = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Color fundoPai = ThemeManager.ObterCorFundoEfetiva(this);
            g.Clear(fundoPai);
            ThemeManager.PrepararAntialias(g);

            int w = Px(32), h = Px(18);
            var trilho = new Rectangle(Px(1), (Height - h) / 2, w, h);
            Color corTrilho = _ligada ? ThemeManager.Acento : ThemeManager.Misturar(ThemeManager.TermApagado, fundoPai, 0.6f);
            ThemeManager.DesenharCaixa(g, trilho, h / 2, corTrilho, null);

            int d = h - Px(6);
            int x = _ligada ? trilho.Right - d - Px(3) : trilho.X + Px(3);
            using (var b = new SolidBrush(Color.White))
            {
                g.FillEllipse(b, x, trilho.Y + Px(3), d, d);
            }

            var areaTexto = new Rectangle(trilho.Right + Px(6), 0, Math.Max(0, Width - trilho.Right - Px(6)), Height);
            TextRenderer.DrawText(g, Text, Font, areaTexto, _corTexto ?? ThemeManager.TextoSecundario,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

            if (MostrarFoco) ThemeManager.DesenharFoco(g, Rectangle.Inflate(trilho, Px(2), Px(2)), h / 2 + Px(2));
        }
    }

    /// <summary>
    /// Cartão de opção da Bancada (estado CP ou cabo PP): título, subtítulo e indicador de rádio.
    /// Selecionado = seleção local (acento). <see cref="ConfirmadoFirmware"/> marca o que a placa confirmou.
    /// Teclado: ↑/↓ só movem o foco entre os cartões irmãos; Espaço/Enter escolhem (no já marcado = reenviar).
    /// </summary>
    public class CartaoEstado : ControleNordic
    {
        private bool _selecionado;
        private bool _confirmado;
        private bool _hover;
        private bool _perigo;
        private string _subtitulo = "";

        public event EventHandler<SegmentoEventArgs>? Escolhido;

        public CartaoEstado() : base(selecionavel: true)
        {
            Size = new Size(260, 52);
            Cursor = Cursors.Hand;
            AccessibleRole = AccessibleRole.RadioButton;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Subtitulo
        {
            get => _subtitulo;
            set
            {
                _subtitulo = value ?? "";
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Selecionado
        {
            get => _selecionado;
            set
            {
                if (_selecionado == value) return;
                _selecionado = value;
                AccessibleDescription = value ? "selecionado" : null;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ConfirmadoFirmware
        {
            get => _confirmado;
            set
            {
                if (_confirmado == value) return;
                _confirmado = value;
                Invalidate();
            }
        }

        /// <summary>Opção de falha (estado E, PP6): título em vermelho quando não selecionada.</summary>
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

        /// <summary>Índice da opção dentro do grupo (informado pela tela).</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Indice { get; set; }

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

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left || !Enabled) return;
            Focus();
            Escolhido?.Invoke(this, new SegmentoEventArgs(Indice, _selecionado));
        }

        protected override bool IsInputKey(Keys keyData) =>
            (keyData & Keys.KeyCode) is Keys.Up or Keys.Down || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!Enabled) return;

            if (e.KeyCode is Keys.Space or Keys.Enter)
            {
                Escolhido?.Invoke(this, new SegmentoEventArgs(Indice, _selecionado));
                e.Handled = e.SuppressKeyPress = true;
            }
            else if (e.KeyCode is Keys.Up or Keys.Down && Parent is not null)
            {
                // A seta só move o foco: escolher um cartão manda comando para o hardware (ex.: estado E, PP6),
                // então o envio fica no Espaço/Enter/clique
                CartaoEstado? vizinho = null;
                foreach (Control c in Parent.Controls)
                {
                    if (c is not CartaoEstado ce || !ce.Enabled) continue;
                    if (e.KeyCode == Keys.Down && ce.Indice == Indice + 1) vizinho = ce;
                    if (e.KeyCode == Keys.Up && ce.Indice == Indice - 1) vizinho = ce;
                }
                vizinho?.Focus();
                e.Handled = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            LimparComFundoDoPai(g);
            ThemeManager.PrepararAntialias(g);

            int raio = Px(ThemeManager.RaioMedio);
            Color fundo = _selecionado ? ThemeManager.AcentoSuave : _hover ? ThemeManager.FundoHover : ThemeManager.FundoCard;
            Color borda = _selecionado ? ThemeManager.Acento : _hover ? ThemeManager.BordaForte : ThemeManager.BordaSutil;
            ThemeManager.DesenharCaixa(g, ClientRectangle, raio, fundo, borda, _selecionado ? 2f : 1f);

            int esq = Px(14);
            int dir = Width - Px(14);
            int indicador = Px(14);

            // Marca do firmware: pílula "FW ✓" à esquerda do indicador
            int larguraMarca = 0;
            if (_confirmado)
            {
                Rectangle m = Selos.DesenharAlinhadoADireita(g, "FW ✓", TipoSelo.Sucesso, dir - indicador - Px(8), Height / 2, this);
                larguraMarca = m.Width + Px(8);
            }

            int larguraTexto = Math.Max(0, dir - esq - indicador - Px(8) - larguraMarca);
            Color corTitulo = _selecionado ? ThemeManager.Acento : _perigo ? ThemeManager.Perigo : ThemeManager.TextoPrincipal;
            if (!Enabled) corTitulo = ThemeManager.TextoMudo;

            int alturaTitulo = ThemeManager.FonteUiForte.Height;
            int alturaSub = ThemeManager.FontePequena.Height;
            int topo = (Height - alturaTitulo - alturaSub) / 2;
            TextRenderer.DrawText(g, Text, ThemeManager.FonteUiForte, new Rectangle(esq, topo, larguraTexto, alturaTitulo), corTitulo,
                TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, _subtitulo, ThemeManager.FontePequena, new Rectangle(esq, topo + alturaTitulo, larguraTexto, alturaSub),
                ThemeManager.TextoSecundario,
                TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);

            // Indicador de rádio
            var ind = new RectangleF(dir - indicador, (Height - indicador) / 2f, indicador, indicador);
            if (_selecionado)
            {
                using var b = new SolidBrush(ThemeManager.Acento);
                g.FillEllipse(b, ind);
                using var anel = new Pen(ThemeManager.FundoCard, 2f);
                g.DrawEllipse(anel, RectangleF.Inflate(ind, -2.5f, -2.5f));
            }
            else
            {
                using var b = new SolidBrush(ThemeManager.FundoCard);
                g.FillEllipse(b, ind);
                using var p = new Pen(ThemeManager.BordaForte, 2f);
                g.DrawEllipse(p, RectangleF.Inflate(ind, -1f, -1f));
            }

            if (MostrarFoco) ThemeManager.DesenharFoco(g, ClientRectangle, raio);
        }
    }

    /// <summary>Bloco de relé (somente leitura): pino, nome e LED (verde; vermelho no relé de falha).</summary>
    public class BlocoRele : ControleNordic
    {
        private bool _ligado;

        public BlocoRele() : base(selecionavel: false)
        {
            Size = new Size(120, 46);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Pino { get; set; } = "";

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Perigo { get; set; }

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

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            LimparComFundoDoPai(g);
            ThemeManager.PrepararAntialias(g);
            ThemeManager.DesenharCaixa(g, ClientRectangle, Px(ThemeManager.RaioMedio), ThemeManager.FundoCard, ThemeManager.BordaSutil);

            int esq = Px(12);
            int led = Px(12);
            int largura = Math.Max(0, Width - esq * 2 - led - Px(6));
            int hPino = ThemeManager.FonteMonoPequena.Height;
            int hNome = ThemeManager.FonteRotulo.Height;
            int topo = (Height - hPino - hNome) / 2;

            TextRenderer.DrawText(g, Pino, ThemeManager.FonteMonoPequena, new Rectangle(esq, topo, largura, hPino), ThemeManager.TextoSecundario,
                TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, Text, ThemeManager.FonteRotulo, new Rectangle(esq, topo + hPino, largura, hNome),
                _ligado && Perigo ? ThemeManager.Perigo : ThemeManager.TextoPrincipal,
                TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

            EstadoLed estado = !_ligado ? EstadoLed.Apagado : Perigo ? EstadoLed.Vermelho : EstadoLed.Verde;
            Leds.Desenhar(g, new RectangleF(Width - esq - led, (Height - led) / 2f, led, led), estado, ThemeManager.FundoCard);
        }
    }

    /// <summary>Leitura de telemetria do topo da Bancada: rótulo (+ selo NOMINAL opcional) e valor mono.</summary>
    public class LeituraTelemetria : ControleNordic
    {
        private string _valor = "—";
        private PapelTexto _papelValor = PapelTexto.Principal;

        public LeituraTelemetria() : base(selecionavel: false)
        {
            Size = new Size(150, 50);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Nominal { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Valor
        {
            get => _valor;
            set
            {
                _valor = value ?? "";
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PapelTexto PapelValor
        {
            get => _papelValor;
            set
            {
                _papelValor = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            LimparComFundoDoPai(g);
            ThemeManager.PrepararAntialias(g);
            ThemeManager.DesenharCaixa(g, ClientRectangle, Px(ThemeManager.RaioMedio), ThemeManager.FundoInput, ThemeManager.BordaSutil);

            int dir = Width - Px(12);
            int hRotulo = ThemeManager.FonteRotulo.Height;
            int hValor = ThemeManager.FonteMonoValor.Height;
            int topo = (Height - hRotulo - hValor) / 2;

            int x = dir;
            if (Nominal)
            {
                Size tam = TextRenderer.MeasureText(g, "NOMINAL", ThemeManager.FonteMicro, Size.Empty, TextFormatFlags.NoPadding);
                var r = new Rectangle(dir - tam.Width - Px(8), topo + (hRotulo - tam.Height - Px(2)) / 2, tam.Width + Px(8), tam.Height + Px(2));
                ThemeManager.DesenharCaixa(g, r, Px(ThemeManager.RaioPequeno), ThemeManager.AcentoSuave,
                    ThemeManager.Misturar(ThemeManager.Acento, ThemeManager.FundoInput, 0.25f));
                TextRenderer.DrawText(g, "NOMINAL", ThemeManager.FonteMicro, r, ThemeManager.Acento,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                x = r.X - Px(4);
            }

            TextRenderer.DrawText(g, Text, ThemeManager.FonteRotulo, new Rectangle(Px(8), topo, Math.Max(0, x - Px(8)), hRotulo),
                ThemeManager.TextoSecundario,
                TextFormatFlags.Right | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, _valor, ThemeManager.FonteMonoValor, new Rectangle(Px(8), topo + hRotulo, dir - Px(8), hValor),
                RotuloNordic.CorDoPapel(_papelValor),
                TextFormatFlags.Right | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }
    }

    /// <summary>Botão de tema do cabeçalho: ícone lua/sol + "Modo Escuro"/"Modo Claro"; alterna e persiste.</summary>
    public class BotaoTema : ControleNordic
    {
        private bool _hover;

        public BotaoTema() : base(selecionavel: true)
        {
            Size = new Size(130, 28);
            Cursor = Cursors.Hand;
            AccessibleRole = AccessibleRole.PushButton;
            AtualizarTexto();
        }

        private void AtualizarTexto()
        {
            // Mostra a ação: no claro oferece o escuro, e vice-versa (como no protótipo)
            Text = ThemeManager.Escuro ? "Modo Claro" : "Modo Escuro";
            AccessibleName = $"Alternar para o {Text.ToLowerInvariant()}";
        }

        protected override void AoMudarTema()
        {
            AtualizarTexto();
            base.AoMudarTema();
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
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            ThemeManager.Alternar();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode is Keys.Space or Keys.Enter)
            {
                ThemeManager.Alternar();
                e.Handled = e.SuppressKeyPress = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            LimparComFundoDoPai(g);
            ThemeManager.PrepararAntialias(g);
            ThemeManager.DesenharCaixa(g, ClientRectangle, Px(ThemeManager.RaioMedio),
                _hover ? ThemeManager.FundoHover : ThemeManager.FundoInput,
                _hover ? ThemeManager.Acento : ThemeManager.BordaForte);

            int icone = Px(14);
            Size m = TextRenderer.MeasureText(g, Text, ThemeManager.FonteBotaoPequeno, Size.Empty, TextFormatFlags.NoPadding);
            int total = icone + Px(6) + m.Width;
            int x = (Width - total) / 2;
            Icones.Desenhar(g, ThemeManager.Escuro ? Icone.Sol : Icone.Lua, new RectangleF(x, (Height - icone) / 2f, icone, icone),
                ThemeManager.TextoPrincipal);
            TextRenderer.DrawText(g, Text, ThemeManager.FonteBotaoPequeno, new Rectangle(x + icone + Px(6), 0, m.Width + Px(2), Height),
                ThemeManager.TextoPrincipal, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

            if (MostrarFoco) ThemeManager.DesenharFoco(g, ClientRectangle, Px(ThemeManager.RaioMedio));
        }
    }
}
