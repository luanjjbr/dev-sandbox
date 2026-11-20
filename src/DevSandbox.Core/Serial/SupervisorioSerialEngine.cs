using System.IO.Ports;

namespace DevSandbox.Core.Serial
{
    /// <summary>
    /// Motor serial resiliente: conexão, envio protegido, watchdog de presença da porta e reconexão automática.
    /// </summary>
    /// <remarks>
    /// Regras de concorrência:
    /// <list type="bullet">
    /// <item>Os eventos são disparados fora do <c>lock</c> interno, para que um assinante que use <c>Control.Invoke</c>
    /// não provoque deadlock com a thread de UI.</item>
    /// <item>A thread do <c>DataReceived</c> nunca adquire o <c>lock</c> interno: o <c>SerialPort</c> segura um lock
    /// próprio durante o evento e o <c>Close()</c> (chamado sob o nosso lock) espera por ele — inversão de locks.
    /// Uma falha de leitura só é sinalizada; o watchdog (≤ 1 s) trata a perda.</item>
    /// <item>Cada <c>Conectar</c>/<c>Desconectar</c> abre uma nova sessão; eventos decididos numa sessão anterior
    /// são descartados.</item>
    /// </list>
    /// </remarks>
    public sealed class SupervisorioSerialEngine : ISerialEngine
    {
        private static readonly TimeSpan IntervaloWatchdogPadrao = TimeSpan.FromSeconds(1);

        private readonly Func<string, int, ISerialPort> _fabricaPorta;
        private readonly Func<string[]> _listarPortas;
        private readonly TimeSpan? _intervaloWatchdog;
        private readonly object _lock = new();

        private volatile ISerialPort? _portaSerial;
        private System.Timers.Timer? _watchdogTimer;
        private bool _monitorando;
        private volatile bool _disposed;
        private int _sessao;

        private string? _ultimaPorta;
        private int _ultimoBaudRate;
        private volatile bool _tentandoReconectar;

        // Porta cuja leitura falhou na thread do DataReceived; tratada pelo watchdog.
        private volatile ISerialPort? _portaComFalhaDeLeitura;

        // Só usa a ausência em GetPortNames() como sinal de queda se a porta estava listada ao conectar
        // (alguns drivers de portas virtuais não se registram em HARDWARE\DEVICEMAP\SERIALCOMM).
        private bool _portaListadaAoConectar;

        public event EventHandler<string>? DadosRecebidos;
        public event EventHandler? ConexaoPerdida;
        public event EventHandler? Reconectado;

        public SupervisorioSerialEngine()
            : this((nome, baud) => new SistemaSerialPort(nome, baud), SerialPort.GetPortNames, IntervaloWatchdogPadrao)
        {
        }

        /// <summary>
        /// Construtor para testes: fábrica de portas, enumeração de portas e intervalo do watchdog
        /// (<c>null</c> desliga o timer; o ciclo é então executado manualmente por <see cref="ExecutarCicloWatchdog"/>).
        /// </summary>
        internal SupervisorioSerialEngine(Func<string, int, ISerialPort> fabricaPorta, Func<string[]> listarPortas, TimeSpan? intervaloWatchdog)
        {
            _fabricaPorta = fabricaPorta ?? throw new ArgumentNullException(nameof(fabricaPorta));
            _listarPortas = listarPortas ?? throw new ArgumentNullException(nameof(listarPortas));
            _intervaloWatchdog = intervaloWatchdog;
        }

        public bool EstaConectado
        {
            get
            {
                ISerialPort? porta = _portaSerial;
                try { return porta != null && porta.IsOpen; }
                catch (ObjectDisposedException) { return false; }
            }
        }

        public bool TentandoReconectar => _tentandoReconectar;

        /// <summary>
        /// <c>true</c> se uma falha de leitura foi sinalizada e aguarda o próximo ciclo do watchdog.
        /// </summary>
        internal bool FalhaDeLeituraPendente => _portaComFalhaDeLeitura != null;

        // Retorna a lista de portas COM disponíveis no Windows (ex: COM1, COM6)
        public string[] ObterPortasDisponiveis()
        {
            try { return _listarPortas() ?? []; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                return [];
            }
        }

