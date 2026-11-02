using DevSandbox.Core.Serial;

namespace DevSandbox.Tests.Fakes
{
    /// <summary>
    /// Dublê de <see cref="ISerialPort"/>: grava o que foi escrito e permite simular falhas de E/S e recepção.
    /// </summary>
    internal sealed class FakeSerialPort : ISerialPort
    {
        private readonly List<string> _escritos = [];

        public FakeSerialPort(string nome, int baudRate)
        {
            Nome = nome;
            BaudRate = baudRate;
        }

        public string Nome { get; }
        public int BaudRate { get; }
        public bool IsOpen { get; set; }
        public bool Descartada { get; private set; }
        public IReadOnlyList<string> Escritos => _escritos;

        /// <summary>Exceção lançada na próxima escrita (simula cabo removido durante o envio).</summary>
        public Exception? FalhaNaEscrita { get; set; }

        /// <summary>Exceção lançada na leitura (simula falha durante a recepção).</summary>
        public Exception? FalhaNaLeitura { get; set; }

        /// <summary>Exceção lançada ao abrir (simula porta ocupada ou inexistente).</summary>
        public Exception? FalhaNaAbertura { get; set; }

        private string _bufferEntrada = "";

        public event EventHandler? DataReceived;

        public void Open()
        {
            if (FalhaNaAbertura != null) throw FalhaNaAbertura;
            IsOpen = true;
        }

        public void Close() => IsOpen = false;

        public void Write(string texto)
        {
            if (FalhaNaEscrita != null) throw FalhaNaEscrita;
            _escritos.Add(texto);
        }

        public void WriteLine(string texto)
        {
            if (FalhaNaEscrita != null) throw FalhaNaEscrita;
            _escritos.Add(texto + "\n");
        }

        public string ReadExisting()
        {
            if (FalhaNaLeitura != null) throw FalhaNaLeitura;
            string dados = _bufferEntrada;
            _bufferEntrada = "";
            return dados;
        }

        /// <summary>Coloca dados no buffer de entrada e dispara <see cref="DataReceived"/>.</summary>
        public void SimularRecepcao(string dados)
        {
            _bufferEntrada += dados;
            DataReceived?.Invoke(this, EventArgs.Empty);
        }

        public int AssinantesDataReceived => DataReceived?.GetInvocationList().Length ?? 0;

        public void Dispose()
        {
            IsOpen = false;
            Descartada = true;
        }
    }
}
