namespace DevSandbox.Core.Pomodoro
{
    /// <summary>
    /// Estatísticas da sessão atual do Pomodoro (só em memória): ciclo da série de 4 focos, tempo total em foco
    /// e interrupções. Alimentada por <see cref="PomodoroEngine.OnTransicao"/>.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><b>Interrupção:</b> Pausar ou Parar durante <see cref="EstadoPomodoro.Foco"/> (inclui um novo Iniciar por
    /// cima de um foco ativo). Parar um foco que já estava pausado não conta de novo.</item>
    /// <item><b>Tempo em foco:</b> tempo efetivamente corrido em Foco (pausas não contam), com a precisão do tick do
    /// motor (50 ms). Durante um foco ativo, o valor inclui o trecho em andamento.</item>
    /// <item><b>Ciclo atual:</b> número (1–4) do foco em andamento ou do próximo foco da série. Depois do 4º foco
    /// concluído fica em 4/4 (descanso longo) até o próximo foco ser iniciado, quando volta a 1/4.</item>
    /// </list>
    /// As transições podem chegar da thread do timer (conclusão); o acesso é protegido por lock. A conclusão de um
    /// foco pode chegar depois do Iniciado do ciclo seguinte: ela é atribuída ao próprio ciclo
    /// (<see cref="TransicaoPomodoro.Ciclo"/>), sem mexer no trecho do ciclo novo.
    /// </remarks>
    public sealed class EstatisticasSessao : IDisposable
    {
        /// <summary>Focos por série (o 4º é seguido do descanso longo).</summary>
        public const int CiclosPorSerie = 4;

        private readonly PomodoroEngine _engine;
        private readonly object _lock = new();

        private int _focosConcluidos;
        private int _interrupcoes;
        private TimeSpan _focoAcumulado;

        // Trecho de foco em andamento: restante quando começou (Iniciar/Retomar); null se não há foco correndo.
        private TimeSpan? _inicioTrechoFoco;
        private long _cicloDoTrecho;

        // Último ciclo iniciado (TransicaoPomodoro.Ciclo).
        private long _ultimoCiclo;

        // Trecho de foco de um ciclo já substituído cuja conclusão ainda não chegou (corrida conclusão × Iniciar).
        private (long Ciclo, TimeSpan Inicio)? _trechoAguardandoConclusao;

        // true depois que um foco foi iniciado e ainda não concluiu (o ciclo exibido é o desse foco).
        private bool _focoIniciadoSemConcluir;
        private bool _disposed;

        /// <summary>Disparado após cada transição que atualiza as estatísticas (pode vir da thread do timer).</summary>
        public event EventHandler? Mudou;

        public EstatisticasSessao(PomodoroEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _engine.OnTransicao += Engine_OnTransicao;
        }

        /// <summary>Focos que chegaram a 00:00 nesta sessão.</summary>
        public int FocosConcluidos
        {
            get { lock (_lock) return _focosConcluidos; }
        }

        /// <summary>Número do ciclo na série (1–<see cref="CiclosPorSerie"/>).</summary>
        public int CicloAtual
        {
            get
            {
                lock (_lock)
                {
                    // Série completa e o próximo foco ainda não começou: mantém 4/4 durante o descanso longo
                    if (_focosConcluidos > 0 && _focosConcluidos % CiclosPorSerie == 0 && !_focoIniciadoSemConcluir)
                        return CiclosPorSerie;

                    return (_focosConcluidos % CiclosPorSerie) + 1;
                }
            }
        }

        /// <summary>Pausas e paradas feitas durante um foco.</summary>
        public int Interrupcoes
        {
            get { lock (_lock) return _interrupcoes; }
        }

        /// <summary>Tempo total corrido em foco nesta sessão, incluindo o trecho em andamento.</summary>
        public TimeSpan TempoEmFoco
        {
            get
            {
                lock (_lock)
                {
                    if (_inicioTrechoFoco is not { } inicio || _engine.EstadoAtual != EstadoPomodoro.Foco)
                        return _focoAcumulado;

                    TimeSpan corrido = inicio - _engine.TempoRestante;
                    return corrido > TimeSpan.Zero ? _focoAcumulado + corrido : _focoAcumulado;
                }
            }
        }

        private void Engine_OnTransicao(TransicaoPomodoro t) => ProcessarTransicao(t);

        /// <summary>Aplica uma transição (exposto aos testes para reproduzir ordens de chegada raras).</summary>
        internal void ProcessarTransicao(TransicaoPomodoro t)
        {
            lock (_lock)
            {
                if (_disposed) return;

                if (t.Ciclo < _ultimoCiclo)
                {
                    // Transição de um ciclo já substituído: só a conclusão atrasada de um foco conta
                    if (t.Tipo != TipoTransicaoPomodoro.Concluido || t.Estado != EstadoPomodoro.Foco ||
                        _trechoAguardandoConclusao is not { } pendente || pendente.Ciclo != t.Ciclo)
                        return;

                    _focoAcumulado += pendente.Inicio; // concluiu: correu do início do trecho até 00:00
                    _focosConcluidos++;
                    _trechoAguardandoConclusao = null;
                }
                else
                {
                    Aplicar(t);
                }
            }

            Mudou?.Invoke(this, EventArgs.Empty);
        }

        // Deve ser chamado com _lock adquirido; t é do ciclo atual (ou de um mais novo).
        private void Aplicar(TransicaoPomodoro t)
        {
                switch (t.Tipo)
                {
                    case TipoTransicaoPomodoro.Iniciado:
                    case TipoTransicaoPomodoro.Retomado:
                        if (t.Tipo == TipoTransicaoPomodoro.Iniciado)
                        {
                            // Foco anterior ainda aberto: a conclusão dele está a caminho (thread do timer)
                            if (_inicioTrechoFoco is { } aberto && _cicloDoTrecho < t.Ciclo)
                                _trechoAguardandoConclusao = (_cicloDoTrecho, aberto);

                            _ultimoCiclo = t.Ciclo;
                            if (t.Estado == EstadoPomodoro.Foco) _focoIniciadoSemConcluir = true;
                        }
                        _inicioTrechoFoco = t.Estado == EstadoPomodoro.Foco ? t.TempoRestante : null;
                        _cicloDoTrecho = t.Ciclo;
                        break;

                    case TipoTransicaoPomodoro.Pausado:
                    case TipoTransicaoPomodoro.Parado:
                        if (t.Estado == EstadoPomodoro.Foco)
                        {
                            FecharTrecho(t.TempoRestante);
                            _interrupcoes++;
                        }
                        _inicioTrechoFoco = null;
                        break;

                    case TipoTransicaoPomodoro.Concluido:
                        if (t.Estado == EstadoPomodoro.Foco)
                        {
                            FecharTrecho(TimeSpan.Zero);
                            _focosConcluidos++;
                            _focoIniciadoSemConcluir = false;
                        }
                        _inicioTrechoFoco = null;
                        break;
                }
        }

        // Deve ser chamado com _lock adquirido.
        private void FecharTrecho(TimeSpan restante)
        {
            if (_inicioTrechoFoco is not { } inicio) return;

            TimeSpan corrido = inicio - restante;
            if (corrido > TimeSpan.Zero) _focoAcumulado += corrido;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
            }

            _engine.OnTransicao -= Engine_OnTransicao;
        }
    }
}
