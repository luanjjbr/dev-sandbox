using System.Text;
using DevSandbox.Core.Serial;

namespace DevSandbox.Core.Bancada
{
    /// <summary>
    /// Motor de controle e comunicação da Bancada de Testes de Veículo Elétrico (IEC 61851-1 / IEC 62196-2).
    /// Envia os comandos seriais ao emulador físico (Arduino), interpreta as respostas do firmware e
    /// informa o desfecho de cada comando (<see cref="ComandoConcluido"/>).
    /// </summary>
    /// <remarks>
    /// <para>O firmware processa uma linha por vez e responde em ordem, então os comandos que esperam resposta
    /// ficam numa fila FIFO e cada <c>OK</c>/<c>ERRO</c>/<c>[STATUS ...]</c> conclui o mais antigo.
    /// <c>help</c>, <c>?</c> e <c>debug stack</c> não entram na fila (respondem várias linhas, sem OK).</para>
    /// <para>Prazo por atividade: qualquer linha recebida renova o prazo do comando da vez. Uma resposta que chega
    /// depois do <see cref="ResultadoComando.SemResposta"/> (dentro de mais um prazo) é consumida como órfã e não
    /// conclui o comando seguinte.</para>
    /// <para>Ordem dos eventos: <see cref="CaboAlterado"/>/<see cref="EstadoVeiculoAlterado"/> vêm antes da
    /// transmissão; <see cref="ComandoConcluido"/> é a verdade final e pode chegar antes do
    /// <see cref="ComandoEnviado"/>. Os eventos podem vir da thread da serial ou do timer do prazo: a UI deve
    /// usar <c>BeginInvoke</c>.</para>
    /// </remarks>
    public sealed class BancadaEngine : IDisposable
    {
        /// <summary>Prazo padrão sem atividade da serial até concluir um comando como <see cref="ResultadoComando.SemResposta"/>.</summary>
        public static readonly TimeSpan TimeoutRespostaPadrao = TimeSpan.FromSeconds(4);

        /// <summary>Motivo do <see cref="ResultadoComando.NaoEnviado"/> sem conexão ou com falha de escrita.</summary>
        public const string DetalheSemConexao = "Sem conexão com a bancada ou falha na escrita da porta serial.";

        /// <summary>Motivo do <see cref="ResultadoComando.NaoEnviado"/> de um comando manual com caractere inválido.</summary>
        public const string DetalheCaractereInvalido =
            "O comando contém caractere fora do ASCII (ex.: acento ou 'ç') ou de controle; o firmware só aceita ASCII imprimível.";

        // Linha sem '\n' maior que isto é descartada (o firmware nunca envia linhas tão longas)
        private const int TamanhoMaximoLinha = 4096;

        private readonly ISerialEngine _serialEngine;
        private readonly bool _descartarSerial;
        private readonly TimeProvider _relogio;
        private readonly TimeSpan _timeoutResposta;
        private readonly ITimer _timerPrazo;
        private readonly object _lock = new();
        private readonly LinkedList<Pendente> _pendentes = new();
        private readonly Queue<Orfa> _orfas = new();
        private readonly StringBuilder _bufferLinha = new();
        private long _ultimoId;
        private volatile bool _disposed;

        public EstadoCaboPP CaboAtual { get; private set; } = EstadoCaboPP.CircuitoAberto;
        public EstadoVeiculoCP EstadoAtual { get; private set; } = EstadoVeiculoCP.AX;

        /// <summary>Último estado CP confirmado pelo firmware (<c>OK</c> ou <c>[STATUS CP]</c>); <c>null</c> se nenhum.</summary>
        public EstadoVeiculoCP? EstadoConfirmado { get; private set; }

        /// <summary>Último cabo PP confirmado pelo firmware (<c>OK</c> ou <c>[STATUS CABO]</c>); <c>null</c> se nenhum.</summary>
        public EstadoCaboPP? CaboConfirmado { get; private set; }

