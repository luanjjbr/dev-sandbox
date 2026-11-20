using System;
using System.Drawing;
using System.Windows.Forms;
using DevSandbox.UI.Common;
using DevSandbox.UI.Telas;

namespace DevSandbox.UI.Forms
{
    /// <summary>
    /// Janela única do DevSandbox: cabeçalho (título + botão de tema), barra de abas e a área das telas.
    /// As telas são criadas uma vez e só trocam de visibilidade: Supervisório e Bancada mantêm a conexão
    /// (cada um com seu motor e sua porta) ao trocar de aba. Tudo é descartado ao fechar a janela.
    /// Atalhos: Ctrl+Tab / Ctrl+Shift+Tab trocam de aba.
    /// </summary>
    public sealed class MainForm : Form
    {
        private readonly Panel _cabecalho;
        private readonly RotuloNordic _titulo;
        private readonly RotuloNordic _subtitulo;
        private readonly BotaoTema _botaoTema;
        private readonly BarraAbas _abas;
        private readonly Panel _area;
        private readonly TelaBase[] _telas;

        public MainForm()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            SuspendLayout();

            Text = "DevSandbox .NET 10";
            StartPosition = FormStartPosition.CenterScreen;
            Font = ThemeManager.FonteUi;
            ClientSize = new Size(1240, 860);
            MinimumSize = new Size(1080, 640);
            KeyPreview = true;

            _titulo = new RotuloNordic { Text = "DevSandbox .NET 10", Font = ThemeManager.FonteUiForte, AutoSize = true };
            _subtitulo = new RotuloNordic { Text = "Utilitários de engenharia e bancada", Papel = PapelTexto.Secundario, AutoSize = true };
            _botaoTema = new BotaoTema { Size = new Size(132, 28) };

            _cabecalho = new Panel { Dock = DockStyle.Top, Height = 48 };
            _cabecalho.Controls.AddRange(new Control[] { _titulo, _subtitulo, _botaoTema });
            _cabecalho.Paint += (_, e) =>
            {
                ThemeManager.PrepararAntialias(e.Graphics);
                int tam = ThemeManager.Px(this, 18);
                Icones.Desenhar(e.Graphics, Icone.Raio, new RectangleF(ThemeManager.Px(this, 16), (_cabecalho.Height - tam) / 2f, tam, tam), ThemeManager.Acento);
                using var pen = new Pen(ThemeManager.BordaSutil);
                e.Graphics.DrawLine(pen, 0, _cabecalho.Height - 1, _cabecalho.Width, _cabecalho.Height - 1);
            };
            _cabecalho.Layout += (_, _) => PosicionarCabecalho();

            _abas = new BarraAbas { Dock = DockStyle.Top, Height = 40 };
            _abas.Adicionar("Início", Icone.Inicio);
            _abas.Adicionar("Supervisório Serial", Icone.Plugue);
            _abas.Adicionar("Bancada EVSE", Icone.Raio);
            _abas.Adicionar("Calculadora Hex & Bits", Icone.Hash);
            _abas.Adicionar("Pomodoro Timer", Icone.Relogio);

            _area = new Panel { Dock = DockStyle.Fill };

            var inicio = new TelaInicio();
            _telas = new TelaBase[] { inicio, new TelaSupervisorio(), new TelaBancada(), new TelaCalculadora(), new TelaPomodoro() };
            foreach (TelaBase t in _telas)
            {
                t.Dock = DockStyle.Fill;
                t.Visible = false;
                _area.Controls.Add(t);
            }
            inicio.AbrirTela += (_, indice) => _abas.Ativa = indice;
            _abas.AbaSelecionada += (_, indice) => MostrarTela(indice);

            Controls.Add(_area);
            Controls.Add(_abas);
            Controls.Add(_cabecalho);

            ThemeManager.TemaMudou += AoMudarTema;
            AplicarCores();
            MostrarTela(0);
            ResumeLayout(true);
        }

        private void PosicionarCabecalho()
        {
            int esq = ThemeManager.Px(this, 16) + ThemeManager.Px(this, 18) + ThemeManager.Px(this, 8);
            _cabecalho.Height = ThemeManager.Px(this, 48);
            int h = _cabecalho.Height;
            _titulo.Location = new Point(esq, (h - _titulo.Height) / 2);
            _subtitulo.Location = new Point(_titulo.Right + ThemeManager.Px(this, 8), (h - _subtitulo.Height) / 2);
            _botaoTema.Size = new Size(ThemeManager.Px(this, 132), ThemeManager.Px(this, 28));
            _botaoTema.Location = new Point(_cabecalho.Width - _botaoTema.Width - ThemeManager.Px(this, 16), (h - _botaoTema.Height) / 2);
        }

        private void MostrarTela(int indice)
        {
            SuspendLayout();
            for (int i = 0; i < _telas.Length; i++)
            {
                _telas[i].Visible = i == indice;
            }
            ResumeLayout(true);
            _telas[indice].AoAtivar();
        }

        private void AplicarCores()
        {
            BackColor = ThemeManager.FundoApp;
            _cabecalho.BackColor = ThemeManager.FundoCabecalho;
            _area.BackColor = ThemeManager.FundoApp;
        }

        private void AoMudarTema(object? sender, EventArgs e)
        {
            if (IsDisposed) return;
            AplicarCores();
            Invalidate(true);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ThemeManager.AplicarBarraDeTitulo(this);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Tab))
            {
                _abas.Ativa = (_abas.Ativa + 1) % _telas.Length;
                return true;
            }
            if (keyData == (Keys.Control | Keys.Shift | Keys.Tab))
            {
                _abas.Ativa = (_abas.Ativa + _telas.Length - 1) % _telas.Length;
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Fechamento confirmado (nenhum FormClosing cancelou): as telas descartam motores, portas e timers
            ThemeManager.TemaMudou -= AoMudarTema;
            foreach (TelaBase t in _telas)
            {
                t.Dispose();
            }
            base.OnFormClosed(e);
        }
    }
}
