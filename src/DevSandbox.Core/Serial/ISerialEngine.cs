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
        /// Transmite o texto. Retorna <c>true</c> só quando a escrita ocorreu; <c>false</c> se não há conexão,
        /// se o texto é vazio ou se a escrita falhou (nesse caso dispara <c>ConexaoPerdida</c> e entra em reconexão).
        /// </summary>
        bool EnviarDados(string texto, bool adicionarNovaLinha = true);
    }
}
