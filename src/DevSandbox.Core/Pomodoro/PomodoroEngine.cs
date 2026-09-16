using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DevSandbox.Core.Pomodoro
{
    public class PomodoroEngine
    {
        private CancellationTokenSource? _cts;
        private readonly object _lock = new();

        public EstadoPomodoro EstadoAtual { get; private set; } = EstadoPomodoro.Parado;
        public EstadoPomodoro EstadoAnterior { get; private set; } = EstadoPomodoro.Parado;

        public bool EstaRodando => EstadoAtual != EstadoPomodoro.Parado;
        public bool EstaPausado => EstadoAtual == EstadoPomodoro.Pausado;

        public TimeSpan TempoRestante { get; private set; } = TimeSpan.Zero;
        public TimeSpan TempoTotal { get; private set; } = TimeSpan.Zero;

        // Eventos para notificar a interface ou os testes
        public event Action<TimeSpan>? OnTick;
        public event Action<EstadoPomodoro>? OnEstadoMudou;

        public void Iniciar(TimeSpan duracao, EstadoPomodoro estado = EstadoPomodoro.Foco)
        {
            if (duracao <= TimeSpan.Zero)
                throw new ArgumentException("A duração deve ser maior que zero.", nameof(duracao));

            Parar(); // Interrompe qualquer ciclo ativo

            CancellationToken token;
            lock (_lock)
            {
                _cts = new CancellationTokenSource();
                token = _cts.Token;
                EstadoAtual = estado;
                EstadoAnterior = estado;
                TempoTotal = duracao;
                TempoRestante = duracao;
            }

            _ = RodarTimerAsync(duracao, token);
        }

        public bool Pausar()
        {
            CancellationTokenSource? ctsToCancel = null;

            lock (_lock)
            {
                if (EstadoAtual == EstadoPomodoro.Parado || EstadoAtual == EstadoPomodoro.Pausado)
                    return false;

                EstadoAnterior = EstadoAtual;
                EstadoAtual = EstadoPomodoro.Pausado;

                if (_cts != null)
                {
                    ctsToCancel = _cts;
                    _cts = null;
                }
            }

            if (ctsToCancel != null)
            {
                try
                {
                    if (!ctsToCancel.IsCancellationRequested)
                    {
                        ctsToCancel.Cancel();
                    }
                }
                catch (ObjectDisposedException) { }
                finally
                {
                    ctsToCancel.Dispose();
                }
            }

            OnEstadoMudou?.Invoke(EstadoPomodoro.Pausado);
            return true;
        }

        public bool Retomar()
        {
            CancellationToken token;
            TimeSpan duracaoRestante;

            lock (_lock)
            {
                if (EstadoAtual != EstadoPomodoro.Pausado || TempoRestante <= TimeSpan.Zero)
                    return false;

                EstadoAtual = EstadoAnterior;
                duracaoRestante = TempoRestante;

                _cts = new CancellationTokenSource();
                token = _cts.Token;
            }

            _ = RodarTimerAsync(duracaoRestante, token);
            return true;
        }

        public void Parar()
        {
            CancellationTokenSource? ctsToCancel = null;

            lock (_lock)
            {
                if (_cts != null)
                {
                    ctsToCancel = _cts;
                    _cts = null;
                }
                EstadoAtual = EstadoPomodoro.Parado;
                EstadoAnterior = EstadoPomodoro.Parado;
                TempoRestante = TimeSpan.Zero;
            }

            if (ctsToCancel != null)
            {
                try
                {
                    if (!ctsToCancel.IsCancellationRequested)
                    {
                        ctsToCancel.Cancel();
                    }
                }
                catch (ObjectDisposedException) { }
                finally
                {
                    ctsToCancel.Dispose();
                }
            }
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
                    if (restante < TimeSpan.Zero) restante = TimeSpan.Zero;

                    lock (_lock)
                    {
                        TempoRestante = restante;
                    }

                    OnTick?.Invoke(restante);

                    await Task.Delay(50, token);
                }

                if (!token.IsCancellationRequested)
                {
                    lock (_lock)
                    {
                        TempoRestante = TimeSpan.Zero;
                    }

                    // Garante que o último tick notificado seja 00:00
                    OnTick?.Invoke(TimeSpan.Zero);

                    EstadoPomodoro estadoFinalizado;
                    lock (_lock)
                    {
                        estadoFinalizado = EstadoAtual;
                        EstadoAtual = EstadoPomodoro.Parado;
                        _cts?.Dispose();
                        _cts = null;
                    }

                    OnEstadoMudou?.Invoke(estadoFinalizado);
                }
            }
            catch (OperationCanceledException)
            {
                // Tratamento gracioso ao cancelar a Task
            }
            catch (ObjectDisposedException)
            {
                // Tratamento gracioso caso o token tenha sido descartado
            }
        }
    }
}

