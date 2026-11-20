using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DevSandbox.UI.Common
{
    /// <summary>Clique num segmento: índice e se ele já era o selecionado (permite o "reenviar" da Bancada).</summary>
    public sealed class SegmentoEventArgs(int indice, bool jaSelecionado) : EventArgs
    {
        public int Indice { get; } = indice;
        public bool JaSelecionado { get; } = jaSelecionado;
    }

    /// <summary>
    /// Seletor segmentado: tolerância do CP (botões separados) e modos do Pomodoro (barra em cartão).
    /// O segmento ativo fica em acento com texto branco. Teclado: ←/→ trocam o segmento; Espaço/Enter clicam o atual.
    /// </summary>
    public class SeletorSegmentado : ControleNordic
    {
        private readonly List<string> _opcoes = new();
        private readonly List<bool> _habilitadas = new();
        private int _indice = -1;
        private int _hover = -1;
        private bool _emCartao;

        public event EventHandler<SegmentoEventArgs>? SegmentoClicado;

        public SeletorSegmentado() : base(selecionavel: true)
        {
            Size = new Size(300, 32);
            Cursor = Cursors.Hand;
        }

        /// <summary>true = barra dentro de um cartão (Pomodoro); false = botões separados com borda (tolerância).</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EmCartao
        {
            get => _emCartao;
            set
            {
                _emCartao = value;
                Invalidate();
            }
        }

        public void DefinirOpcoes(params string[] opcoes)
        {
            _opcoes.Clear();
            _opcoes.AddRange(opcoes);
            _habilitadas.Clear();
            foreach (string _ in opcoes) _habilitadas.Add(true);
            if (_indice >= _opcoes.Count) _indice = -1;
            AccessibleRole = AccessibleRole.PageTabList;
            Invalidate();
        }

        public void DefinirTexto(int indice, string texto)
        {
            _opcoes[indice] = texto;
            Invalidate();
        }

        public void HabilitarOpcao(int indice, bool habilitada)
        {
            _habilitadas[indice] = habilitada;
            Invalidate();
        }

        /// <summary>Segmento ativo (-1 = nenhum). Definir não dispara <see cref="SegmentoClicado"/>.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex
        {
            get => _indice;
            set
            {
                _indice = value;
                Invalidate();
            }
        }

        private Rectangle AreaSegmento(int i)
        {
            int n = Math.Max(1, _opcoes.Count);
            int pad = _emCartao ? Px(5) : 0;
            int gap = _emCartao ? Px(8) : Px(8);
            int largura = (Width - pad * 2 - gap * (n - 1)) / n;
            return new Rectangle(pad + i * (largura + gap), pad, largura, Height - pad * 2);
        }

        private int SegmentoEm(Point p)
        {
            for (int i = 0; i < _opcoes.Count; i++)
            {
                if (AreaSegmento(i).Contains(p)) return i;
            }
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int h = SegmentoEm(e.Location);
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
            if (e.Button != MouseButtons.Left || !Enabled) return;
            Focus();
            Clicar(SegmentoEm(e.Location));
        }

        private void Clicar(int i)
        {
            if (i < 0 || i >= _opcoes.Count || !_habilitadas[i]) return;
            bool ja = i == _indice;
            _indice = i;
            Invalidate();
            SegmentoClicado?.Invoke(this, new SegmentoEventArgs(i, ja));
        }

        protected override bool IsInputKey(Keys keyData) =>
            (keyData & Keys.KeyCode) is Keys.Left or Keys.Right || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!Enabled || _opcoes.Count == 0) return;

            int passo = e.KeyCode switch { Keys.Left => -1, Keys.Right => 1, _ => 0 };
            if (passo != 0)
            {
                int i = _indice;
                for (int k = 0; k < _opcoes.Count; k++)
                {
                    i = Math.Clamp(i + passo, 0, _opcoes.Count - 1);
                    if (_habilitadas[i]) break;
                }
                if (i != _indice && _habilitadas[i]) Clicar(i);
                e.Handled = true;
            }
            else if (e.KeyCode is Keys.Space or Keys.Enter)
            {
                Clicar(Math.Max(0, _indice));
                e.Handled = e.SuppressKeyPress = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            LimparComFundoDoPai(g);
            ThemeManager.PrepararAntialias(g);

            if (_emCartao)
            {
                ThemeManager.DesenharCaixa(g, ClientRectangle, Px(ThemeManager.RaioMedio), ThemeManager.FundoCard, ThemeManager.BordaSutil);
            }

            for (int i = 0; i < _opcoes.Count; i++)
            {
                Rectangle r = AreaSegmento(i);
                bool ativo = i == _indice && Enabled;
                bool habilitado = Enabled && _habilitadas[i];
                bool hover = i == _hover && habilitado && !ativo;

                Color fundo, texto;
                Color? borda;
                if (ativo)
                {
                    fundo = ThemeManager.Acento;
                    texto = Color.White;
                    borda = ThemeManager.Acento;
                }
                else
                {
                    fundo = _emCartao ? (hover ? ThemeManager.FundoHover : ThemeManager.FundoCard)
                                      : (hover ? ThemeManager.FundoHover : ThemeManager.FundoCard);
                    texto = !habilitado ? ThemeManager.TextoMudo : hover ? ThemeManager.TextoPrincipal : ThemeManager.TextoSecundario;
                    borda = _emCartao ? null : ThemeManager.BordaForte;
                }

                ThemeManager.DesenharCaixa(g, r, Px(ThemeManager.RaioPequeno), fundo, borda);
                TextRenderer.DrawText(g, _opcoes[i], ThemeManager.FonteBotaoPequeno, r, texto,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                if (MostrarFoco && i == Math.Max(0, _indice))
                {
                    ThemeManager.DesenharFoco(g, r, Px(ThemeManager.RaioPequeno));
                }
            }
        }
    }
}
