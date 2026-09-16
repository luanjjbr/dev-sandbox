using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;
using DevSandbox.Core.Pomodoro;
using DevSandbox.UI.Common;

namespace DevSandbox.UI.Forms
{
    public partial class FormPomodoro : Form
    {
        private readonly PomodoroEngine _engine = new();

        public FormPomodoro()
        {
            InitializeComponent();

            ThemeManager.AplicarBarraDeTituloEscura(this);
            AplicarTema();

            _engine.OnTick += Engine_OnTick;
            _engine.OnEstadoMudou += Engine_OnEstadoMudou;
        }

        private void AplicarTema()
        {
            this.BackColor = ThemeManager.FundoJanela;
            this.ForeColor = ThemeManager.TextoPrincipal;

            lblTempo.ForeColor = ThemeManager.CorAcento;
            lblStatusCiclo.ForeColor = ThemeManager.TextoSecundario;

            cardControles.CardColor = ThemeManager.FundoCard;
            cardControles.BorderColor = ThemeManager.BordaCard;

            lblCustomMinutos.ForeColor = ThemeManager.TextoSecundario;
            numCiclos.BackColor = ThemeManager.FundoInput;
            numCiclos.ForeColor = ThemeManager.TextoPrincipal;

            AtualizarBotoesAcao();
        }

        private void FormPomodoro_Load(object sender, EventArgs e)
        {
            lblTempo.Text = $"{numCiclos.Value:00}:00";
        }

        private void numCiclos_ValueChanged(object sender, EventArgs e)
        {
            if (!_engine.EstaRodando)
            {
                lblTempo.Text = $"{numCiclos.Value:00}:00";
            }
        }

        private void btnTempo25_Click(object sender, EventArgs e) => IniciarContagemPorMinutos(25, EstadoPomodoro.Foco);
        private void btnTempo15_Click(object sender, EventArgs e) => IniciarContagemPorMinutos(15, EstadoPomodoro.DescansoLongo);
        private void btnTempo05_Click(object sender, EventArgs e) => IniciarContagemPorMinutos(5, EstadoPomodoro.DescansoCurto);

        private void IniciarContagemPorMinutos(int minutos, EstadoPomodoro estado)
        {
            _engine.Parar();
            numCiclos.Value = minutos;
            lblTempo.Text = $"{minutos:00}:00";
            IniciarPomodoro(estado);
        }

        private void btnIniciar_Click(object sender, EventArgs e)
        {
            if (!_engine.EstaRodando)
            {
                IniciarPomodoro(EstadoPomodoro.Foco);
            }
            else
            {
                _engine.Parar();
                lblStatusCiclo.Text = "Ciclo interrompido";
                lblTempo.Text = $"{numCiclos.Value:00}:00";
                HabilitarSeletores(true);
                AtualizarBotoesAcao();
            }
        }

        private void btnPausar_Click(object sender, EventArgs e)
        {
            if (_engine.EstaPausado)
            {
                _engine.Retomar();
                AtualizarStatusPorEstado(_engine.EstadoAtual);
            }
            else if (_engine.EstaRodando)
            {
                _engine.Pausar();
                lblStatusCiclo.Text = "⏸️ Timer Pausado";
                lblTempo.ForeColor = ThemeManager.TextoSecundario;
            }

            AtualizarBotoesAcao();
        }

