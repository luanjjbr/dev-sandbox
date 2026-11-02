namespace DevSandbox.Core.Bancada
{
    /// <summary>Classificação de uma linha recebida do firmware da Bancada.</summary>
    public enum TipoRespostaFirmware
    {
        /// <summary><c>OK</c>: comando de cabo ou de estado aplicado.</summary>
        Ok,
        /// <summary>Linha <c>ERRO</c> ou <c>ERRO: ...</c>.</summary>
        Erro,
        /// <summary><c>[STATUS CP] Estado: ...</c> (resposta a <c>estado status</c>).</summary>
        StatusCP,
        /// <summary><c>[STATUS CABO] Canal: ...</c> (resposta a <c>cabo status</c>).</summary>
        StatusCabo,
        /// <summary>Qualquer outra linha (menu de ajuda, banner de boot, <c>[STACK]</c>, ...).</summary>
        Outra
    }

    /// <summary>Motivo de uma linha <c>ERRO</c>, conforme os textos do <c>main.cpp</c> do firmware.</summary>
    public enum MotivoErroFirmware
    {
        /// <summary>Não é erro.</summary>
        Nenhum,
        /// <summary><c>ERRO</c>: comando desconhecido.</summary>
        ComandoDesconhecido,
        /// <summary><c>ERRO: Estado</c>: argumento de <c>estado</c> inválido ou falha ao ativar.</summary>
        Estado,
        /// <summary><c>ERRO: Cabo</c>: argumento de <c>cabo</c> inválido.</summary>
        Cabo,
        /// <summary><c>ERRO: CP indisponivel no Nano</c>: placa sem os relés do CP (use o Mega).</summary>
        CPIndisponivel,
        /// <summary><c>ERRO: Comando longo</c>: linha com mais de 31 caracteres, descartada.</summary>
        ComandoLongo,
        /// <summary>Outra mensagem após <c>ERRO:</c>.</summary>
        Outro
    }

    /// <summary>
    /// Uma linha de resposta do firmware já interpretada. Use <see cref="Interpretar"/>.
    /// </summary>
    /// <param name="Tipo">Classificação da linha.</param>
    /// <param name="Linha">Texto da linha, sem <c>\r</c>/<c>\n</c> e sem espaços nas pontas.</param>
    /// <param name="Motivo">Motivo do erro (<see cref="MotivoErroFirmware.Nenhum"/> se não for erro).</param>
    /// <param name="EstadoCP">Estado lido de um <c>[STATUS CP]</c>, se reconhecido.</param>
    /// <param name="CaboPP">Canal lido de um <c>[STATUS CABO]</c>, se reconhecido.</param>
    public sealed record RespostaFirmware(
        TipoRespostaFirmware Tipo,
        string Linha,
        MotivoErroFirmware Motivo = MotivoErroFirmware.Nenhum,
        EstadoVeiculoCP? EstadoCP = null,
        EstadoCaboPP? CaboPP = null)
    {
        // Literais copiados de firmware/Bancada/src/main.cpp e EmuladorVeiculoCP.cpp / EmuladorCabo.cpp
        public const string TextoOk = "OK";
        public const string TextoErro = "ERRO";
        public const string TextoErroEstado = "ERRO: Estado";
        public const string TextoErroCabo = "ERRO: Cabo";
        public const string TextoErroCPIndisponivel = "ERRO: CP indisponivel no Nano";
        public const string TextoErroComandoLongo = "ERRO: Comando longo";
        public const string PrefixoStatusCP = "[STATUS CP] Estado: ";
        public const string PrefixoStatusCabo = "[STATUS CABO] Canal: ";

        public bool EhErro => Tipo == TipoRespostaFirmware.Erro;

        /// <summary>Interpreta uma linha recebida do firmware.</summary>
        public static RespostaFirmware Interpretar(string? linha)
        {
            string texto = (linha ?? "").Trim();

            if (texto == TextoOk)
                return new RespostaFirmware(TipoRespostaFirmware.Ok, texto);

            if (texto == TextoErro)
                return new RespostaFirmware(TipoRespostaFirmware.Erro, texto, MotivoErroFirmware.ComandoDesconhecido);

            if (texto.StartsWith(TextoErro + ":", StringComparison.Ordinal))
                return new RespostaFirmware(TipoRespostaFirmware.Erro, texto, ClassificarErro(texto));

            if (texto.StartsWith(PrefixoStatusCP, StringComparison.Ordinal))
            {
                // "[STATUS CP] Estado: BX+ | Vcp: 10 V | Conectado, limite superior | Reles: S7"
                string codigo = PrimeiroCampo(texto.Substring(PrefixoStatusCP.Length));
                EstadoVeiculoCP? estado = EstadoVeiculoCPExtensions.TentarInterpretarCodigo(codigo, out var e) ? e : null;
                return new RespostaFirmware(TipoRespostaFirmware.StatusCP, texto, EstadoCP: estado);
            }

            if (texto.StartsWith(PrefixoStatusCabo, StringComparison.Ordinal))
            {
                // "[STATUS CABO] Canal: 4 | R: 220 Ohms | Imax: 32 A | Descricao: ..."
                string canal = PrimeiroCampo(texto.Substring(PrefixoStatusCabo.Length));
                EstadoCaboPP? cabo = int.TryParse(canal, out int n) && Enum.IsDefined(typeof(EstadoCaboPP), n)
                    ? (EstadoCaboPP)n
                    : null;
                return new RespostaFirmware(TipoRespostaFirmware.StatusCabo, texto, CaboPP: cabo);
            }

            return new RespostaFirmware(TipoRespostaFirmware.Outra, texto);
        }

        private static MotivoErroFirmware ClassificarErro(string texto) => texto switch
        {
            TextoErroEstado => MotivoErroFirmware.Estado,
            TextoErroCabo => MotivoErroFirmware.Cabo,
            TextoErroCPIndisponivel => MotivoErroFirmware.CPIndisponivel,
            TextoErroComandoLongo => MotivoErroFirmware.ComandoLongo,
            _ => MotivoErroFirmware.Outro
        };

        private static string PrimeiroCampo(string resto)
        {
            int separador = resto.IndexOf('|');
            return (separador >= 0 ? resto.Substring(0, separador) : resto).Trim();
        }
    }
}