        // Conecta à porta serial especificada e inicia o monitoramento de integridade
        public void Conectar(string nomePorta, int baudRate)
        {
            if (string.IsNullOrWhiteSpace(nomePorta))
                throw new ArgumentException("Selecione uma porta COM válida.", nameof(nomePorta));

            if (baudRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(baudRate), "O Baud Rate deve ser maior que zero.");

            ObjectDisposedException.ThrowIf(_disposed, this);

            lock (_lock)
            {
                // Dispose pode ter ocorrido entre a checagem acima e a aquisição do lock
                ObjectDisposedException.ThrowIf(_disposed, this);

                _sessao++;
                PararWatchdog();

                _ultimaPorta = nomePorta;
                _ultimoBaudRate = baudRate;
                _tentandoReconectar = false;
                _portaComFalhaDeLeitura = null;
                _portaListadaAoConectar = PortaEstaListada(ObterPortasDisponiveis());

                AbrirPortaInterno();
                IniciarWatchdog();
            }
        }

        // Desconecta a porta e interrompe o Watchdog
        public void Desconectar()
        {
            lock (_lock)
            {
                _sessao++;
                PararWatchdog();
                _tentandoReconectar = false;
                _portaComFalhaDeLeitura = null;
                FecharPortaInterno();
            }
        }

        /// <summary>
        /// Transmite texto + terminador numa única escrita. Retorna <c>true</c> só se a escrita ocorreu.
        /// Texto vazio envia só o terminador (CR/LF/CRLF); vazio com <see cref="FimDeLinha.Nenhum"/> retorna <c>false</c>.
        /// Em falha de E/S fecha a porta, entra em reconexão e dispara <see cref="ConexaoPerdida"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="fimDeLinha"/> fora do enum.</exception>
        public bool EnviarDados(string texto, FimDeLinha fimDeLinha)
        {
            string dados = (texto ?? "") + fimDeLinha.ObterTerminador();
            if (dados.Length == 0 || _disposed) return false;

            bool perdeuConexao;
            int sessao;
            lock (_lock)
            {
                ISerialPort? porta = _portaSerial;
                if (porta == null || !EstaConectado) return false;

                try
                {
                    porta.Write(dados);

                    return true;
                }
                catch (Exception ex) when (ex is IOException or InvalidOperationException or TimeoutException
                                               or UnauthorizedAccessException or ObjectDisposedException)
                {
                    perdeuConexao = MarcarPerdaInterno();
                    sessao = _sessao;
                }
            }

            if (perdeuConexao) DispararSeMesmaSessao(ConexaoPerdida, sessao);
            return false;
        }

        /// <summary>
        /// Um ciclo do watchdog: detecta porta fechada, removida da enumeração do Windows ou com falha de leitura
        /// sinalizada e, em reconexão, reabre a porta assim que ela reaparece. Chamado pelo timer (1 s).
        /// </summary>
        internal void ExecutarCicloWatchdog()
        {
            if (_disposed) return;

            bool perdeuConexao = false;
            bool reconectou = false;
            int sessao;

            // Garante que apenas um ciclo do watchdog execute por vez
            if (!Monitor.TryEnter(_lock)) return;
            try
            {
                if (_disposed || !_monitorando) return;

                sessao = _sessao;
                string[] portasAtivas = ObterPortasDisponiveis();
                bool portaPresente = PortaEstaListada(portasAtivas);

                ISerialPort? portaComFalha = _portaComFalhaDeLeitura;
                _portaComFalhaDeLeitura = null;

                ISerialPort? porta = _portaSerial;
                if (porta != null && !_tentandoReconectar)
                {
                    bool fechada = !EstaConectado;
                    bool sumiu = _portaListadaAoConectar && !portaPresente;
                    bool falhouLeitura = ReferenceEquals(portaComFalha, porta);
                    if (fechada || sumiu || falhouLeitura)
                    {
                        perdeuConexao = MarcarPerdaInterno();
                    }
                }

                // Porta que nunca foi listada (driver virtual): tenta reabrir às cegas a cada ciclo
                if (_tentandoReconectar && (portaPresente || !_portaListadaAoConectar))
                {
                    reconectou = TentarReconectarInterno();
                }
            }
            finally
            {
                Monitor.Exit(_lock);
            }

            if (perdeuConexao) DispararSeMesmaSessao(ConexaoPerdida, sessao);
            if (reconectou) DispararSeMesmaSessao(Reconectado, sessao);
        }

