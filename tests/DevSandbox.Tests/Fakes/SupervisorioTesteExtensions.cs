using DevSandbox.Core.Serial;

namespace DevSandbox.Tests.Fakes
{
    /// <summary>
    /// O overload legado <c>EnviarDados(string, bool)</c> existe só como método padrão de <see cref="ISerialEngine"/>
    /// (não é visível pelo tipo concreto). Esta extensão deixa os testes antigos chamarem pelo
    /// <see cref="SupervisorioSerialEngine"/> exercitando exatamente o caminho da interface.
    /// </summary>
    internal static class SupervisorioTesteExtensions
    {
        public static bool EnviarDados(this SupervisorioSerialEngine engine, string texto, bool adicionarNovaLinha = true) =>
            ((ISerialEngine)engine).EnviarDados(texto, adicionarNovaLinha);
    }
}
