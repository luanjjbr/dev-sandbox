namespace DevSandbox.Core.Serial
{
    /// <summary>
    /// Histórico de envios estilo terminal (setas ↑/↓) do Supervisório e da Bancada. Só memória; não é thread-safe
    /// (uso pela thread de UI).
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><see cref="Adicionar"/>: vazio/só espaços não entra; repetição igual à última entrada é ignorada;
    /// acima de <see cref="Limite"/> descarta a mais antiga. Sempre encerra a navegação.</item>
    /// <item><see cref="Anterior"/> (↑): na primeira chamada guarda o texto que estava sendo digitado e devolve a
    /// entrada mais recente; as seguintes vão para as mais antigas e param na mais antiga.</item>
    /// <item><see cref="Proximo"/> (↓): vai para as mais recentes; ao passar da mais recente devolve o texto que
    /// estava sendo digitado e encerra a navegação. Fora da navegação devolve <c>null</c> (a UI mantém o texto).</item>
    /// </list>
    /// </remarks>
    public sealed class HistoricoComandos
    {
        /// <summary>Quantidade máxima de entradas guardadas.</summary>
        public const int Limite = 100;

        private readonly List<string> _itens = [];

        // _posicao == _itens.Count significa "fora da navegação" (editando o texto atual).
        private int _posicao;
        private string _rascunho = "";

        /// <summary>Entradas, da mais antiga para a mais recente.</summary>
        public IReadOnlyList<string> Itens => _itens;

        public int Quantidade => _itens.Count;

        /// <summary><c>true</c> enquanto ↑/↓ está percorrendo o histórico.</summary>
        public bool EstaNavegando => _posicao < _itens.Count;

        /// <summary>
        /// Registra um texto enviado. Retorna <c>true</c> se entrou no histórico (<c>false</c> se vazio ou igual
        /// à última entrada).
        /// </summary>
        public bool Adicionar(string? texto)
        {
            bool adicionou = false;
            if (!string.IsNullOrWhiteSpace(texto) &&
                (_itens.Count == 0 || !string.Equals(_itens[^1], texto, StringComparison.Ordinal)))
            {
                _itens.Add(texto);
                if (_itens.Count > Limite) _itens.RemoveAt(0);
                adicionou = true;
            }

            ResetarNavegacao();
            return adicionou;
        }

        /// <summary>
        /// Seta ↑: devolve a entrada anterior. <paramref name="textoAtual"/> (o que está na caixa) é guardado na
        /// primeira chamada para ser devolvido por <see cref="Proximo"/> ao passar do fim. Com o histórico vazio
        /// devolve <paramref name="textoAtual"/>.
        /// </summary>
        public string Anterior(string? textoAtual)
        {
            if (_itens.Count == 0) return textoAtual ?? "";

            if (!EstaNavegando) _rascunho = textoAtual ?? "";
            if (_posicao > 0) _posicao--;
            return _itens[_posicao];
        }

        /// <summary>
        /// Seta ↓: devolve a entrada seguinte; ao passar da mais recente devolve o texto que estava sendo digitado
        /// e encerra a navegação. Fora da navegação devolve <c>null</c> (nada a mudar).
        /// </summary>
        public string? Proximo()
        {
            if (!EstaNavegando) return null;

            _posicao++;
            if (_posicao < _itens.Count) return _itens[_posicao];

            string rascunho = _rascunho;
            ResetarNavegacao();
            return rascunho;
        }

        /// <summary>Encerra a navegação (ex.: o usuário editou o texto) e descarta o texto guardado.</summary>
        public void ResetarNavegacao()
        {
            _posicao = _itens.Count;
            _rascunho = "";
        }
    }
}