        /// <summary><see cref="ConclusaoComando.Id"/> do último comando iniciado (0 se nenhum).</summary>
        public long UltimoIdComando => Interlocked.Read(ref _ultimoId);

        public bool EstaConectado => _serialEngine.EstaConectado;
        public bool TentandoReconectar => _serialEngine.TentandoReconectar;

        /// <summary>Texto bruto recebido da serial (fragmentos, como chegaram).</summary>
        public event EventHandler<string>? DadosRecebidos;

        /// <summary>
        /// Disparado somente quando o comando foi efetivamente transmitido pela serial.
        /// Pode chegar depois do <see cref="ComandoConcluido"/> do mesmo comando.
        /// </summary>
        public event EventHandler<string>? ComandoEnviado;

        /// <summary>Cada linha completa recebida do firmware, já interpretada.</summary>
        public event EventHandler<RespostaFirmware>? RespostaRecebida;

        /// <summary>
        /// Desfecho de um comando (verdade final): confirmado, erro do firmware, não enviado ou sem resposta.
        /// Comandos sem resposta de confirmação (<c>help</c>, <c>?</c>, <c>debug stack</c>) só geram este evento se não forem enviados.
        /// </summary>
        public event EventHandler<ConclusaoComando>? ComandoConcluido;

        /// <summary>Estado CP informado por um <c>[STATUS CP]</c> do firmware.</summary>
        public event EventHandler<EstadoVeiculoCP>? StatusCPRecebido;

        public event EventHandler? ConexaoPerdida;
        public event EventHandler? Reconectado;

        /// <summary>Seleção local do cabo (disparado antes da transmissão).</summary>
        public event EventHandler<EstadoCaboPP>? CaboAlterado;

        /// <summary>Seleção local do estado CP (disparado antes da transmissão).</summary>
        public event EventHandler<EstadoVeiculoCP>? EstadoVeiculoAlterado;

        public BancadaEngine() : this(new SupervisorioSerialEngine())
        {
        }

        /// <param name="serialEngine">Motor serial (real ou dublê de teste).</param>
        /// <param name="descartarSerial">Se <c>true</c> (padrão), o <see cref="Dispose"/> descarta também o motor serial.</param>
        /// <param name="relogio">Relógio para o prazo de resposta (padrão <see cref="TimeProvider.System"/>).</param>
        /// <param name="timeoutResposta">Prazo sem atividade (padrão <see cref="TimeoutRespostaPadrao"/>).</param>
        public BancadaEngine(ISerialEngine serialEngine, bool descartarSerial = true,
                             TimeProvider? relogio = null, TimeSpan? timeoutResposta = null)
        {
            _serialEngine = serialEngine ?? throw new ArgumentNullException(nameof(serialEngine));
            _descartarSerial = descartarSerial;
            _relogio = relogio ?? TimeProvider.System;
            _timeoutResposta = timeoutResposta ?? TimeoutRespostaPadrao;
            if (_timeoutResposta <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeoutResposta), "O prazo de resposta deve ser maior que zero.");

            _timerPrazo = _relogio.CreateTimer(_ => ExpirarPendentes(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            _serialEngine.DadosRecebidos += Serial_DadosRecebidos;
            _serialEngine.ConexaoPerdida += Serial_ConexaoPerdida;
            _serialEngine.Reconectado += Serial_Reconectado;
        }

        /// <summary>Quantidade de comandos aguardando resposta do firmware.</summary>
        public int ComandosPendentes
        {
            get { lock (_lock) return _pendentes.Count; }
        }

        public string[] ObterPortasDisponiveis() => _serialEngine.ObterPortasDisponiveis();

        public void Conectar(string nomePorta, int baudRate = 9600)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            DescartarPendentes(); // respostas de uma conexão anterior não valem para a nova
            _serialEngine.Conectar(nomePorta, baudRate);
        }

        public void Desconectar()
        {
            _serialEngine.Desconectar();
            DescartarPendentes();
        }

