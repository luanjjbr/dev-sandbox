namespace DevSandbox.Core.Bancada
{
    /// <summary>
    /// Representa os canais de resistores do Pino de Proximidade (PP - Proximity Pilot)
    /// conforme a norma IEC 62196-2 e a lógica da Bancada.
    /// </summary>
    public enum EstadoCaboPP
    {
        CircuitoAberto = 0,
        Desconectado = 1,
        Cabo13A = 2,
        Cabo20A = 3,
        Cabo32A = 4,
        Cabo63A = 5,
        FalhaCurto = 6
    }

    public static class EstadoCaboPPExtensions
    {
        public static string ObterDescricao(this EstadoCaboPP cabo) => cabo switch
        {
            EstadoCaboPP.CircuitoAberto => "Circuito Aberto (Nenhum cabo)",
            EstadoCaboPP.Desconectado   => "Desconectado (4700 Ω)",
            EstadoCaboPP.Cabo13A        => "Cabo 13 A (1500 Ω)",
            EstadoCaboPP.Cabo20A        => "Cabo 20 A (680 Ω)",
            EstadoCaboPP.Cabo32A        => "Cabo 32 A (220 Ω)",
            EstadoCaboPP.Cabo63A        => "Cabo 63 A (100 Ω)",
            EstadoCaboPP.FalhaCurto     => "Falha / Curto-circuito (47 Ω)",
            _                           => "Desconhecido"
        };

        public static int ObterResistenciaNominal(this EstadoCaboPP cabo) => cabo switch
        {
            EstadoCaboPP.CircuitoAberto => -1,
            EstadoCaboPP.Desconectado   => 4700,
            EstadoCaboPP.Cabo13A        => 1500,
            EstadoCaboPP.Cabo20A        => 680,
            EstadoCaboPP.Cabo32A        => 220,
            EstadoCaboPP.Cabo63A        => 100,
            EstadoCaboPP.FalhaCurto     => 47,
            _                           => 0
        };

        public static string ObterComandoSerial(this EstadoCaboPP cabo) => $"cabo {(int)cabo}";
    }
}

