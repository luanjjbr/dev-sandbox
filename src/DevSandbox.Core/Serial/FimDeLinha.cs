namespace DevSandbox.Core.Serial
{
    /// <summary>
    /// Terminador acrescentado ao texto transmitido pela serial (Supervisório e terminal da Bancada).
    /// </summary>
    public enum FimDeLinha
    {
        /// <summary>Sem terminador: o texto vai exatamente como digitado.</summary>
        Nenhum,

        /// <summary>Carriage return (<c>\r</c>).</summary>
        CR,

        /// <summary>Line feed (<c>\n</c>) — padrão; é o que o firmware da Bancada espera.</summary>
        LF,

        /// <summary>Carriage return + line feed (<c>\r\n</c>).</summary>
        CRLF
    }

    public static class FimDeLinhaExtensions
    {
        /// <summary>Sequência de caracteres do terminador ("", "\r", "\n" ou "\r\n").</summary>
        /// <exception cref="ArgumentOutOfRangeException">Valor fora do enum.</exception>
        public static string ObterTerminador(this FimDeLinha fimDeLinha) => fimDeLinha switch
        {
            FimDeLinha.Nenhum => "",
            FimDeLinha.CR => "\r",
            FimDeLinha.LF => "\n",
            FimDeLinha.CRLF => "\r\n",
            _ => throw new ArgumentOutOfRangeException(nameof(fimDeLinha), fimDeLinha, "Fim de linha desconhecido.")
        };

        /// <summary>Rótulo curto para a UI e para o log TX ("—", "CR", "LF" ou "CRLF").</summary>
        /// <exception cref="ArgumentOutOfRangeException">Valor fora do enum.</exception>
        public static string ObterRotulo(this FimDeLinha fimDeLinha) => fimDeLinha switch
        {
            FimDeLinha.Nenhum => "—",
            FimDeLinha.CR => "CR",
            FimDeLinha.LF => "LF",
            FimDeLinha.CRLF => "CRLF",
            _ => throw new ArgumentOutOfRangeException(nameof(fimDeLinha), fimDeLinha, "Fim de linha desconhecido.")
        };
    }
}