        /// <summary>
        /// Seleciona o cabo PP. O estado local e <see cref="CaboAlterado"/> (antes da transmissão) são atualizados
        /// mesmo sem conexão; o retorno indica se o comando foi transmitido. O desfecho chega por <see cref="ComandoConcluido"/>.
        /// </summary>
        public bool DefinirCabo(EstadoCaboPP cabo)
        {
            CaboAtual = cabo;
            CaboAlterado?.Invoke(this, cabo);
            return EnviarComando(cabo.ObterComandoSerial(), estado: null, cabo: cabo);
        }

        /// <summary>
        /// Seleciona o estado CP do veículo. O estado local e <see cref="EstadoVeiculoAlterado"/> (antes da transmissão)
        /// são atualizados mesmo sem conexão; o retorno indica se o comando foi transmitido. O desfecho chega por
        /// <see cref="ComandoConcluido"/>.
        /// </summary>
        public bool DefinirEstado(EstadoVeiculoCP estado)
        {
            if (!Enum.IsDefined(estado))
                throw new ArgumentOutOfRangeException(nameof(estado), estado, "Estado CP desconhecido.");

            EstadoAtual = estado;
            EstadoVeiculoAlterado?.Invoke(this, estado);
            return EnviarComando(estado.ObterComandoSerial(), estado: estado, cabo: null);
        }

        /// <summary>Pede o relatório do CP (<c>estado status</c>); o estado chega por <see cref="StatusCPRecebido"/>.</summary>
        public bool SolicitarStatusCP() => EnviarComando(EstadoVeiculoCPExtensions.ComandoStatus, null, null);

        /// <summary>
        /// <c>true</c> se o comando manual só tem ASCII imprimível (o que o firmware entende). A UI pode usar
        /// para validar a caixa de texto antes de enviar; <see cref="EnviarComandoManual"/> aplica a mesma regra.
        /// </summary>
        public static bool ComandoManualValido(string? comando) =>
            !string.IsNullOrWhiteSpace(comando) && comando.All(c => c is >= ' ' and <= '~' || c == '\t');

        /// <summary>
        /// Envia um comando livre. Retorna <c>true</c> se foi transmitido. Comando com caractere fora do ASCII
        /// imprimível é recusado sem transmitir (<see cref="ResultadoComando.NaoEnviado"/> com
        /// <see cref="DetalheCaractereInvalido"/>).
        /// </summary>
        public bool EnviarComandoManual(string comando)
        {
            if (string.IsNullOrWhiteSpace(comando)) return false;

            string cmd = comando.Trim();
            InterpretarAlvoManual(cmd, out EstadoVeiculoCP? estado, out EstadoCaboPP? cabo);

            if (!ComandoManualValido(cmd))
            {
                // A SerialPort codifica em ASCII: 'ç' viraria '?' e o firmware executaria outro comando
                long id = Interlocked.Increment(ref _ultimoId);
                ComandoConcluido?.Invoke(this, new ConclusaoComando(cmd, ResultadoComando.NaoEnviado, null, estado, cabo)
                {
                    Id = id,
                    Detalhe = DetalheCaractereInvalido
                });
                return false;
            }

            return EnviarComando(cmd, estado, cabo);
        }

        private bool EnviarComando(string cmd, EstadoVeiculoCP? estado, EstadoCaboPP? cabo)
        {
            if (_disposed) return false;

            long id = Interlocked.Increment(ref _ultimoId);

            // Enfileira ANTES de transmitir: a resposta pode chegar antes de EnviarDados retornar.
            // "Transmitindo" impede que um DescartarPendentes disparado DENTRO do EnviarDados (ConexaoPerdida
            // síncrona na falha de escrita) conclua este comando: o desfecho dele é decidido aqui.
            Espera espera = ClassificarEspera(cmd);
            Pendente? pendente = null;
            LinkedListNode<Pendente>? no = null;
            if (espera != Espera.Nenhuma)
            {
                pendente = new Pendente(id, cmd, espera, estado, cabo) { Timestamp = _relogio.GetTimestamp(), Transmitindo = true };
                lock (_lock)
                {
                    if (_disposed) return false;
                    no = _pendentes.AddLast(pendente);
                    ReagendarPrazo();
                }
            }

            bool enviado = _serialEngine.EstaConectado && _serialEngine.EnviarDados(cmd);

            if (!enviado)
            {
                if (no != null)
                {
                    lock (_lock)
                    {
                        if (no.List != null) _pendentes.Remove(no);
                        ReagendarPrazo();
                    }
                }

                ComandoConcluido?.Invoke(this, new ConclusaoComando(cmd, ResultadoComando.NaoEnviado, null, estado, cabo)
                {
                    Id = id,
                    Detalhe = DetalheSemConexao
                });
                return false;
            }

            if (pendente != null)
            {
                lock (_lock)
                {
                    pendente.Transmitindo = false;
                }
            }

            ComandoEnviado?.Invoke(this, cmd);
            return true;
        }

