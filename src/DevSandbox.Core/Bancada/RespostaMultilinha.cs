namespace DevSandbox.Core.Bancada
{
    /// <summary>
    /// Resposta de um comando que o firmware responde em várias linhas e sem <c>OK</c> (<c>help</c>, <c>?</c>,
    /// <c>debug stack</c>). As linhas são agrupadas até uma janela de silêncio da serial
    /// (<see cref="BancadaEngine.JanelaSilencioMultilinhaPadrao"/>). Lista vazia = nada chegou no prazo de resposta.
    /// </summary>
    /// <param name="Comando">Comando enviado, como foi transmitido.</param>
    /// <param name="Linhas">Linhas recebidas (sem <c>\r</c>/<c>\n</c>, sem espaços nas pontas e sem as linhas vazias).</param>
    public sealed record RespostaMultilinha(string Comando, IReadOnlyList<string> Linhas)
    {
        /// <summary>Diagnóstico de stack, se alguma linha for um <c>[STACK] ...</c> válido.</summary>
        public StackTasks? Stack =>
            Linhas.Select(StackTasks.TentarInterpretar).FirstOrDefault(d => d != null);
    }
}