        // Descarta o evento se um Conectar/Desconectar abriu outra sessão depois que ele foi decidido.
        private void DispararSeMesmaSessao(EventHandler? handler, int sessao)
        {
            if (Volatile.Read(ref _sessao) != sessao || _disposed) return;
            handler?.Invoke(this, EventArgs.Empty);
        }

        // Deve ser chamado com _lock adquirido. Retorna true se a perda é nova (evento a disparar).
        private bool MarcarPerdaInterno()
        {
            if (_disposed || !_monitorando || _tentandoReconectar) return false;

            _tentandoReconectar = true;
            FecharPortaInterno();
            return true;
        }

        private bool PortaEstaListada(string[] portas) =>
            !string.IsNullOrEmpty(_ultimaPorta) &&
            Array.Exists(portas, p => string.Equals(p, _ultimaPorta, StringComparison.OrdinalIgnoreCase));

        private void AbrirPortaInterno()
        {
            FecharPortaInterno();

            ISerialPort? porta = null;
            try
            {
                porta = _fabricaPorta(_ultimaPorta!, _ultimoBaudRate);
                porta.DataReceived += PortaSerial_DataReceived;
                porta.Open();
                _portaSerial = porta;
            }
            catch
            {
                if (porta != null)
                {
                    porta.DataReceived -= PortaSerial_DataReceived;
                    try { if (porta.IsOpen) porta.Close(); } catch { /* porta já inválida */ }
                    try { porta.Dispose(); } catch { /* porta já inválida */ }
                }
                _portaSerial = null;
                throw;
            }
        }

        private void FecharPortaInterno()
        {
            ISerialPort? porta = _portaSerial;
            if (porta == null) return;

            _portaSerial = null;
            porta.DataReceived -= PortaSerial_DataReceived;
            try { if (porta.IsOpen) porta.Close(); } catch { /* porta já inválida (cabo removido) */ }
            try { porta.Dispose(); } catch { /* porta já inválida (cabo removido) */ }
        }

        // Deve ser chamado com _lock adquirido. Retorna true se reabriu a porta.
        private bool TentarReconectarInterno()
        {
            try
            {
                AbrirPortaInterno();
                _tentandoReconectar = false;
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
            {
                // Falha nesta tentativa; mantém o status até a porta ser restabelecida
                return false;
            }
        }

        private void IniciarWatchdog()
        {
            PararWatchdog();
            _monitorando = true;

            if (_intervaloWatchdog is not { } intervalo) return;

            _watchdogTimer = new System.Timers.Timer(intervalo.TotalMilliseconds) { AutoReset = true };
            _watchdogTimer.Elapsed += WatchdogTimer_Elapsed;
            _watchdogTimer.Start();
        }

        private void PararWatchdog()
        {
            _monitorando = false;

            if (_watchdogTimer != null)
            {
                _watchdogTimer.Stop();
                _watchdogTimer.Elapsed -= WatchdogTimer_Elapsed;
                _watchdogTimer.Dispose();
                _watchdogTimer = null;
            }
        }

        private void WatchdogTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e) => ExecutarCicloWatchdog();

        // Roda na thread do SerialPort: NÃO adquirir _lock aqui (ver remarks da classe).
        private void PortaSerial_DataReceived(object? sender, EventArgs e)
        {
            if (_disposed || sender is not ISerialPort porta) return;

            string? dados;
            try
            {
                if (!porta.IsOpen) return;

                // Leitura não bloqueante de todo o buffer disponível
                dados = porta.ReadExisting();
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or TimeoutException or ObjectDisposedException or UnauthorizedAccessException)
            {
                // Só sinaliza; o watchdog confirma se ainda é a porta ativa e trata a perda.
                _portaComFalhaDeLeitura = porta;
                return;
            }

            // Só repassa dados da porta da sessão atual (descarta sobras de uma porta já fechada/substituída)
            if (!string.IsNullOrEmpty(dados) && ReferenceEquals(porta, _portaSerial) && !_disposed)
            {
                DadosRecebidos?.Invoke(this, dados);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Desconectar();
        }
    }
}
