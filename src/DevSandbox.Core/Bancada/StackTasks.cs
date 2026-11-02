using System.Globalization;
using System.Text.RegularExpressions;

namespace DevSandbox.Core.Bancada
{
    /// <summary>
    /// Resposta do <c>debug stack</c>: menor stack livre já observada (high water mark, em words = bytes no AVR)
    /// e o tamanho configurado de cada task. Texto do <c>imprimirStack()</c> do <c>main.cpp</c>:
    /// <c>[STACK] SerialCLI livre: N de M | Blink livre: N de M</c>.
    /// </summary>
    public sealed partial record StackTasks(int SerialCLILivre, int SerialCLITotal, int BlinkLivre, int BlinkTotal)
    {
        /// <summary>Prefixo da linha de diagnóstico de stack.</summary>
        public const string Prefixo = "[STACK] ";

        /// <summary>Interpreta a linha <c>[STACK] ...</c>; <c>null</c> se o formato não bater.</summary>
        public static StackTasks? TentarInterpretar(string? linha)
        {
            Match m = RegexStack().Match((linha ?? "").Trim());
            if (!m.Success) return null;

            static int N(Group g) => int.Parse(g.Value, NumberStyles.None, CultureInfo.InvariantCulture);
            return new StackTasks(N(m.Groups[1]), N(m.Groups[2]), N(m.Groups[3]), N(m.Groups[4]));
        }

        [GeneratedRegex(@"^\[STACK\] SerialCLI livre: (\d{1,9}) de (\d{1,9}) \| Blink livre: (\d{1,9}) de (\d{1,9})$",
                        RegexOptions.CultureInvariant)]
        private static partial Regex RegexStack();
    }
}