        private void Serial_DadosRecebidos(object? sender, string dados)
        {
            DadosRecebidos?.Invoke(this, dados);
            ProcessarDados(dados);
        }

        /// <summary>Acumula o texto recebido e processa cada linha completa (terminada em <c>\n</c>).</summary>
        internal void ProcessarDados(string dados)
        {
            if (_disposed || string.IsNullOrEmpty(dados)) return;

            var linhas = new List<string>();
            lock (_lock)
            {
                foreach (char c in dados)
                {
                    if (c == '\n')
                    {
                        linhas.Add(_bufferLinha.ToString());
                        _bufferLinha.Clear();
                    }
                    else if (c != '\r')
                    {
                        if (_bufferLinha.Length >= TamanhoMaximoLinha) _bufferLinha.Clear();
                        _bufferLinha.Append(c);
                    }
                }
            }

            foreach (string linha in linhas)
            {
                if (!string.IsNullOrWhiteSpace(linha)) ProcessarLinha(linha);
            }
        }

        private void ProcessarLinha(string linha)
        {
            RespostaFirmware resposta = RespostaFirmware.Interpretar(linha);
            ConclusaoComando? concluido = null;

            lock (_lock)
            {
                if (_disposed) return;

                // Prazo por atividade: o firmware está falando (ex.: menu de ajuda longo), o comando da vez não expirou
                if (_pendentes.First?.Value is { } daVez) daVez.Timestamp = _relogio.GetTimestamp();
                DescartarOrfasVencidas();

                if (ConsumirOrfa(resposta))
                {
                    // Resposta atrasada de um comando já concluído como SemResposta: não desloca a FIFO
                }
                else if (_pendentes.First?.Value is { } primeiro && Conclui(primeiro.Espera, resposta.Tipo) is { } r)
                {
                    _pendentes.RemoveFirst();
                    concluido = new ConclusaoComando(primeiro.Comando, r, resposta, primeiro.Estado, primeiro.Cabo) { Id = primeiro.Id };

                    // O próximo da fila começa a contar o prazo agora (estava esperando a vez)
                    if (_pendentes.First?.Value is { } proximo) proximo.Timestamp = _relogio.GetTimestamp();

                    if (r == ResultadoComando.Confirmado)
                    {
                        if (primeiro.Estado is { } e) EstadoConfirmado = e;
                        if (primeiro.Cabo is { } c) CaboConfirmado = c;
                    }
                }

                if (resposta.EstadoCP is { } estadoStatus) EstadoConfirmado = estadoStatus;
                if (resposta.CaboPP is { } caboStatus) CaboConfirmado = caboStatus;

                ReagendarPrazo();
            }

            RespostaRecebida?.Invoke(this, resposta);
            if (concluido != null) ComandoConcluido?.Invoke(this, concluido);
            if (resposta.EstadoCP is { } estadoCP) StatusCPRecebido?.Invoke(this, estadoCP);
        }

