namespace DevSandbox.Core.Serial
{
    /// <summary>
    /// Contrato de alto nível do motor serial resiliente (conexão, watchdog e reconexão).
    /// Permite substituir o motor real por um dublê nos testes do <c>BancadaEngine</c> e das telas.
    /// </summary>
    public interface ISerialEngine : IDisposable
    {
        bool EstaConectado { get; }
        bool TentandoReconectar { get; }

        event EventHandler<string>? DadosRecebidos;
        event EventHandler? ConexaoPerdida;
        event EventHandler? Reconectado;

        string[] ObterPortasDisponiveis();
        void Conectar(string nomePorta, int baudRate);
        void Desconectar();

        /// <summary>
        /// Transmite o texto seguido do terminador <paramref name="fimDeLinha"/>. Texto vazio envia só o terminador
        /// (CR/LF/CRLF). Retorna <c>true</c> só quando a escrita ocorreu; <c>false</c> se não há conexão, se não há
        /// nada a enviar (texto vazio com <see cref="FimDeLinha.Nenhum"/>) ou se a escrita falhou (nesse caso
        /// dispara <c>ConexaoPerdida</c> e entra em reconexão).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="fimDeLinha"/> fora do enum.</exception>
        bool EnviarDados(string texto, FimDeLinha fimDeLinha);

        /// <summary>
        /// Compatibilidade: <c>true</c> envia com <see cref="FimDeLinha.LF"/>, <c>false</c> com
        /// <see cref="FimDeLinha.Nenhum"/>. Texto vazio continua retornando <c>false</c>.
        /// </summary>
        bool EnviarDados(string texto, bool adicionarNovaLinha = true) =>
            !string.IsNullOrEmpty(texto) && EnviarDados(texto, adicionarNovaLinha ? FimDeLinha.LF : FimDeLinha.Nenhum);
    }
}
