using DevSandbox.Core.Serial;

namespace DevSandbox.Tests.Fakes
{
    /// <summary>
    /// Dublê de <see cref="ISerialEngine"/> para testar o <c>BancadaEngine</c> sem hardware.
    /// </summary>
    internal sealed class FakeSerialEngine : ISerialEngine
    {
        private readonly List<string> _comandosTransmitidos = [];
        private readonly List<FimDeLinha> _finsDeLinha = [];

        public bool EstaConectado { get; set; }
        public bool TentandoReconectar { get; set; }
        public bool Descartado { get; private set; }

        /// <summary>Quando <c>true</c>, <see cref="EnviarDados"/> simula falha de escrita (retorna <c>false</c>).</summary>
        public bool FalharEnvio { get; set; }

        public IReadOnlyList<string> ComandosTransmitidos => _comandosTransmitidos;

        /// <summary>Terminador usado em cada item de <see cref="ComandosTransmitidos"/> (mesmo índice).</summary>
        public IReadOnlyList<FimDeLinha> FinsDeLinha => _finsDeLinha;

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

        public bool EnviarDados(string texto, FimDeLinha fimDeLinha)
        {
            // Como o motor real: vazio só sai com terminador
            if (!EstaConectado || FalharEnvio || (string.IsNullOrEmpty(texto) && fimDeLinha == FimDeLinha.Nenhum)) return false;
            _comandosTransmitidos.Add(texto);
            _finsDeLinha.Add(fimDeLinha);
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
