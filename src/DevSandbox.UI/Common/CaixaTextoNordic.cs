using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;

namespace DevSandbox.UI.Common
{
    /// <summary>
    /// Caixa de texto Nordic: cavidade (fundo de input), borda forte de 1 px que vira acento no foco
    /// (com halo suave) e, com <see cref="Erro"/>, borda estritamente vermelha (#EF4444), sem texto empurrando o layout.
    /// </summary>
    public class CaixaTextoNordic : UserControl
    {
        private readonly TextBox _caixa;
        private bool _focado;
        private bool _erro;
        private bool _destaque;
        private string _placeholder = "";

        public event KeyEventHandler? InnerKeyDown;
        public event EventHandler? InnerTextChanged;

        public CaixaTextoNordic()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;

            _caixa = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = ThemeManager.FonteUi,
                TabStop = true
            };
            _caixa.GotFocus += (_, _) => { _focado = true; AtualizarCores(); Invalidate(); };
            _caixa.LostFocus += (_, _) => { _focado = false; AtualizarCores(); Invalidate(); };
            _caixa.KeyDown += (_, e) => InnerKeyDown?.Invoke(this, e);
            _caixa.TextChanged += (_, e) =>
            {
                InnerTextChanged?.Invoke(this, e);
                Invalidate();
            };
            Controls.Add(_caixa);
            Size = new Size(250, 32);

            TabStop = true;
            Cursor = Cursors.IBeam;
            Click += (_, _) => _caixa.Focus();
            ThemeManager.TemaMudou += AoMudarTema;
            AtualizarCores();
        }

        [AllowNull]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override string Text
        {
            get => _caixa.Text;
            set => _caixa.Text = value ?? "";
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string PlaceholderText
        {
            get => _placeholder;
            set
            {
                _placeholder = value ?? "";
                _caixa.PlaceholderText = _placeholder;
            }
        }

        /// <summary>Campo inválido: borda vermelha #EF4444 (a explicação vai em toast/dica).</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Erro
        {
            get => _erro;
            set
            {
                if (_erro == value) return;
                _erro = value;
                Invalidate();
            }
        }

        /// <summary>Visual "hero" da calculadora: borda de 2 px e texto em acento.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Destaque
        {
            get => _destaque;
            set
            {
                _destaque = value;
                AtualizarCores();
                PerformLayout();
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Font InnerFont
        {
            get => _caixa.Font;
            set
            {
                _caixa.Font = value;
                PerformLayout();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int MaxLength
        {
            get => _caixa.MaxLength;
            set => _caixa.MaxLength = value;
        }

        /// <summary>Nome lido pelo leitor de tela na caixa interna.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? NomeAcessivel
        {
            get => _caixa.AccessibleName;
            set => _caixa.AccessibleName = value;
        }

        /// <summary>Troca o texto e põe o cursor no fim (navegação do histórico com ↑/↓).</summary>
        public void DefinirTextoComCursorNoFim(string texto)
        {
            _caixa.Text = texto;
            _caixa.SelectionStart = _caixa.TextLength;
            _caixa.SelectionLength = 0;
        }

        public void SelectAll() => _caixa.SelectAll();
        public void Clear() => _caixa.Clear();
        public new bool Focus() => _caixa.Focus();

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            if (!_caixa.Focused && _caixa.CanFocus) _caixa.Focus();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            AtualizarCores();
            Invalidate();
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (_caixa is null) return; // layout disparado pelo construtor da base
            int margemX = ThemeManager.Px(this, _destaque ? 14 : 10);
            int h = _caixa.PreferredHeight;
            _caixa.SetBounds(margemX, Math.Max(2, (Height - h) / 2), Math.Max(10, Width - margemX * 2), h);
        }

        private Color FundoAtual => _focado ? ThemeManager.FundoCard : ThemeManager.FundoInput;

        private void AtualizarCores()
        {
            _caixa.BackColor = FundoAtual;
            _caixa.ForeColor = !Enabled ? ThemeManager.TextoMudo
                             : _destaque ? ThemeManager.Acento
                             : ThemeManager.TextoPrincipal;
        }

        private void AoMudarTema(object? sender, EventArgs e)
        {
            if (IsDisposed) return;
            AtualizarCores();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(ThemeManager.ObterCorFundoEfetiva(this));
            ThemeManager.PrepararAntialias(g);

            int raio = ThemeManager.Px(this, ThemeManager.RaioMedio);
            Color borda = _erro ? ThemeManager.BordaErro
                        : _focado ? ThemeManager.BordaFoco
                        : ThemeManager.BordaForte;
            float espessura = _destaque || _erro ? 2f : 1f;

            if (_focado && !_erro)
            {
                // Halo de foco (equivale ao box-shadow 0 0 0 3px do protótipo)
                ThemeManager.DesenharCaixa(g, ClientRectangle, raio + 2,
                    ThemeManager.Misturar(ThemeManager.BordaFoco, ThemeManager.ObterCorFundoEfetiva(this), 0.18f), null);
            }

            int inset = _focado && !_erro ? ThemeManager.Px(this, 2) : 0;
            Rectangle r = Rectangle.Inflate(ClientRectangle, -inset, -inset);
            ThemeManager.DesenharCaixa(g, r, raio, FundoAtual, borda, espessura);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.TemaMudou -= AoMudarTema;
            base.Dispose(disposing);
        }
    }
}
