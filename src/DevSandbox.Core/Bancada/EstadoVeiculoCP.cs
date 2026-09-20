namespace DevSandbox.Core.Bancada
{
    /// <summary>
    /// Representa os estados do veículo elétrico segundo a norma IEC 61851-1
    /// determinados pela tensão da borda positiva do sinal PWM no Pino Piloto de Controle (CP).
    /// </summary>
    public enum EstadoVeiculoCP
    {
        EstadoA,
        EstadoB,
        EstadoC,
        EstadoD,
        EstadoE
    }

    public static class EstadoVeiculoCPExtensions
    {
        public static string ObterDescricao(this EstadoVeiculoCP estado) => estado switch
        {
            EstadoVeiculoCP.EstadoA => "Estado A (+12V) - Desconectado",
            EstadoVeiculoCP.EstadoB => "Estado B (+9V) - Conectado (Standby)",
            EstadoVeiculoCP.EstadoC => "Estado C (+6V) - Carregando",
            EstadoVeiculoCP.EstadoD => "Estado D (+3V) - Carregando c/ Ventilação",
            EstadoVeiculoCP.EstadoE => "Estado E (0V) - Falha ou Curto CP-PE",
            _                       => "Desconhecido"
        };

        public static double ObterTensaoNominal(this EstadoVeiculoCP estado) => estado switch
        {
            EstadoVeiculoCP.EstadoA => 12.0,
            EstadoVeiculoCP.EstadoB => 9.0,
            EstadoVeiculoCP.EstadoC => 6.0,
            EstadoVeiculoCP.EstadoD => 3.0,
            EstadoVeiculoCP.EstadoE => 0.0,
            _                       => 0.0
        };

        public static string ObterCodigo(this EstadoVeiculoCP estado) => estado switch
        {
            EstadoVeiculoCP.EstadoA => "A",
            EstadoVeiculoCP.EstadoB => "B",
            EstadoVeiculoCP.EstadoC => "C",
            EstadoVeiculoCP.EstadoD => "D",
            EstadoVeiculoCP.EstadoE => "E",
            _                       => "?"
        };

        public static string ObterComandoSerial(this EstadoVeiculoCP estado) => $"estado {estado.ObterCodigo().ToLowerInvariant()}";
    }
}

