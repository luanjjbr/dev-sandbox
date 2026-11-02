using DevSandbox.Core.Serial;

namespace DevSandbox.Core.Bancada
{
    /// <summary>
    /// Motor de controle e comunicação da Bancada de Testes de Veículo Elétrico (IEC 61851-1 / IEC 62196-2).
    /// Gerencia o envio de comandos seriais para o emulador físico (Arduino).
    /// </summary>
    public sealed class BancadaEngine : IDisposable
    {
        private readonly ISerialEngine _serialEngine;
        private readonly bool _descartarSerial;
        private bool _disposed;

        public EstadoCaboPP CaboAtual { get; private set; } = EstadoCaboPP.CircuitoAberto;
        public EstadoVeiculoCP EstadoAtual { get; private set; } = EstadoVeiculoCP.EstadoA;

        public bool EstaConectado => _serialEngine.EstaConectado;
        public bool TentandoReconectar => _serialEngine.TentandoReconectar;

        public event EventHandler<string>? DadosRecebidos;

        /// <summary>Disparado somente quando o comando foi efetivamente transmitido pela serial.</summary>
        public event EventHandler<string>? ComandoEnviado;
        public event EventHandler? ConexaoPerdida;
        public event EventHandler? Reconectado;
        public event EventHandler<EstadoCaboPP>? CaboAlterado;
        public event EventHandler<EstadoVeiculoCP>? EstadoVeiculoAlterado;

        public BancadaEngine() : this(new SupervisorioSerialEngine())
        {
        }

        /// <param name="serialEngine">Motor serial (real ou dublê de teste).</param>
        /// <param name="descartarSerial">Se <c>true</c> (padrão), o <see cref="Dispose"/> descarta também o motor serial.</param>
        public BancadaEngine(ISerialEngine serialEngine, bool descartarSerial = true)
        {
            _serialEngine = serialEngine ?? throw new ArgumentNullException(nameof(serialEngine));
            _descartarSerial = descartarSerial;

            _serialEngine.DadosRecebidos += Serial_DadosRecebidos;
            _serialEngine.ConexaoPerdida += Serial_ConexaoPerdida;
            _serialEngine.Reconectado += Serial_Reconectado;
        }

        public string[] ObterPortasDisponiveis() => _serialEngine.ObterPortasDisponiveis();

        public void Conectar(string nomePorta, int baudRate = 9600)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _serialEngine.Conectar(nomePorta, baudRate);
        }

        public void Desconectar()
        {
            _serialEngine.Desconectar();
        }

        /// <summary>
        /// Seleciona o cabo PP. O estado local e <see cref="CaboAlterado"/> são atualizados mesmo sem conexão;
        /// o retorno indica se o comando foi transmitido.
        /// </summary>
        public bool DefinirCabo(EstadoCaboPP cabo)
        {
            CaboAtual = cabo;
            bool enviado = EnviarComando(cabo.ObterComandoSerial());
            CaboAlterado?.Invoke(this, cabo);
            return enviado;
        }

        /// <summary>
        /// Seleciona o estado CP do veículo. O estado local e <see cref="EstadoVeiculoAlterado"/> são atualizados
        /// mesmo sem conexão; o retorno indica se o comando foi transmitido.
        /// </summary>
        public bool DefinirEstado(EstadoVeiculoCP estado)
        {
            EstadoAtual = estado;
            bool enviado = EnviarComando(estado.ObterComandoSerial());
            EstadoVeiculoAlterado?.Invoke(this, estado);
            return enviado;
        }

        /// <summary>Envia um comando livre. Retorna <c>true</c> se foi transmitido.</summary>
        public bool EnviarComandoManual(string comando)
        {
            if (string.IsNullOrWhiteSpace(comando)) return false;
            return EnviarComando(comando.Trim());
        }

        private bool EnviarComando(string cmd)
        {
            if (_disposed || !_serialEngine.EstaConectado || !_serialEngine.EnviarDados(cmd))
                return false;

            ComandoEnviado?.Invoke(this, cmd);
            return true;
        }

        private void Serial_DadosRecebidos(object? sender, string dados) => DadosRecebidos?.Invoke(this, dados);
        private void Serial_ConexaoPerdida(object? sender, EventArgs e) => ConexaoPerdida?.Invoke(this, e);
        private void Serial_Reconectado(object? sender, EventArgs e) => Reconectado?.Invoke(this, e);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _serialEngine.DadosRecebidos -= Serial_DadosRecebidos;
            _serialEngine.ConexaoPerdida -= Serial_ConexaoPerdida;
            _serialEngine.Reconectado -= Serial_Reconectado;

            if (_descartarSerial)
            {
                _serialEngine.Dispose();
            }
        }
    }
}
