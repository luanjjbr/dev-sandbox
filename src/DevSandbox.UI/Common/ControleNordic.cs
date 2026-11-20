using System;
using System.Drawing;
using System.Windows.Forms;

namespace DevSandbox.UI.Common
{
    /// <summary>
    /// Base dos controles desenhados do visual Nordic: double buffer, repintura no <see cref="ThemeManager.TemaMudou"/>
    /// e helpers de DPI. Subclasses pintam tudo em <see cref="Control.OnPaint"/> lendo os tokens do tema.
    /// </summary>
    public abstract class ControleNordic : Control
    {
        protected ControleNordic(bool selecionavel)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Selectable, selecionavel);
            TabStop = selecionavel;
            BackColor = Color.Transparent;
            ThemeManager.TemaMudou += AoMudarTema;
        }

        protected int Px(int px) => ThemeManager.Px(this, px);

        /// <summary>Chamado na troca de tema; o padrão só repinta.</summary>
        protected virtual void AoMudarTema() => Invalidate();

        private void AoMudarTema(object? sender, EventArgs e)
        {
            if (!IsDisposed) AoMudarTema();
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

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            Invalidate();
            base.OnTextChanged(e);
        }

        /// <summary>Fundo do pai (cantos arredondados sem pontas de outra cor).</summary>
        protected void LimparComFundoDoPai(Graphics g) => g.Clear(ThemeManager.ObterCorFundoEfetiva(this));

        protected bool MostrarFoco => Focused && ShowFocusCues && Enabled;

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.TemaMudou -= AoMudarTema;
            base.Dispose(disposing);
        }
    }

    /// <summary>Papel de cor de um <see cref="RotuloNordic"/>.</summary>
    public enum PapelTexto
    {
        Principal,
        Secundario,
        Mudo,
        Acento,
        Sucesso,
        Aviso,
        Perigo
    }

    /// <summary>
    /// Label transparente cuja cor vem de um papel do tema (reage à troca de tema).
    /// Telas que mostram status trocam o <see cref="Papel"/>, nunca o ForeColor direto.
    /// </summary>
    public class RotuloNordic : Label
    {
        private PapelTexto _papel = PapelTexto.Principal;

        public RotuloNordic()
        {
            BackColor = Color.Transparent;
            AutoSize = false;
            UseMnemonic = false;
            Font = ThemeManager.FonteUi;
            ThemeManager.TemaMudou += AoMudarTema;
            AtualizarCor();
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public PapelTexto Papel
        {
            get => _papel;
            set
            {
                _papel = value;
                AtualizarCor();
            }
        }

        public static Color CorDoPapel(PapelTexto papel) => papel switch
        {
            PapelTexto.Secundario => ThemeManager.TextoSecundario,
            PapelTexto.Mudo => ThemeManager.TextoMudo,
            PapelTexto.Acento => ThemeManager.Acento,
            PapelTexto.Sucesso => ThemeManager.Sucesso,
            PapelTexto.Aviso => ThemeManager.Aviso,
            PapelTexto.Perigo => ThemeManager.Perigo,
            _ => ThemeManager.TextoPrincipal
        };

        private void AtualizarCor() => ForeColor = CorDoPapel(_papel);

        private void AoMudarTema(object? sender, EventArgs e)
        {
            if (!IsDisposed) AtualizarCor();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.TemaMudou -= AoMudarTema;
            base.Dispose(disposing);
        }
    }
}