        // Que resultado esta resposta dá ao comando que espera 'espera' (null = não o conclui).
        private static ResultadoComando? Conclui(Espera espera, TipoRespostaFirmware tipo) => (espera, tipo) switch
        {
            (_, TipoRespostaFirmware.Erro) => ResultadoComando.ErroFirmware,
            (Espera.Confirmacao, TipoRespostaFirmware.Ok) => ResultadoComando.Confirmado,
            (Espera.StatusCP, TipoRespostaFirmware.StatusCP) => ResultadoComando.Confirmado,
            (Espera.StatusCabo, TipoRespostaFirmware.StatusCabo) => ResultadoComando.Confirmado,
            _ => null // linha que não conclui o comando da vez (menu, banner, OK fora de ordem)
        };

        // Deve ser chamado com _lock adquirido. Consome a resposta se ela é a que um comando expirado ainda devia.
        private bool ConsumirOrfa(RespostaFirmware resposta)
        {
            if (_orfas.Count == 0 || Conclui(_orfas.Peek().Espera, resposta.Tipo) == null) return false;
            _orfas.Dequeue();
            return true;
        }

        // Deve ser chamado com _lock adquirido. Uma resposta atrasada só é aceita como órfã dentro de mais um prazo.
        private void DescartarOrfasVencidas()
        {
            while (_orfas.Count > 0 && _relogio.GetElapsedTime(_orfas.Peek().Timestamp) >= _timeoutResposta)
            {
                _orfas.Dequeue();
            }
        }

        private void ExpirarPendentes()
        {
            var expirados = new List<ConclusaoComando>();
            lock (_lock)
            {
                if (_disposed) return;

                while (_pendentes.First?.Value is { } p && !p.Transmitindo
                       && _relogio.GetElapsedTime(p.Timestamp) >= _timeoutResposta)
                {
                    _pendentes.RemoveFirst();
                    _orfas.Enqueue(new Orfa(p.Espera, _relogio.GetTimestamp()));
                    expirados.Add(new ConclusaoComando(p.Comando, ResultadoComando.SemResposta, null, p.Estado, p.Cabo) { Id = p.Id });

                    // O próximo da fila começa a contar o prazo agora (estava esperando a vez)
                    if (_pendentes.First?.Value is { } proximo) proximo.Timestamp = _relogio.GetTimestamp();
                }
                ReagendarPrazo();
            }

            foreach (ConclusaoComando c in expirados) ComandoConcluido?.Invoke(this, c);
        }

        // Conclui como SemResposta tudo o que estava pendente quando a conexão caiu (ConexaoPerdida), no
        // Desconectar e antes de um novo Conectar. Pula o comando que está dentro do EnviarDados (o desfecho
        // dele é decidido em EnviarComando).
        private void DescartarPendentes()
        {
            var descartados = new List<ConclusaoComando>();
            lock (_lock)
            {
                LinkedListNode<Pendente>? no = _pendentes.First;
                while (no != null)
                {
                    LinkedListNode<Pendente>? proximo = no.Next;
                    Pendente p = no.Value;
                    if (!p.Transmitindo)
                    {
                        _pendentes.Remove(no);
                        descartados.Add(new ConclusaoComando(p.Comando, ResultadoComando.SemResposta, null, p.Estado, p.Cabo) { Id = p.Id });
                    }
                    no = proximo;
                }
                _orfas.Clear();
                _bufferLinha.Clear();
                ReagendarPrazo();
            }

            foreach (ConclusaoComando c in descartados) ComandoConcluido?.Invoke(this, c);
        }

