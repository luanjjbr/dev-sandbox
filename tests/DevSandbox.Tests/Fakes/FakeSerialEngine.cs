using DevSandbox.Core.Serial;

namespace DevSandbox.Tests.Fakes
{
    /// <summary>
    /// Dublê de <see cref="ISerialEngine"/> para testar o <c>BancadaEngine</c> sem hardware.
    /// </summary>
    internal sealed class FakeSerialEngine : ISerialEngine
    {
        private readonly List<string> _comandosTransmitidos = [];

        public bool EstaConectado { get; set; }
        public bool TentandoReconectar { get; set; }
        public bool Descartado { get; private set; }

        /// <summary>Quando <c>true</c>, <see cref="EnviarDados"/> simula falha de escrita (retorna <c>false</c>).</summary>
        public bool FalharEnvio { get; set; }

        public IReadOnlyList<string> ComandosTransmitidos => _comandosTransmitidos;

        public event EventHandler<string>? DadosRecebidos;
        public event EventHandler? ConexaoPerdida;
        public event EventHandler? Reconectado;

        public int TotalAssinantes =>
            (DadosRecebidos?.GetInvocationList().Length ?? 0) +
            (ConexaoPerdida?.GetInvocationList().Length ?? 0) +
            (Reconectado?.GetInvocationList().Length ?? 0);

        public string[] ObterPortasDisponiveis() => ["COM9"];

        public void Conectar(string nomePorta, int baudRate) => EstaConectado = true;

        public void Desconectar() => EstaConectado = false;

        public bool EnviarDados(string texto, bool adicionarNovaLinha = true)
        {
            if (!EstaConectado || string.IsNullOrEmpty(texto) || FalharEnvio) return false;
            _comandosTransmitidos.Add(texto);
            return true;
        }

        public void SimularRecepcao(string dados) => DadosRecebidos?.Invoke(this, dados);

        public void SimularDesconexao()
        {
            EstaConectado = false;
            TentandoReconectar = true;
            ConexaoPerdida?.Invoke(this, EventArgs.Empty);
        }

        public void SimularReconexao()
        {
            EstaConectado = true;
            TentandoReconectar = false;
            Reconectado?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose() => Descartado = true;
    }
}
