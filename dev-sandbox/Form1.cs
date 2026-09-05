using System;
using System.Windows.Forms;

namespace dev_sandbox
{
    public partial class Form1 : Form
    {
        private readonly PomodoroEngine _engine = new PomodoroEngine();

        public Form1()
        {
            InitializeComponent();

            // Inscreve o Form1 nos eventos disparados pela Engine
            _engine.OnTick += Engine_OnTick;
            _engine.OnEstadoMudou += Engine_OnEstadoMudou;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            lblTempo.Text = $"{numCiclos.Value:00}:00";
        }

        // --- Botões com Minutos Pré-definidos ---

        private void btnTempo25_Click(object sender, EventArgs e)
        {
            IniciarContagemPorMinutos(25);
        }

        private void btnTempo15_Click(object sender, EventArgs e)
        {
            IniciarContagemPorMinutos(15);
        }

        private void btnTempo05_Click(object sender, EventArgs e)
        {
            IniciarContagemPorMinutos(5);
        }



        private void IniciarContagemPorMinutos(int minutos)
        {
            _engine.Parar();
            numCiclos.Value = minutos;
            lblTempo.Text = $"{minutos:00}:00";
            IniciarPomodoro();
        }

        // --- Botão Iniciar / Parar ---

        private void btnIniciar_Click(object sender, EventArgs e)
        {
            if (!_engine.EstaRodando)
            {
                IniciarPomodoro();
            }
            else
            {
                _engine.Parar();
                btnIniciar.Text = "Iniciar";
                HabilitarControles(true);
                lblTempo.Text = $"{numCiclos.Value:00}:00";
            }
        }

        // --- Lógica de Leitura do NumericUpDown e Início da Engine ---

        private void IniciarPomodoro()
        {
            int minutos = (int)numCiclos.Value;

            if (minutos > 0)
            {
                TimeSpan duracao = TimeSpan.FromMinutes(minutos);
                _engine.Iniciar(duracao, EstadoPomodoro.Foco);
                btnIniciar.Text = "Parar";
                HabilitarControles(false);
            }
            else
            {
                MessageBox.Show("Por favor, selecione um tempo maior que 0 minutos.",
                                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void HabilitarControles(bool habilitar)
        {
            numCiclos.Enabled = habilitar;
            btnTempo25.Enabled = habilitar;
            btnTempo15.Enabled = habilitar;
            btnTempo05.Enabled = habilitar;
        }

        // --- Eventos da Engine (Sincronização com a UI) ---

        private void Engine_OnTick(TimeSpan tempoRestante)
        {
            if (lblTempo.InvokeRequired)
            {
                lblTempo.Invoke(new Action(() => lblTempo.Text = tempoRestante.ToString(@"mm\:ss")));
            }
            else
            {
                lblTempo.Text = tempoRestante.ToString(@"mm\:ss");
            }
        }

        private void Engine_OnEstadoMudou(EstadoPomodoro estadoAnterior)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => TratarFimDoTempo()));
            }
            else
            {
                TratarFimDoTempo();
            }
        }

        private void TratarFimDoTempo()
        {
            btnIniciar.Text = "Iniciar";
            HabilitarControles(true);
            lblTempo.Text = $"{numCiclos.Value:00}:00";

            System.Media.SystemSounds.Beep.Play(); // Emite alerta sonoro do Windows
            MessageBox.Show("Tempo finalizado!", "Pomodoro", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}