        // Deve ser chamado com _lock adquirido.
        private void ReagendarPrazo()
        {
            if (_disposed) return;

            if (_pendentes.First?.Value is not { } primeiro)
            {
                _timerPrazo.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                return;
            }

            TimeSpan restante = _timeoutResposta - _relogio.GetElapsedTime(primeiro.Timestamp);
            _timerPrazo.Change(restante > TimeSpan.Zero ? restante : TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        }

        /// <summary>Que resposta o firmware dá a este comando (regras de <c>processarComando</c> do main.cpp).</summary>
        internal static Espera ClassificarEspera(string comando)
        {
            // help/?/debug stack são comparados literalmente pelo firmware ("debug  stack" responde ERRO)
            string bruto = comando.Trim().ToLowerInvariant();
            if (bruto is "" or "help" or "?" or "debug stack") return Espera.Nenhuma;

            // "estado"/"cabo" pulam espaços antes do argumento: "estado   status" = "estado status"
            string cmd = Normalizar(comando);
            return cmd switch
            {
                "estado status" => Espera.StatusCP,
                "cabo status" => Espera.StatusCabo,
                _ => Espera.Confirmacao // OK ou ERRO (inclusive comandos desconhecidos e longos)
            };
        }

        // Estado/cabo alvo de um comando manual, aceitando os mesmos atalhos do firmware (a|b|c|d = nominal).
        private static void InterpretarAlvoManual(string comando, out EstadoVeiculoCP? estado, out EstadoCaboPP? cabo)
        {
            estado = null;
            cabo = null;
            string cmd = Normalizar(comando);

            if (cmd.StartsWith("estado ", StringComparison.Ordinal))
            {
                string arg = cmd.Substring("estado ".Length);
                estado = arg switch
                {
                    "a" => EstadoVeiculoCP.AX,
                    "b" => EstadoVeiculoCP.BXNominal,
                    "c" => EstadoVeiculoCP.CXNominal,
                    "d" => EstadoVeiculoCP.DXNominal,
                    _ => EstadoVeiculoCPExtensions.TentarInterpretarCodigo(arg, out var e) ? e : null
                };
            }
            else if (cmd.StartsWith("cabo ", StringComparison.Ordinal))
            {
                string arg = cmd.Substring("cabo ".Length);
                if (arg.Length == 1 && arg[0] is >= '0' and <= '6') cabo = (EstadoCaboPP)(arg[0] - '0');
            }
        }

        // Como o firmware: sem espaços nas pontas, minúsculas; aqui também colapsa espaços internos repetidos.
        private static string Normalizar(string comando) =>
            string.Join(' ', comando.Trim().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        private void Serial_ConexaoPerdida(object? sender, EventArgs e)
        {
            DescartarPendentes();
            ConexaoPerdida?.Invoke(this, e);
        }

        private void Serial_Reconectado(object? sender, EventArgs e) => Reconectado?.Invoke(this, e);

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;

                // Dentro do lock: nenhum ReagendarPrazo/ExpirarPendentes concorrente toca o timer já descartado
                _timerPrazo.Dispose();
                _pendentes.Clear();
                _orfas.Clear();
                _bufferLinha.Clear();
            }

            _serialEngine.DadosRecebidos -= Serial_DadosRecebidos;
            _serialEngine.ConexaoPerdida -= Serial_ConexaoPerdida;
            _serialEngine.Reconectado -= Serial_Reconectado;

            if (_descartarSerial)
            {
                _serialEngine.Dispose();
            }
        }

        internal enum Espera
        {
            /// <summary>O firmware não responde com OK/ERRO (ajuda, diagnóstico).</summary>
            Nenhuma,
            /// <summary><c>OK</c> ou <c>ERRO...</c>.</summary>
            Confirmacao,
            /// <summary><c>[STATUS CP]</c> ou <c>ERRO...</c> (Nano).</summary>
            StatusCP,
            /// <summary><c>[STATUS CABO]</c> ou <c>ERRO...</c>.</summary>
            StatusCabo
        }

        // Mutável só sob _lock (Timestamp renovado por atividade; Transmitindo durante o EnviarDados).
        private sealed class Pendente(long id, string comando, Espera espera, EstadoVeiculoCP? estado, EstadoCaboPP? cabo)
        {
            public long Id { get; } = id;
            public string Comando { get; } = comando;
            public Espera Espera { get; } = espera;
            public EstadoVeiculoCP? Estado { get; } = estado;
            public EstadoCaboPP? Cabo { get; } = cabo;
            public long Timestamp { get; set; }
            public bool Transmitindo { get; set; }
        }

        private readonly record struct Orfa(Espera Espera, long Timestamp);
    }
}
