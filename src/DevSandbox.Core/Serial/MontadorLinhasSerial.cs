using System.Text;

namespace DevSandbox.Core.Serial
{
    /// <summary>
    /// Junta os pedaços entregues pela serial (<c>ReadExisting</c>) em linhas completas, cortando em <c>\n</c>.
    /// O tamanho de cada pedaço depende do driver USB/Windows, não do conteúdo; esta classe esconde isso da UI.
    /// </summary>
    /// <remarks>
    /// Thread-safe com <c>lock</c> próprio: <see cref="Adicionar"/> roda na thread da serial e a coleta roda na UI.
    /// Não toca no lock de nenhum engine.
    /// </remarks>
    public sealed class MontadorLinhasSerial
    {
        /// <summary>Tempo sem receber nenhum byte para que o texto sem <c>\n</c> seja entregue.</summary>
        public static readonly TimeSpan TempoOciosoParaEntrega = TimeSpan.FromMilliseconds(100);

        /// <summary>Tamanho máximo da linha pendente; acima disso ela é entregue mesmo sem <c>\n</c>.</summary>
        public const int TamanhoMaximoLinha = 4096;

        private readonly TimeProvider _relogio;
        private readonly object _lock = new();
        private readonly StringBuilder _pendente = new();
        private long _instanteUltimoByte;

        // O último texto entregue por ociosidade/descarga terminava em '\r': um '\n' logo em seguida só fecha esse
        // "\r\n" atrasado e não pode virar uma linha vazia falsa.
        private bool _engolirProximoLf;

        public MontadorLinhasSerial() : this(TimeProvider.System)
        {
        }

        public MontadorLinhasSerial(TimeProvider relogio)
        {
            ArgumentNullException.ThrowIfNull(relogio);
            _relogio = relogio;
        }

        /// <summary>
        /// Acumula <paramref name="dados"/> e devolve as linhas que fecharam (sem <c>\r\n</c>/<c>\n</c>).
        /// Linha vazia também é devolvida.
        /// </summary>
        public IReadOnlyList<string> Adicionar(string dados)
        {
            if (string.IsNullOrEmpty(dados)) return Array.Empty<string>();

            List<string>? linhas = null;
            lock (_lock)
            {
                _instanteUltimoByte = _relogio.GetTimestamp();

                foreach (char c in dados)
                {
                    if (_engolirProximoLf)
                    {
                        _engolirProximoLf = false;
                        if (c == '\n') continue;
                    }

                    if (c == '\n')
                    {
                        (linhas ??= new List<string>()).Add(RetirarPendente());
                        continue;
                    }

                    // Proteção de memória: linha longa demais sem '\n' é entregue como está.
                    // O '\r' pode passar do limite, para que o '\n' seguinte feche "\r\n" sem linha vazia falsa.
                    if (_pendente.Length >= TamanhoMaximoLinha && c != '\r')
                    {
                        (linhas ??= new List<string>()).Add(RetirarPendente());
                    }

                    _pendente.Append(c);
                }
            }

            return linhas is null ? Array.Empty<string>() : linhas;
        }

        /// <summary>
        /// Se há texto pendente sem <c>\n</c> e já se passaram ao menos <see cref="TempoOciosoParaEntrega"/>
        /// desde o último byte, devolve esse texto e limpa; senão devolve <c>null</c>.
        /// </summary>
        public string? ColetarPendenteSeOcioso()
        {
            lock (_lock)
            {
                if (_pendente.Length == 0) return null;
                if (_relogio.GetElapsedTime(_instanteUltimoByte) < TempoOciosoParaEntrega) return null;
                return RetirarPendenteAntecipado();
            }
        }

        /// <summary>Devolve o texto pendente na hora (ou <c>null</c> se vazio) e limpa.</summary>
        public string? Descarregar()
        {
            lock (_lock)
            {
                return _pendente.Length == 0 ? null : RetirarPendenteAntecipado();
            }
        }

        /// <summary>Descarta o texto pendente.</summary>
        public void Limpar()
        {
            lock (_lock)
            {
                _pendente.Clear();
                _engolirProximoLf = false;
            }
        }

        // Chamar sob _lock. Entrega antes do '\n' (ociosidade/descarga): se terminava em '\r', o próximo '\n' é engolido.
        private string RetirarPendenteAntecipado()
        {
            _engolirProximoLf = _pendente[^1] == '\r';
            return RetirarPendente();
        }

        // Chamar sob _lock
        private string RetirarPendente()
        {
            int tamanho = _pendente.Length;
            if (tamanho > 0 && _pendente[tamanho - 1] == '\r') tamanho--;

            string linha = _pendente.ToString(0, tamanho);
            _pendente.Clear();
            return linha;
        }
    }
}
