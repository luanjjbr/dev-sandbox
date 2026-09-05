using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace dev_sandbox
{
    public enum EstadoPomodoro
    {
        Parado,
        Foco,
        Descanso
    }

    public class PomodoroEngine
    {
        private CancellationTokenSource _cts;

        public EstadoPomodoro EstadoAtual { get; private set; } = EstadoPomodoro.Parado;
        public bool EstaRodando => EstadoAtual != EstadoPomodoro.Parado;

        // Eventos para notificar a interface ou os testes
        public event Action<TimeSpan> OnTick;
        public event Action<EstadoPomodoro> OnEstadoMudou;

        public void Iniciar(TimeSpan duracao, EstadoPomodoro estado)
        {
            Parar(); // Interrompe qualquer execução ativa antes de iniciar uma nova

            _cts = new CancellationTokenSource();
            EstadoAtual = estado;

            _ = RodarTimerAsync(duracao, _cts.Token);
        }

        public void Parar()
        {
            if (_cts != null)
            {
                if (!_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                }
                _cts.Dispose();
                _cts = null;
            }

            EstadoAtual = EstadoPomodoro.Parado;
        }

        private async Task RodarTimerAsync(TimeSpan duracao, CancellationToken token)
        {
            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                while (sw.Elapsed < duracao)
                {
                    if (token.IsCancellationRequested) return;

                    TimeSpan restante = duracao - sw.Elapsed;
                    OnTick?.Invoke(restante);

                    // Intervalo de 50ms para garantir alta precisão e compatibilidade com testes curtos
                    await Task.Delay(50, token);
                }

                // Finalização do ciclo
                EstadoPomodoro estadoFinalizado = EstadoAtual;
                Parar(); // Reseta o estado para Parado

                OnEstadoMudou?.Invoke(estadoFinalizado);
            }
            catch (TaskCanceledException)
            {
                // Tratamento gracioso ao cancelar a Task
            }
        }
    }
}