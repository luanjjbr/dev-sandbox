using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DevSandbox.UI.Common
{
    public enum TipoToast
    {
        Info,
        Sucesso,
        Aviso,
        Erro
    }

    /// <summary>
    /// Notificação não bloqueante no canto inferior direito da janela (empilhada, some sozinha em ~3,5 s).
    /// Substitui os MessageBox de aviso de rotina (skill design-interfaces §3.1). Não rouba o foco.
    /// </summary>
    public sealed class Toast : Form
    {
        private static readonly List<Toast> Abertos = new();
        private readonly Form _dono;
        private readonly TipoToast _tipo;
        private readonly string _mensagem;
        private readonly System.Windows.Forms.Timer _timer;

        private Toast(Form dono, string mensagem, TipoToast tipo)
        {
            _dono = dono;
            _mensagem = mensagem;
            _tipo = tipo;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            BackColor = ThemeManager.FundoCard;
            Font = ThemeManager.FonteUi;

            int largura = ThemeManager.Px(dono, 340);
            using (Graphics g = dono.CreateGraphics())
            {
                Size m = TextRenderer.MeasureText(g, mensagem, Font, new Size(largura - ThemeManager.Px(dono, 40), 0),
                                                  TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
                ClientSize = new Size(largura, Math.Max(ThemeManager.Px(dono, 44), m.Height + ThemeManager.Px(dono, 22)));
            }

            _timer = new System.Windows.Forms.Timer { Interval = tipo == TipoToast.Erro ? 5000 : 3500 };
            _timer.Tick += (_, _) => Close();
            ThemeManager.TemaMudou += AoMudarTema;
            Click += (_, _) => Close();
        }

        private void AoMudarTema(object? sender, EventArgs e)
        {
            if (IsDisposed) return;
            BackColor = ThemeManager.FundoCard;
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.TemaMudou -= AoMudarTema;
            base.Dispose(disposing);
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                return cp;
            }
        }

        /// <summary>Mostra um toast sobre a janela de <paramref name="origem"/>.</summary>
        public static void Mostrar(Control origem, string mensagem, TipoToast tipo = TipoToast.Info)
        {
            Form? dono = origem.FindForm();
            if (dono is null || dono.IsDisposed || !dono.IsHandleCreated) return;

            var t = new Toast(dono, mensagem, tipo);
            Abertos.Add(t);
            t.FormClosed += (_, _) =>
            {
                Abertos.Remove(t);
                t._timer.Dispose();
                Reposicionar();
                t.Dispose();
            };
            Reposicionar();
            t.Show(dono);
            t._timer.Start();
        }

        private static void Reposicionar()
        {
            int base_ = 0;
            for (int i = Abertos.Count - 1; i >= 0; i--)
            {
                Toast t = Abertos[i];
                Rectangle area = t._dono.RectangleToScreen(t._dono.ClientRectangle);
                int margem = ThemeManager.Px(t._dono, 24);
                t.Location = new Point(area.Right - t.Width - margem, area.Bottom - margem - base_ - t.Height);
                base_ += t.Height + ThemeManager.Px(t._dono, 8);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Color faixa = _tipo switch
            {
                TipoToast.Sucesso => ThemeManager.Sucesso,
                TipoToast.Aviso => ThemeManager.Aviso,
                TipoToast.Erro => ThemeManager.Perigo,
                _ => ThemeManager.Acento
            };

            using (var pen = new Pen(ThemeManager.BordaForte))
            {
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
            using (var b = new SolidBrush(faixa))
            {
                g.FillRectangle(b, 0, 0, ThemeManager.Px(this, 4), Height);
            }

            var area = new Rectangle(ThemeManager.Px(this, 18), 0, Width - ThemeManager.Px(this, 30), Height);
            TextRenderer.DrawText(g, _mensagem, Font, area, ThemeManager.TextoPrincipal,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        }
    }
}
