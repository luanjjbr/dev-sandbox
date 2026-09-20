using System;
using DevSandbox.Core.Serial;

namespace DevSandbox.Core.Bancada
{
    /// <summary>
    /// Motor de controle e comunicação da Bancada de Testes de Veículo Elétrico (IEC 61851-1 / IEC 62196-2).
    /// Gerencia o envio de comandos seriais para o emulador físico (Arduino).
    /// </summary>
    public class BancadaEngine : IDisposable
    {
        private readonly SupervisorioSerialEngine _serialEngine;
        private bool _disposed = false;

        public EstadoCaboPP CaboAtual { get; private set; } = EstadoCaboPP.CircuitoAberto;
        public EstadoVeiculoCP EstadoAtual { get; private set; } = EstadoVeiculoCP.EstadoA;

        public bool EstaConectado => _serialEngine.EstaConectado;
        public bool TentandoReconectar => _serialEngine.TentandoReconectar;

        public event EventHandler<string>? DadosRecebidos;
        public event EventHandler<string>? ComandoEnviado;
        public event EventHandler? ConexaoPerdida;
        public event EventHandler? Reconectado;
        public event EventHandler<EstadoCaboPP>? CaboAlterado;
        public event EventHandler<EstadoVeiculoCP>? EstadoVeiculoAlterado;

        public BancadaEngine() : this(new SupervisorioSerialEngine())
        {
        }

        public BancadaEngine(SupervisorioSerialEngine serialEngine)
        {
            _serialEngine = serialEngine ?? throw new ArgumentNullException(nameof(serialEngine));

            _serialEngine.DadosRecebidos += (s, dados) => DadosRecebidos?.Invoke(this, dados);
            _serialEngine.ConexaoPerdida += (s, e) => ConexaoPerdida?.Invoke(this, e);
            _serialEngine.Reconectado += (s, e) => Reconectado?.Invoke(this, e);
        }

        public string[] ObterPortasDisponiveis() => _serialEngine.ObterPortasDisponiveis();

        public void Conectar(string nomePorta, int baudRate = 9600)
        {
            _serialEngine.Conectar(nomePorta, baudRate);
        }

        public void Desconectar()
        {
            _serialEngine.Desconectar();
        }

        public void DefinirCabo(EstadoCaboPP cabo)
        {
            CaboAtual = cabo;
            string cmd = cabo.ObterComandoSerial();
            EnviarComando(cmd);
            CaboAlterado?.Invoke(this, cabo);
        }

        public void DefinirEstado(EstadoVeiculoCP estado)
        {
            EstadoAtual = estado;
            string cmd = estado.ObterComandoSerial();
            EnviarComando(cmd);
            EstadoVeiculoAlterado?.Invoke(this, estado);
        }

        public void EnviarComandoManual(string comando)
        {
            if (string.IsNullOrWhiteSpace(comando)) return;
            EnviarComando(comando.Trim());
        }

        private void EnviarComando(string cmd)
        {
            if (_serialEngine.EstaConectado)
            {
                _serialEngine.EnviarDados(cmd);
            }
            ComandoEnviado?.Invoke(this, cmd);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _serialEngine.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}

