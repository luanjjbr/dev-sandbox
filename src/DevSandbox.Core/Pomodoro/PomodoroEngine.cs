namespace DevSandbox.Core.Pomodoro
{
    /// <summary>
    /// Motor do cronômetro Pomodoro. O relógio é injetável (<see cref="TimeProvider"/>) para testes determinísticos,
    /// e as notificações isolam exceções dos assinantes para não interromper o laço do cronômetro.
    /// </summary>
    public sealed class PomodoroEngine : IDisposable
    {
        private static readonly TimeSpan IntervaloTick = TimeSpan.FromMilliseconds(50);

        private readonly TimeProvider _timeProvider;
        private readonly object _lock = new();
        private CancellationTokenSource? _cts;
        private bool _disposed;

        public EstadoPomodoro EstadoAtual { get; private set; } = EstadoPomodoro.Parado;
        public EstadoPomodoro EstadoAnterior { get; private set; } = EstadoPomodoro.Parado;

        public bool EstaRodando => EstadoAtual != EstadoPomodoro.Parado;
        public bool EstaPausado => EstadoAtual == EstadoPomodoro.Pausado;

        public TimeSpan TempoRestante { get; private set; } = TimeSpan.Zero;
        public TimeSpan TempoTotal { get; private set; } = TimeSpan.Zero;

        // Eventos para notificar a interface ou os testes
        public event Action<TimeSpan>? OnTick;
        public event Action<EstadoPomodoro>? OnEstadoMudou;

        public PomodoroEngine(TimeProvider? timeProvider = null)
        {
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public void Iniciar(TimeSpan duracao, EstadoPomodoro estado = EstadoPomodoro.Foco)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (duracao <= TimeSpan.Zero)
                throw new ArgumentException("A duração deve ser maior que zero.", nameof(duracao));

            CancellationToken token;
            CancellationTokenSource? ctsAnterior;
            lock (_lock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                // Troca atômica de ciclo: o anterior perde a posse no mesmo lock em que o novo é criado
                ctsAnterior = _cts;
                _cts = new CancellationTokenSource();
                token = _cts.Token;
                EstadoAtual = estado;
                EstadoAnterior = estado;
                TempoTotal = duracao;
                TempoRestante = duracao;
            }

            CancelarEDescartar(ctsAnterior);
            _ = RodarTimerAsync(duracao, token);
        }

        public bool Pausar()
        {
            CancellationTokenSource? ctsToCancel;

            lock (_lock)
            {
                if (EstadoAtual == EstadoPomodoro.Parado || EstadoAtual == EstadoPomodoro.Pausado)
                    return false;

                EstadoAnterior = EstadoAtual;
                EstadoAtual = EstadoPomodoro.Pausado;

                ctsToCancel = _cts;
                _cts = null;
            }

            CancelarEDescartar(ctsToCancel);
            NotificarEstadoMudou(EstadoPomodoro.Pausado);
            return true;
        }

        public bool Retomar()
        {
            CancellationToken token;
            TimeSpan duracaoRestante;

            lock (_lock)
            {
                if (_disposed || EstadoAtual != EstadoPomodoro.Pausado || TempoRestante <= TimeSpan.Zero)
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
            CancellationTokenSource? ctsToCancel;

            lock (_lock)
            {
                ctsToCancel = _cts;
                _cts = null;
                EstadoAtual = EstadoPomodoro.Parado;
                EstadoAnterior = EstadoPomodoro.Parado;
                TempoRestante = TimeSpan.Zero;
            }

            CancelarEDescartar(ctsToCancel);
        }

        private static void CancelarEDescartar(CancellationTokenSource? cts)
        {
            if (cts == null) return;

            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException) { }
            finally
            {
                cts.Dispose();
            }
        }

        private async Task RodarTimerAsync(TimeSpan duracao, CancellationToken token)
        {
            long inicio = _timeProvider.GetTimestamp();

            try
            {
                while (true)
                {
                    if (token.IsCancellationRequested) return;

                    TimeSpan decorrido = _timeProvider.GetElapsedTime(inicio);
                    if (decorrido >= duracao) break;

                    TimeSpan restante = duracao - decorrido;

                    lock (_lock)
                    {
                        if (!EhCicloAtivo(token)) return;
                        TempoRestante = restante;
                    }

                    NotificarTick(restante);

                    TimeSpan espera = restante < IntervaloTick ? restante : IntervaloTick;
                    await Task.Delay(espera, _timeProvider, token).ConfigureAwait(false);
                }

                // Finalização num lock único: um Pausar/Parar/Iniciar concorrente ou acontece antes
                // (e este laço deixa de ser o ciclo ativo) ou depois (e encontra o motor já Parado).
                EstadoPomodoro estadoFinalizado;
                CancellationTokenSource ctsFinalizado;
                lock (_lock)
                {
                    if (!EhCicloAtivo(token)) return;

                    TempoRestante = TimeSpan.Zero;
                    estadoFinalizado = EstadoAtual;
                    EstadoAtual = EstadoPomodoro.Parado;
                    ctsFinalizado = _cts!;
                    _cts = null;
                }

                ctsFinalizado.Dispose();

                // Fora do lock: o último tick notificado é 00:00 e vem antes da mudança de estado
                NotificarTick(TimeSpan.Zero);
                NotificarEstadoMudou(estadoFinalizado);
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

        // Deve ser chamado com _lock adquirido.
        private bool EhCicloAtivo(CancellationToken token) =>
            !token.IsCancellationRequested && _cts != null && _cts.Token == token;

        // Cada assinante é isolado: a exceção de um não impede os demais nem derruba o cronômetro.
        private void NotificarTick(TimeSpan restante)
        {
            Action<TimeSpan>? handlers = OnTick;
            if (handlers == null) return;

            foreach (Action<TimeSpan> handler in handlers.GetInvocationList().Cast<Action<TimeSpan>>())
            {
                try { handler(restante); }
                catch (Exception) { /* isola falhas do assinante */ }
            }
        }

        private void NotificarEstadoMudou(EstadoPomodoro estado)
        {
            Action<EstadoPomodoro>? handlers = OnEstadoMudou;
            if (handlers == null) return;

            foreach (Action<EstadoPomodoro> handler in handlers.GetInvocationList().Cast<Action<EstadoPomodoro>>())
            {
                try { handler(estado); }
                catch (Exception) { /* isola falhas do assinante */ }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Parar();
        }
    }
}
