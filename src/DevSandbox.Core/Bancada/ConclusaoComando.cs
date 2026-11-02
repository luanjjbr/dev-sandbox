namespace DevSandbox.Core.Bancada
{
    /// <summary>Desfecho de um comando enviado à Bancada (estados visuais da UI, seção 6 de docs/design/bancada-cp.md).</summary>
    public enum ResultadoComando
    {
        /// <summary>Transmitido e confirmado pelo firmware (<c>OK</c> ou o relatório de status pedido).</summary>
        Confirmado,
        /// <summary>Transmitido, mas o firmware respondeu <c>ERRO...</c>.</summary>
        ErroFirmware,
        /// <summary>Não transmitido (sem conexão ou falha de escrita).</summary>
        NaoEnviado,
        /// <summary>Transmitido, mas sem resposta no prazo (ou a conexão caiu antes da resposta).</summary>
        SemResposta
    }

    /// <summary>
    /// Desfecho de um comando. <see cref="Estado"/>/<see cref="Cabo"/> indicam o alvo, quando o comando era
    /// de estado CP ou de cabo PP; <see cref="Resposta"/> é a linha do firmware que concluiu o comando, se houve.
    /// </summary>
    /// <remarks>
    /// É a verdade final sobre o comando: cada comando gera no máximo uma conclusão. O <c>ComandoEnviado</c> do
    /// <c>BancadaEngine</c> pode chegar DEPOIS desta conclusão (a resposta do firmware chega por outra thread).
    /// </remarks>
    public sealed record ConclusaoComando(
        string Comando,
        ResultadoComando Resultado,
        RespostaFirmware? Resposta = null,
        EstadoVeiculoCP? Estado = null,
        EstadoCaboPP? Cabo = null)
    {
        /// <summary>
        /// Número sequencial do comando no <c>BancadaEngine</c> (1, 2, 3, ...; único por instância).
        /// Comandos mais novos têm <c>Id</c> maior: a UI pode ignorar uma conclusão mais velha que a última seleção
        /// (compare com <c>BancadaEngine.UltimoIdComando</c> lido logo após <c>DefinirEstado</c>/<c>DefinirCabo</c>).
        /// </summary>
        public long Id { get; init; }

        /// <summary>Explicação legível do desfecho (ex.: por que não foi enviado); <c>null</c> quando não há.</summary>
        public string? Detalhe { get; init; }
    }
}
