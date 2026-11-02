using System.Diagnostics.CodeAnalysis;

namespace DevSandbox.Core.Converters
{
    /// <summary>Base numérica detectada na entrada da calculadora.</summary>
    public enum BaseNumerica
    {
        [SuppressMessage("Naming", "CA1720:Identifier contains type name",
            Justification = "Nome do domínio (base 10), não o tipo System.Decimal; renomear quebraria a API usada pela UI.")]
        Decimal,
        Hexadecimal,
        Binario
    }
}
