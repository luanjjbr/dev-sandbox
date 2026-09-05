using System;
using System.IO.Ports;

namespace dev_sandbox.Core
{
    public class SupervisorioSerialEngine
    {
        private SerialPort _portaSerial;
        private System.Timers.Timer _watchdogTimer;

        private string _ultimaPorta;
        private int _ultimoBaudRate;
        private bool _tentandoReconectar = false;

        // Eventos para notificar a interface sobre o status da conexão
        public event EventHandler<string> DadosRecebidos;
        public event EventHandler ConexaoPerdida;
        public event EventHandler Reconectado;

        public bool EstaConectado => _portaSerial != null && _portaSerial.IsOpen;
        public bool TentandoReconectar => _tentandoReconectar;

        // Retorna a lista de portas COM disponíveis no Windows (ex: COM1, COM6)
        public string[] ObterPortasDisponiveis() => SerialPort.GetPortNames();

        // Conecta à porta serial especificada e inicia o monitoramento de saúde
        public void Conectar(string nomePorta, int baudRate)
        {
            if (string.IsNullOrWhiteSpace(nomePorta))
                throw new ArgumentException("Selecione uma porta COM válida.");

            _ultimaPorta = nomePorta;
            _ultimoBaudRate = baudRate;

            AbrirPorta();
            IniciarWatchdog();
        }

        private void AbrirPorta()
        {
            if (EstaConectado) Desconectar();

            _portaSerial = new SerialPort(_ultimaPorta, _ultimoBaudRate);
            _portaSerial.DataReceived += PortaSerial_DataReceived;
            _portaSerial.Open();
        }

        // Desconecta a porta e interrompe o Watchdog
        public void Desconectar()
        {
            PararWatchdog();
            _tentandoReconectar = false;

            if (_portaSerial != null)
            {
                try
                {
                    if (_portaSerial.IsOpen)
                    {
                        _portaSerial.DataReceived -= PortaSerial_DataReceived;
                        _portaSerial.Close();
                    }
                }
                catch { }

                _portaSerial.Dispose();
                _portaSerial = null;
            }
        }

        // Transmite texto para a porta serial
        public void EnviarDados(string texto)
        {
            if (EstaConectado && !string.IsNullOrEmpty(texto))
            {
                _portaSerial.WriteLine(texto);
            }
        }

        private void IniciarWatchdog()
        {
            _watchdogTimer = new System.Timers.Timer(1000); // Checa a conexão a cada 1 segundo
            _watchdogTimer.Elapsed += WatchdogTimer_Elapsed;
            _watchdogTimer.AutoReset = true;
            _watchdogTimer.Start();
        }

        private void PararWatchdog()
        {
            if (_watchdogTimer != null)
            {
                _watchdogTimer.Stop();
                _watchdogTimer.Dispose();
                _watchdogTimer = null;
            }
        }

        private void WatchdogTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Detecta se a porta foi fechada repentinamente (ex: remoção do cabo USB)
            if (_portaSerial != null && !_portaSerial.IsOpen && !_tentandoReconectar)
            {
                _tentandoReconectar = true;
                ConexaoPerdida?.Invoke(this, EventArgs.Empty);
            }

            // Tenta reconectar ciclicamente
            if (_tentandoReconectar)
            {
                TentarReconectar();
            }
        }

        private void TentarReconectar()
        {
            try
            {
                // Verifica se o SO voltou a reconhecer a porta COM
                string[] portas = ObterPortasDisponiveis();
                bool portaEncontrada = Array.Exists(portas, p => p.Equals(_ultimaPorta, StringComparison.OrdinalIgnoreCase));

                if (portaEncontrada)
                {
                    AbrirPorta();
                    _tentandoReconectar = false;
                    Reconectado?.Invoke(this, EventArgs.Empty);
                }
            }
            catch
            {
                // Mantém a busca até a porta reaparecer
            }
        }

        private void PortaSerial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_portaSerial != null && _portaSerial.IsOpen)
                {
                    string linha = _portaSerial.ReadLine();
                    DadosRecebidos?.Invoke(this, linha);
                }
            }
            catch
            {
                // Falha na leitura dispara o estado de conexão perdida
                if (!_tentandoReconectar)
                {
                    _tentandoReconectar = true;
                    ConexaoPerdida?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}