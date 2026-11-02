using System.IO.Ports;

namespace DevSandbox.Core.Serial
{
    /// <summary>
    /// Adaptador de <see cref="ISerialPort"/> sobre a porta real <see cref="SerialPort"/>.
    /// </summary>
    internal sealed class SistemaSerialPort : ISerialPort
    {
        /// <summary>Timeout de leitura e escrita da porta real, em milissegundos.</summary>
        internal const int TimeoutMs = 1000;

        private readonly SerialPort _porta;

        public event EventHandler? DataReceived;

        public SistemaSerialPort(string nomePorta, int baudRate)
        {
            _porta = new SerialPort(nomePorta, baudRate)
            {
                ReadTimeout = TimeoutMs,
                WriteTimeout = TimeoutMs
            };
            _porta.DataReceived += Porta_DataReceived;
        }

        public bool IsOpen => _porta.IsOpen;

        public void Open() => _porta.Open();
        public void Close() => _porta.Close();
        public void Write(string texto) => _porta.Write(texto);
        public void WriteLine(string texto) => _porta.WriteLine(texto);
        public string ReadExisting() => _porta.ReadExisting();

        private void Porta_DataReceived(object sender, SerialDataReceivedEventArgs e) =>
            DataReceived?.Invoke(this, EventArgs.Empty);

        public void Dispose()
        {
            _porta.DataReceived -= Porta_DataReceived;
            _porta.Dispose();
        }
    }
}
