using System;
using System.IO.Ports;

namespace dev_sandbox.Core
{
    public class SupervisorioSerialEngine
    {
        private SerialPort _portaSerial;

        // Evento para notificar a UI quando uma nova mensagem chegar da serial
        public event EventHandler<string> DadosRecebidos;

        // Indica se a porta está aberta no momento
        public bool EstaConectado => _portaSerial != null && _portaSerial.IsOpen;

        // Retorna a lista de portas COM disponíveis no Windows (ex: COM1, COM3)
        public string[] ObterPortasDisponiveis()
        {
            return SerialPort.GetPortNames();
        }

        // Tenta abrir a porta com a velocidade selecionada
        public void Conectar(string nomePorta, int baudRate)
        {
            if (string.IsNullOrWhiteSpace(nomePorta))
                throw new ArgumentException("Selecione uma porta COM válida.");

            if (EstaConectado)
                Desconectar();

            _portaSerial = new SerialPort(nomePorta, baudRate);

            // Inscreve o manipulador de eventos de recepção de dados
            _portaSerial.DataReceived += PortaSerial_DataReceived;

            _portaSerial.Open();
        }

        // Fecha a porta e libera a memória
        public void Desconectar()
        {
            if (_portaSerial != null)
            {
                if (_portaSerial.IsOpen)
                {
                    _portaSerial.DataReceived -= PortaSerial_DataReceived;
                    _portaSerial.Close();
                }

                _portaSerial.Dispose();
                _portaSerial = null;
            }
        }

        // Envia dados para o Arduino/ESP32
        public void EnviarDados(string texto)
        {
            if (EstaConectado && !string.IsNullOrEmpty(texto))
            {
                // Envia o texto seguido de quebra de linha (\r\n)
                _portaSerial.WriteLine(texto);
            }
        }

        // Método executado em segundo plano pela thread da SerialPort
        private void PortaSerial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_portaSerial != null && _portaSerial.IsOpen)
                {
                    // Lê a linha enviada pelo Serial.println() do microcontrolador
                    string linha = _portaSerial.ReadLine();

                    // Dispara o evento repassando a string lida para a camada de UI
                    DadosRecebidos?.Invoke(this, linha);
                }
            }
            catch
            {
                // Ignora falhas pontuais de leitura em desconexões abruptas
            }
        }
    }
}