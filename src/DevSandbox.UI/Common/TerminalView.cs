using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DevSandbox.UI.Common
{
    /// <summary>Tipo de linha do terminal (define a etiqueta e a cor).</summary>
    public enum TipoLinha
    {
        Sistema,
        Rx,
        Tx,
        Aviso,
        Erro
    }

    /// <summary>Uma linha do log, montada pela tela e desenhada pelo <see cref="TerminalView"/>.</summary>
    public readonly record struct LinhaTerminal(DateTime Hora, TipoLinha Tipo, string Texto, string? FimDeLinha = null);

    /// <summary>
    /// Terminal embutido Nordic: cabeçalho escuro (LED, título, "Acompanhar o final", tamanho do buffer), log colorido
    /// por tipo ([SYS]/[RX]/[TX]/[AVISO]/[ERRO]) com etiqueta do fim de linha no TX, e barra de rolagem própria
    /// (fina, escura, sem setas). Limite de 50.000 caracteres: acima disso descarta a metade mais antiga.
    /// Com "Acompanhar o final" desligado, linhas novas não mexem na posição de leitura nem na seleção.
    /// </summary>
    public class TerminalView : UserControl
    {
        public const int LimiteCaracteres = 50000;

        private readonly Panel _cabecalho;
        private readonly LedNordic _led;
        private readonly Label _titulo;
        private readonly Label _infoDireita;
        private readonly ChaveNordic _acompanhar;
        private readonly Label _buffer;
        private readonly CaixaLog _log;
        private readonly BarraRolagemFina _barra;

        public TerminalView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            _led = new LedNordic { Estado = EstadoLed.Apagado };
            _titulo = new Label { AutoSize = true, Font = ThemeManager.FonteMonoPequena, BackColor = Color.Transparent, UseMnemonic = false };
            _infoDireita = new Label { AutoSize = true, Font = ThemeManager.FonteMonoPequena, BackColor = Color.Transparent, UseMnemonic = false };
            _acompanhar = new ChaveNordic { Text = "Acompanhar o final", Checked = true, Width = 160 };
            _buffer = new Label { AutoSize = true, Font = ThemeManager.FonteMonoPequena, BackColor = Color.Transparent, UseMnemonic = false };

            _cabecalho = new Panel { Dock = DockStyle.Top, Height = 30 };
            _cabecalho.Controls.AddRange(new Control[] { _led, _titulo, _infoDireita, _acompanhar, _buffer });
            _cabecalho.Layout += (_, _) => PosicionarCabecalho();
            _cabecalho.Paint += (_, e) =>
            {
                using var pen = new Pen(ThemeManager.TermBorda);
                e.Graphics.DrawLine(pen, 0, _cabecalho.Height - 1, _cabecalho.Width, _cabecalho.Height - 1);
            };

            _log = new CaixaLog
            {
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.None,
                WordWrap = true,
                DetectUrls = false,
                Font = ThemeManager.FonteMono,
                ShortcutsEnabled = true,
                TabStop = true
            };
            _barra = new BarraRolagemFina { Dock = DockStyle.Right, Width = 10 };

            var corpo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 2, 8) };
            corpo.Controls.Add(_log);
            corpo.Controls.Add(_barra);
            _log.Dock = DockStyle.Fill;
            corpo.Layout += (_, _) => { if (_barra.Width != ThemeManager.Px(this, 10)) _barra.Width = ThemeManager.Px(this, 10); };

            Controls.Add(corpo);
            Controls.Add(_cabecalho);
            Padding = new Padding(1);

            _log.Rolou += (_, _) => AtualizarBarra();
            _log.Resize += (_, _) => AtualizarBarra();
            _barra.Rolou += (_, pos) => _log.DefinirPosicaoY(pos);
            _acompanhar.CheckedChanged += (_, _) =>
            {
                if (_acompanhar.Checked) RolarAoFinal();
                AcompanharMudou?.Invoke(this, EventArgs.Empty);
            };

            ThemeManager.TemaMudou += AoMudarTema;
            AplicarCores();
            AtualizarBuffer();
        }

        public event EventHandler? AcompanharMudou;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Titulo
        {
            get => _titulo.Text;
            set
            {
                _titulo.Text = value;
                PosicionarCabecalho();
            }
        }

        /// <summary>Texto à direita do título (ex.: dispositivo/protocolo).</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string InfoCabecalho
        {
            get => _infoDireita.Text;
            set
            {
                _infoDireita.Text = value;
                PosicionarCabecalho();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public EstadoLed EstadoLed
        {
            get => _led.Estado;
            set => _led.Estado = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool AcompanharFinal
        {
            get => _acompanhar.Checked;
            set => _acompanhar.Checked = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string NomeAcessivelLog
        {
            get => _log.AccessibleName ?? "";
            set => _log.AccessibleName = value;
        }

        public int TamanhoTexto => _log.TextLength;

        private void PosicionarCabecalho()
        {
            int x = ThemeManager.Px(this, 12);
            _cabecalho.Height = ThemeManager.Px(this, 30);
            int h = _cabecalho.Height;
            _led.SetBounds(x, (h - _led.Height) / 2, ThemeManager.Px(this, 14), ThemeManager.Px(this, 14));
            _titulo.Location = new Point(_led.Right + ThemeManager.Px(this, 6), (h - _titulo.Height) / 2);

            int dir = _cabecalho.Width - ThemeManager.Px(this, 12);
            _buffer.Location = new Point(dir - _buffer.Width, (h - _buffer.Height) / 2);
            _acompanhar.Height = ThemeManager.Px(this, 22);
            _acompanhar.Width = ThemeManager.Px(this, 160);
            _acompanhar.Location = new Point(_buffer.Left - ThemeManager.Px(this, 14) - _acompanhar.Width, (h - _acompanhar.Height) / 2);
            _infoDireita.Location = new Point(_acompanhar.Left - ThemeManager.Px(this, 14) - _infoDireita.Width, (h - _infoDireita.Height) / 2);
            _infoDireita.Visible = !string.IsNullOrEmpty(_infoDireita.Text) && _infoDireita.Left > _titulo.Right + ThemeManager.Px(this, 8);
        }

        private void AplicarCores()
        {
            BackColor = ThemeManager.TermFundo;
            _cabecalho.BackColor = ThemeManager.TermCabecalho;
            _titulo.ForeColor = ThemeManager.TermApagado;
            _infoDireita.ForeColor = ThemeManager.TermApagado;
            _buffer.ForeColor = ThemeManager.TermApagado;
            _acompanhar.CorTexto = ThemeManager.TermApagado;
            _log.Parent!.BackColor = ThemeManager.TermFundo;
            _log.BackColor = ThemeManager.TermFundo;
            _log.ForeColor = ThemeManager.TermTexto;
            Invalidate(true);
        }

        private void AoMudarTema(object? sender, EventArgs e)
        {
            if (IsDisposed) return;
            // As cores das linhas já gravadas ficam como estão (o terminal é escuro nos dois temas)
            AplicarCores();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using var pen = new Pen(ThemeManager.TermBorda);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        // ------------------------------------------------------------------ Conteúdo

        public void AdicionarLinha(TipoLinha tipo, string texto, string? fimDeLinha = null) =>
            AdicionarLinhas(new[] { new LinhaTerminal(DateTime.Now, tipo, texto, fimDeLinha) });

        /// <summary>Acrescenta um lote de linhas com uma única repintura (recepção em lotes de 50 ms).</summary>
        public void AdicionarLinhas(IReadOnlyList<LinhaTerminal> linhas)
        {
            if (IsDisposed || linhas.Count == 0) return;

            bool acompanhar = _acompanhar.Checked;
            Point posicao = _log.ObterPosicaoRolagem();
            int selInicio = _log.SelectionStart;
            int selTamanho = _log.SelectionLength;
            // Primeiro caractere visível: referência para manter a leitura se o limite cortar o início
            int topoVisivel = acompanhar ? 0 : _log.GetCharIndexFromPosition(new Point(1, 1));

            _log.SuspenderDesenho();
            try
            {
                foreach (LinhaTerminal l in linhas)
                {
                    Escrever(l);
                }

                int removidos = AplicarLimite();
                if (acompanhar)
                {
                    _log.Select(_log.TextLength, 0);
                    _log.RolarAoFim();
                }
                else
                {
                    // Mantém a leitura e a seleção do operador
                    int inicio = Math.Max(0, selInicio - removidos);
                    _log.Select(inicio, selInicio - removidos >= 0 ? selTamanho : 0);
                    if (removidos == 0)
                    {
                        _log.DefinirPosicaoRolagem(posicao);
                    }
                    else
                    {
                        // O mesmo texto continua no topo; se ele foi cortado, a leitura vai para o início do que restou
                        int novoTopo = Math.Max(0, topoVisivel - removidos);
                        int yNoDocumento = _log.GetPositionFromCharIndex(novoTopo).Y + _log.ObterPosicaoRolagem().Y;
                        _log.DefinirPosicaoY(yNoDocumento);
                    }
                }
            }
            finally
            {
                _log.RetomarDesenho();
            }

            AtualizarBuffer();
            AtualizarBarra();
        }

        private void Escrever(LinhaTerminal l)
        {
            bool primeira = _log.TextLength == 0;
            if (!primeira) Anexar(Environment.NewLine, ThemeManager.TermTexto, ThemeManager.FonteMono);

            Anexar($"[{l.Hora:HH:mm:ss}] ", ThemeManager.TermApagado, ThemeManager.FonteMono);
            (string etiqueta, Color cor) = l.Tipo switch
            {
                TipoLinha.Rx => ("[RX]", ThemeManager.TermCiano),
                TipoLinha.Tx => ("[TX]", ThemeManager.TermVerde),
                TipoLinha.Aviso => ("[AVISO]", ThemeManager.TermAmarelo),
                TipoLinha.Erro => ("[ERRO]", ThemeManager.TermVermelho),
                _ => ("[SYS]", ThemeManager.TermCiano)
            };
            Anexar(etiqueta + " ", cor, ThemeManager.FonteMonoForte);
            Anexar(l.Texto, l.Tipo == TipoLinha.Erro ? ThemeManager.TermVermelho : ThemeManager.TermTexto, ThemeManager.FonteMono);

            if (!string.IsNullOrEmpty(l.FimDeLinha))
            {
                Anexar("  ⏎" + l.FimDeLinha, ThemeManager.TermCiano, ThemeManager.FonteMonoPequena);
            }
        }

        private void Anexar(string texto, Color cor, Font fonte)
        {
            _log.Select(_log.TextLength, 0);
            _log.SelectionColor = cor;
            _log.SelectionFont = fonte;
            _log.SelectedText = texto;
        }

        /// <summary>Acima do limite, descarta a metade mais antiga (cortando numa quebra de linha). Retorna o nº de caracteres removidos.</summary>
        private int AplicarLimite()
        {
            if (_log.TextLength <= LimiteCaracteres) return 0;

            int corte = _log.TextLength - LimiteCaracteres / 2;
            string texto = _log.Text;
            int quebra = texto.IndexOf('\n', Math.Min(corte, texto.Length - 1));
            corte = quebra >= 0 ? quebra + 1 : corte;

            // Num RichTextBox somente leitura a troca da seleção por "" é ignorada: libera só durante o corte.
            // O retorno é o que saiu de fato (medido), não o pretendido
            int antes = _log.TextLength;
            _log.ReadOnly = false;
            try
            {
                _log.Select(0, corte);
                _log.SelectedText = "";
            }
            finally
            {
                _log.ReadOnly = true;
            }
            return antes - _log.TextLength;
        }

        public void Limpar()
        {
            _log.Clear();
            AtualizarBuffer();
            AtualizarBarra();
        }

        public void RolarAoFinal()
        {
            _log.Select(_log.TextLength, 0);
            _log.RolarAoFim();
            AtualizarBarra();
        }

        private void AtualizarBuffer()
        {
            int bytes = _log.TextLength;
            string atual = bytes < 1024 ? $"{bytes} B" : $"{bytes / 1024.0:0.0} KB";
            _buffer.Text = $"Buffer: {atual} / {LimiteCaracteres / 1000} KB";
            PosicionarCabecalho();
        }

        private void AtualizarBarra()
        {
            (int total, int visivel, int pos) = _log.ObterMetricas();
            _barra.Definir(total, visivel, pos);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.TemaMudou -= AoMudarTema;
            base.Dispose(disposing);
        }

        // ------------------------------------------------------------------ RichTextBox sem barra nativa

        private sealed class CaixaLog : RichTextBox
        {
            private const int WM_SETREDRAW = 0x000B;
            private const int WM_MOUSEWHEEL = 0x020A;
            private const int EM_GETSCROLLPOS = 0x0400 + 221;
            private const int EM_SETSCROLLPOS = 0x0400 + 222;

            [StructLayout(LayoutKind.Sequential)]
            private struct PONTO
            {
                public int X;
                public int Y;
            }

            [DllImport("user32.dll", EntryPoint = "SendMessageW")]
            private static extern IntPtr EnviarPonto(IntPtr hWnd, int msg, IntPtr wParam, ref PONTO lParam);

            [DllImport("user32.dll", EntryPoint = "SendMessageW")]
            private static extern IntPtr Enviar(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

            private int _suspensoes;

            public event EventHandler? Rolou;

            public void SuspenderDesenho()
            {
                if (_suspensoes++ == 0 && IsHandleCreated) Enviar(Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            }

            public void RetomarDesenho()
            {
                if (--_suspensoes == 0 && IsHandleCreated)
                {
                    Enviar(Handle, WM_SETREDRAW, 1, IntPtr.Zero);
                    Invalidate();
                }
            }

            public Point ObterPosicaoRolagem()
            {
                if (!IsHandleCreated) return Point.Empty;
                var p = new PONTO();
                EnviarPonto(Handle, EM_GETSCROLLPOS, IntPtr.Zero, ref p);
                return new Point(p.X, p.Y);
            }

            public void DefinirPosicaoRolagem(Point pos)
            {
                if (!IsHandleCreated) return;
                var p = new PONTO { X = pos.X, Y = pos.Y };
                EnviarPonto(Handle, EM_SETSCROLLPOS, IntPtr.Zero, ref p);
                Rolou?.Invoke(this, EventArgs.Empty);
            }

            public void DefinirPosicaoY(int y) => DefinirPosicaoRolagem(new Point(0, Math.Max(0, y)));

            public void RolarAoFim()
            {
                (int total, int visivel, _) = ObterMetricas();
                DefinirPosicaoY(Math.Max(0, total - visivel));
            }

            /// <summary>(altura total do conteúdo, altura visível, posição atual) em px.</summary>
            public (int Total, int Visivel, int Posicao) ObterMetricas()
            {
                if (!IsHandleCreated) return (0, ClientSize.Height, 0);
                int posY = ObterPosicaoRolagem().Y;
                int total = TextLength == 0 ? 0 : GetPositionFromCharIndex(TextLength).Y + posY + Font.Height + 4;
                return (total, ClientSize.Height, posY);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_MOUSEWHEEL)
                {
                    int delta = (short)((long)m.WParam >> 16 & 0xFFFF);
                    int linhas = SystemInformation.MouseWheelScrollLines <= 0 ? 3 : SystemInformation.MouseWheelScrollLines;
                    (int total, int visivel, int pos) = ObterMetricas();
                    int novo = Math.Clamp(pos - Math.Sign(delta) * linhas * Font.Height, 0, Math.Max(0, total - visivel));
                    DefinirPosicaoY(novo);
                    m.Result = IntPtr.Zero;
                    return;
                }
                base.WndProc(ref m);
            }

            protected override void OnVScroll(EventArgs e)
            {
                base.OnVScroll(e);
                Rolou?.Invoke(this, EventArgs.Empty);
            }

            protected override void OnSelectionChanged(EventArgs e)
            {
                base.OnSelectionChanged(e);
                if (_suspensoes == 0) Rolou?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>Barra de rolagem vertical fina no estilo do terminal (sem setas; trilho escuro, polegar arredondado).</summary>
    public class BarraRolagemFina : ControleNordic
    {
        private int _total;
        private int _visivel;
        private int _posicao;
        private bool _hover;
        private int? _arrasteOrigemY;
        private int _arrastePosInicial;

        public event EventHandler<int>? Rolou;

        public BarraRolagemFina() : base(selecionavel: false)
        {
            Width = 10;
        }

        public void Definir(int total, int visivel, int posicao)
        {
            _total = total;
            _visivel = visivel;
            _posicao = posicao;
            Invalidate();
        }

        private bool Necessaria => _total > _visivel && _visivel > 0;

        private Rectangle Polegar()
        {
            if (!Necessaria) return Rectangle.Empty;
            int altura = Math.Max(Px(24), (int)((long)Height * _visivel / _total));
            int curso = Height - altura;
            int maxPos = Math.Max(1, _total - _visivel);
            int y = (int)((long)curso * Math.Clamp(_posicao, 0, maxPos) / maxPos);
            return new Rectangle(0, y, Width, altura);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!Necessaria || e.Button != MouseButtons.Left) return;

            Rectangle p = Polegar();
            if (p.Contains(e.Location))
            {
                _arrasteOrigemY = e.Y;
                _arrastePosInicial = _posicao;
                Capture = true;
            }
            else
            {
                // Clique no trilho: avança uma página
                int pagina = e.Y < p.Top ? -_visivel : _visivel;
                Rolou?.Invoke(this, Math.Clamp(_posicao + pagina, 0, _total - _visivel));
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool h = Polegar().Contains(e.Location);
            if (h != _hover)
            {
                _hover = h;
                Invalidate();
            }

            if (_arrasteOrigemY is { } origem)
            {
                Rectangle p = Polegar();
                int curso = Math.Max(1, Height - p.Height);
                int novo = _arrastePosInicial + (int)((long)(e.Y - origem) * (_total - _visivel) / curso);
                Rolou?.Invoke(this, Math.Clamp(novo, 0, Math.Max(0, _total - _visivel)));
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _arrasteOrigemY = null;
            Capture = false;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(ThemeManager.TermFundo);
            if (!Necessaria) return;

            ThemeManager.PrepararAntialias(g);
            Rectangle p = Rectangle.Inflate(Polegar(), -Px(2), -Px(2));
            Color cor = _hover || _arrasteOrigemY.HasValue ? ThemeManager.TermApagado : ThemeManager.TermBorda;
            ThemeManager.DesenharCaixa(g, p, p.Width / 2, cor, null);
        }
    }
}
