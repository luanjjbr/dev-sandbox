using System;
using System.IO.Ports;
using System.Threading;

namespace DevSandbox.Core.Serial
{
    public class SupervisorioSerialEngine : IDisposable
    {
        private SerialPort? _portaSerial;
        private System.Timers.Timer? _watchdogTimer;
        private readonly object _lock = new();
        private bool _disposed = false;

        private string? _ultimaPorta;
        private int _ultimoBaudRate;
        private bool _tentandoReconectar = false;

        // Eventos para notificar a interface sobre o status da conexão
        public event EventHandler<string>? DadosRecebidos;
        public event EventHandler? ConexaoPerdida;
        public event EventHandler? Reconectado;

        public bool EstaConectado => _portaSerial != null && _portaSerial.IsOpen;
        public bool TentandoReconectar => _tentandoReconectar;

        // Retorna a lista de portas COM disponíveis no Windows (ex: COM1, COM6)
        public string[] ObterPortasDisponiveis() => SerialPort.GetPortNames();

        // Conecta à porta serial especificada e inicia o monitoramento de integridade
        public void Conectar(string nomePorta, int baudRate)
        {
            if (string.IsNullOrWhiteSpace(nomePorta))
                throw new ArgumentException("Selecione uma porta COM válida.", nameof(nomePorta));

            if (baudRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(baudRate), "O Baud Rate deve ser maior que zero.");

            lock (_lock)
            {
                _ultimaPorta = nomePorta;
                _ultimoBaudRate = baudRate;
                _tentandoReconectar = false;

                AbrirPortaInterno();
                IniciarWatchdog();
            }
        }

        private void AbrirPortaInterno()
        {
            FecharPortaInterno();

            SerialPort? porta = null;
            try
            {
                porta = new SerialPort(_ultimaPorta!, _ultimoBaudRate)
                {
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };
                porta.DataReceived += PortaSerial_DataReceived;
                porta.Open();
                _portaSerial = porta;
            }
            catch
            {
                if (porta != null)
                {
                    try { porta.DataReceived -= PortaSerial_DataReceived; } catch { }
                    try { if (porta.IsOpen) porta.Close(); } catch { }
                    try { porta.Dispose(); } catch { }
                }
                _portaSerial = null;
                throw;
            }
        }

        private void FecharPortaInterno()
        {
            if (_portaSerial != null)
            {
                try
                {
                    _portaSerial.DataReceived -= PortaSerial_DataReceived;
                    if (_portaSerial.IsOpen)
                    {
                        _portaSerial.Close();
                    }
                }
                catch { }

                try
                {
                    _portaSerial.Dispose();
                }
                catch { }

                _portaSerial = null;
            }
        }

        // Desconecta a porta e interrompe o Watchdog
        public void Desconectar()
        {
            lock (_lock)
            {
                PararWatchdog();
                _tentandoReconectar = false;
                FecharPortaInterno();
            }
        }

        // Transmite texto para a porta serial
        public void EnviarDados(string texto, bool adicionarNovaLinha = true)
        {
            lock (_lock)
            {
                if (EstaConectado && !string.IsNullOrEmpty(texto))
                {
                    if (adicionarNovaLinha)
                        _portaSerial!.WriteLine(texto);
                    else
                        _portaSerial!.Write(texto);
                }
            }
        }

        private void IniciarWatchdog()
        {
            PararWatchdog();

            _watchdogTimer = new System.Timers.Timer(1000)
            {
                AutoReset = true
            };
            _watchdogTimer.Elapsed += WatchdogTimer_Elapsed;
            _watchdogTimer.Start();
        }

        private void PararWatchdog()
        {
            if (_watchdogTimer != null)
            {
                _watchdogTimer.Stop();
                _watchdogTimer.Elapsed -= WatchdogTimer_Elapsed;
                _watchdogTimer.Dispose();
                _watchdogTimer = null;
            }
        }

        private void WatchdogTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (_disposed) return;

            // Garante que apenas um ciclo do watchdog execute por vez
            if (Monitor.TryEnter(_lock))
            {
                try
                {
                    if (_disposed || _watchdogTimer == null) return;

                    // Detecta se a porta foi fechada repentinamente (ex: remoção do cabo USB)
                    if (_portaSerial != null && !_portaSerial.IsOpen && !_tentandoReconectar)
                    {
                        _tentandoReconectar = true;
                        FecharPortaInterno();
                        ConexaoPerdida?.Invoke(this, EventArgs.Empty);
                    }

                    if (_tentandoReconectar)
                    {
                        TentarReconectarInterno();
                    }
                }
                finally
                {
                    Monitor.Exit(_lock);
                }
            }
        }

        private void TentarReconectarInterno()
        {
            try
            {
                string[] portas = ObterPortasDisponiveis();
                bool portaEncontrada = Array.Exists(portas, p => p.Equals(_ultimaPorta, StringComparison.OrdinalIgnoreCase));

                if (portaEncontrada)
                {
                    AbrirPortaInterno();
                    _tentandoReconectar = false;
                    Reconectado?.Invoke(this, EventArgs.Empty);
                }
            }
            catch
            {
                // Falha nesta tentativa; mantém status até a porta ser reestabelecida
            }
        }

        private void PortaSerial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_disposed) return;

            try
            {
                if (_portaSerial != null && _portaSerial.IsOpen)
                {
                    // Leitura não bloqueante de todo o buffer disponível
                    string dados = _portaSerial.ReadExisting();
                    if (!string.IsNullOrEmpty(dados))
                    {
                        DadosRecebidos?.Invoke(this, dados);
                    }
                }
            }
            catch
            {
                lock (_lock)
                {
                    if (!_disposed && _watchdogTimer != null && !_tentandoReconectar)
                    {
                        _tentandoReconectar = true;
                        FecharPortaInterno();
                        ConexaoPerdida?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Desconectar();
            GC.SuppressFinalize(this);
        }
    }
}
