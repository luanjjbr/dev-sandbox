namespace DevSandbox.Core.Bancada
{
    /// <summary>
    /// Relés do Piloto de Controle (CP) na placa EasyEDA do Mega, pelo pino físico que os aciona
    /// (igual ao <c>firmware/Bancada/include/ConfigHardware.h</c>). O <c>[STATUS CP]</c> lista os relés ativos
    /// como <c>Reles: D30 D29</c> (ou <c>Reles: nenhum</c>).
    /// </summary>
    public static class RelesCP
    {
        /// <summary>Menor pino de relé do CP (D22).</summary>
        public const int PinoMinimo = 22;

        /// <summary>Maior pino de relé do CP (D31).</summary>
        public const int PinoMaximo = 31;

        /// <summary>Nome lógico de cada relé, por pino (D30 PE ... D22 D-sel).</summary>
        public static IReadOnlyDictionary<int, string> NomesPorPino { get; } = new Dictionary<int, string>
        {
            [30] = "PE",     // PINO_CP_K_PE     (RLY6, S7)
            [31] = "falha",  // PINO_CP_K_FALHA  (RLY1, S8)
            [29] = "B-hab",  // PINO_CP_K_B_EN   (RLY7)
            [28] = "B-sel",  // PINO_CP_K_B_SEL  (RLY2)
            [27] = "CD-hab", // PINO_CP_K_CD_EN  (RLY5)
            [26] = "CD-sel", // PINO_CP_K_CD_SEL (RLY8)
            [25] = "C-hab",  // PINO_CP_K_C_EN   (RLY3)
            [24] = "C-sel",  // PINO_CP_K_C_SEL  (RLY4)
            [23] = "D-hab",  // PINO_CP_K_D_EN   (RLY9)
            [22] = "D-sel",  // PINO_CP_K_D_SEL  (RLY10)
        };

        /// <summary><c>true</c> se o pino é de um relé do CP (22..31).</summary>
        public static bool EhPinoDeRele(int pino) => pino is >= PinoMinimo and <= PinoMaximo;

        /// <summary>Nome lógico do relé do pino (ex.: 30 → <c>PE</c>); <c>null</c> se o pino não é de relé do CP.</summary>
        public static string? ObterNome(int pino) => NomesPorPino.TryGetValue(pino, out string? nome) ? nome : null;

        /// <summary>
        /// Interpreta o campo <c>Reles</c> do <c>[STATUS CP]</c> (o texto depois de <c>Reles: </c>):
        /// <c>D30 D29</c> → [30, 29] (na ordem do firmware); <c>nenhum</c> → lista vazia.
        /// Retorna <c>false</c> se algum item não for <c>D22</c>..<c>D31</c> ou se houver repetição.
        /// </summary>
        public static bool TentarInterpretar(string? campo, out IReadOnlyList<int> pinos)
        {
            pinos = [];
            string texto = (campo ?? "").Trim();
            if (texto.Length == 0) return false;
            if (texto == "nenhum") return true;

            var lidos = new List<int>();
            foreach (string item in texto.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (item.Length < 2 || item[0] != 'D'
                    || !int.TryParse(item.AsSpan(1), System.Globalization.NumberStyles.None,
                                     System.Globalization.CultureInfo.InvariantCulture, out int pino)
                    || !EhPinoDeRele(pino) || lidos.Contains(pino))
                {
                    return false;
                }
                lidos.Add(pino);
            }

            pinos = lidos.AsReadOnly();
            return true;
        }
    }
}
