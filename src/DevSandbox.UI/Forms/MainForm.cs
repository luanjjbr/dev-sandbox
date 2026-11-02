using System;
using System.Drawing;
using System.Windows.Forms;
using DevSandbox.UI.Common;

namespace DevSandbox.UI.Forms
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            // Configurações do Form
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Aplica tema escuro na barra de título nativa e nos controles
            ThemeManager.AplicarBarraDeTituloEscura(this);
            AplicarTema();
        }

        private void AplicarTema()
        {
            this.BackColor = ThemeManager.FundoJanela;
            this.ForeColor = ThemeManager.TextoPrincipal;

            // Header e Footer
            panelHeader.BackColor = ThemeManager.FundoJanela;
            lblTitulo.ForeColor = ThemeManager.TextoPrincipal;
            lblSubtitulo.ForeColor = ThemeManager.TextoSecundario;

            panelFooter.BackColor = ThemeManager.FundoJanela;
            lblFooter.ForeColor = ThemeManager.TextoSecundario;

            // Configuração dos Cards Fluent
            ConfigurarCard(cardSupervisorio, lblTituloSupervisorio, lblDescSupervisorio, btnAbrirSupervisorio, AbrirSupervisorio);
            ConfigurarCard(cardPomodoro, lblTituloPomodoro, lblDescPomodoro, btnAbrirPomodoro, AbrirPomodoro);
            ConfigurarCard(cardCalculadora, lblTituloCalculadora, lblDescCalculadora, btnAbrirCalculadora, AbrirCalculadora);
            ConfigurarCard(cardBancada, lblTituloBancada, lblDescBancada, btnAbrirBancada, AbrirBancada);
        }

        private static void ConfigurarCard(FluentCard card, Label titulo, Label desc, FluentButton botao, Action aoClicar)
        {
            card.CardColor = ThemeManager.FundoCard;
            card.BorderColor = ThemeManager.BordaCard;
            card.EnableHoverEffect = true;

            titulo.ForeColor = ThemeManager.TextoPrincipal;
            desc.ForeColor = ThemeManager.TextoSecundario;

            botao.IsAccent = true;

            card.Cursor = Cursors.Hand;
            titulo.Cursor = Cursors.Hand;
            desc.Cursor = Cursors.Hand;

            card.Click += (s, e) => aoClicar();
            titulo.Click += (s, e) => aoClicar();
            desc.Click += (s, e) => aoClicar();
        }

        private void AbrirSupervisorio()
        {
            using var form = new FormSupervisorio();
            this.Hide();
            form.ShowDialog();
            this.Show();
        }

        private void AbrirPomodoro()
        {
            using var form = new FormPomodoro();
            this.Hide();
            form.ShowDialog();
            this.Show();
        }

        private void AbrirCalculadora()
        {
            using var form = new FormCalculadora();
            this.Hide();
            form.ShowDialog();
            this.Show();
        }

        private void AbrirBancada()
        {
            using var form = new FormBancada();
            this.Hide();
            form.ShowDialog();
            this.Show();
        }

        private void btnAbrirSupervisorio_Click(object sender, EventArgs e) => AbrirSupervisorio();
        private void btnAbrirPomodoro_Click(object sender, EventArgs e) => AbrirPomodoro();
        private void btnAbrirCalculadora_Click(object sender, EventArgs e) => AbrirCalculadora();
        private void btnAbrirBancada_Click(object sender, EventArgs e) => AbrirBancada();
    }
}