        private void IniciarPomodoro(EstadoPomodoro estado)
        {
            int minutos = (int)numCiclos.Value;

            if (minutos > 0)
            {
                TimeSpan duracao = TimeSpan.FromMinutes(minutos);
                _engine.Iniciar(duracao, estado);
                AtualizarStatusPorEstado(estado);
                HabilitarSeletores(false);
                AtualizarBotoesAcao();
            }
            else
            {
                MessageBox.Show("Por favor, selecione um tempo maior que 0 minutos.",
                                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AtualizarStatusPorEstado(EstadoPomodoro estado)
        {
            switch (estado)
            {
                case EstadoPomodoro.Foco:
                    lblStatusCiclo.Text = "🎯 Em Foco total...";
                    lblTempo.ForeColor = ThemeManager.CorAcento;
                    break;
                case EstadoPomodoro.DescansoCurto:
                    lblStatusCiclo.Text = "☕ Descanso curto...";
                    lblTempo.ForeColor = ThemeManager.VerdeTerminal;
                    break;
                case EstadoPomodoro.DescansoLongo:
                    lblStatusCiclo.Text = "🌿 Descanso longo...";
                    lblTempo.ForeColor = ThemeManager.AzulEscuro;
                    break;
                case EstadoPomodoro.Pausado:
                    lblStatusCiclo.Text = "⏸️ Timer Pausado";
                    lblTempo.ForeColor = ThemeManager.TextoSecundario;
                    break;
                default:
                    lblStatusCiclo.Text = "Pronto para iniciar";
                    lblTempo.ForeColor = ThemeManager.TextoPrincipal;
                    break;
            }
        }

        private void AtualizarBotoesAcao()
        {
            if (!_engine.EstaRodando)
            {
                btnIniciar.Text = "Iniciar";
                btnIniciar.IsAccent = true;
                btnIniciar.CustomAccentColor = ThemeManager.CorAcento;
                btnIniciar.CustomTextColor = ThemeManager.CorAcentoTexto;

                btnPausar.Text = "Pausar";
                btnPausar.Enabled = false;
                btnPausar.IsAccent = false;
                btnPausar.CustomAccentColor = null;
            }
            else if (_engine.EstaPausado)
            {
                btnIniciar.Text = "Parar";
                btnIniciar.IsAccent = true;
                btnIniciar.CustomAccentColor = ThemeManager.VermelhoPerigo;
                btnIniciar.CustomTextColor = Color.White;

                btnPausar.Text = "Continuar";
                btnPausar.Enabled = true;
                btnPausar.IsAccent = true;
                btnPausar.CustomAccentColor = ThemeManager.CorAcento;
                btnPausar.CustomTextColor = ThemeManager.CorAcentoTexto;
            }
            else
            {
                btnIniciar.Text = "Parar";
                btnIniciar.IsAccent = true;
                btnIniciar.CustomAccentColor = ThemeManager.VermelhoPerigo;
                btnIniciar.CustomTextColor = Color.White;

                btnPausar.Text = "Pausar";
                btnPausar.Enabled = true;
                btnPausar.IsAccent = false;
                btnPausar.CustomAccentColor = null;
            }
        }

        private void HabilitarSeletores(bool habilitar)
        {
            numCiclos.Enabled = habilitar;
            btnTempo25.Enabled = habilitar;
            btnTempo15.Enabled = habilitar;
            btnTempo05.Enabled = habilitar;
        }

        private void Engine_OnTick(TimeSpan tempoRestante)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;

            if (lblTempo.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => lblTempo.Text = tempoRestante.ToString(@"mm\:ss")));
            }
            else
            {
                lblTempo.Text = tempoRestante.ToString(@"mm\:ss");
            }
        }

        private void Engine_OnEstadoMudou(EstadoPomodoro estadoFinalizado)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => TratarMudancaDeEstado(estadoFinalizado)));
            }
            else
            {
                TratarMudancaDeEstado(estadoFinalizado);
            }
        }

        private void TratarMudancaDeEstado(EstadoPomodoro estadoFinalizado)
        {
            if (estadoFinalizado == EstadoPomodoro.Pausado)
            {
                AtualizarBotoesAcao();
                return;
            }

            HabilitarSeletores(true);
            AtualizarBotoesAcao();
            lblTempo.Text = "00:00";
            lblStatusCiclo.Text = "🎉 Ciclo concluído!";

            try
            {
                SystemSounds.Beep.Play();
            }
            catch { }

            MessageBox.Show($"Tempo finalizado para o ciclo: {estadoFinalizado}!", "Pomodoro", MessageBoxButtons.OK, MessageBoxIcon.Information);
            lblTempo.Text = $"{numCiclos.Value:00}:00";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _engine.Parar();
            base.OnFormClosing(e);
        }
    }
}
