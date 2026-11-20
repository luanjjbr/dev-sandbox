namespace DevSandbox.Core.Pomodoro
{
    /// <summary>Tipo de transição do ciclo do <see cref="PomodoroEngine"/>.</summary>
    public enum TipoTransicaoPomodoro
    {
        /// <summary>Um ciclo começou (<see cref="PomodoroEngine.Iniciar"/>).</summary>
        Iniciado,

        /// <summary>O ciclo ativo foi pausado.</summary>
        Pausado,

        /// <summary>O ciclo pausado foi retomado.</summary>
        Retomado,

        /// <summary>
        /// O ciclo foi encerrado antes do fim: <see cref="PomodoroEngine.Parar"/> ou um novo
        /// <see cref="PomodoroEngine.Iniciar"/> por cima de um ciclo ainda ativo/pausado.
        /// </summary>
        Parado,

        /// <summary>O ciclo chegou a 00:00.</summary>
        Concluido
    }

    /// <summary>
    /// Transição notificada por <see cref="PomodoroEngine.OnTransicao"/>.
    /// </summary>
    /// <param name="Tipo">O que aconteceu.</param>
    /// <param name="Estado">
    /// Estado do ciclo envolvido: o que começou, foi retomado ou concluiu; em <see cref="TipoTransicaoPomodoro.Pausado"/>,
    /// o estado que foi pausado (ex.: <see cref="EstadoPomodoro.Foco"/>); em <see cref="TipoTransicaoPomodoro.Parado"/>,
    /// o estado no momento da parada (pode ser <see cref="EstadoPomodoro.Pausado"/>).
    /// </param>
    /// <param name="TempoRestante">Tempo restante no momento da transição (zero em <see cref="TipoTransicaoPomodoro.Concluido"/>).</param>
    /// <param name="TempoTotal">Duração total do ciclo.</param>
    /// <param name="Ciclo">
    /// Número do ciclo (cresce a cada <see cref="PomodoroEngine.Iniciar"/>; pausa/retomada mantêm o número). Como a
    /// conclusão é notificada da thread do timer, ela pode chegar DEPOIS do <see cref="TipoTransicaoPomodoro.Iniciado"/>
    /// de um ciclo novo; o número diz a que ciclo cada transição pertence.
    /// </param>
    public sealed record TransicaoPomodoro(
        TipoTransicaoPomodoro Tipo,
        EstadoPomodoro Estado,
        TimeSpan TempoRestante,
        TimeSpan TempoTotal,
        long Ciclo);
}
