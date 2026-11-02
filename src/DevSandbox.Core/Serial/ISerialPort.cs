namespace DevSandbox.Core.Serial
{
    /// <summary>
    /// Abstração mínima de <see cref="System.IO.Ports.SerialPort"/> usada pelo <see cref="SupervisorioSerialEngine"/>.
    /// Existe para testar a perda de conexão (exceções de escrita/leitura) sem hardware.
    /// </summary>
    /// <remarks>
    /// <c>DataReceived</c> usa <see cref="EventHandler"/> em vez de <c>SerialDataReceivedEventHandler</c>,
    /// porque <c>SerialDataReceivedEventArgs</c> não tem construtor público e não poderia ser disparado por um dublê.
    /// </remarks>
    public interface ISerialPort : IDisposable
    {
        bool IsOpen { get; }
        void Open();
        void Close();
        void Write(string texto);
        void WriteLine(string texto);
        string ReadExisting();
        event EventHandler? DataReceived;
    }
}
