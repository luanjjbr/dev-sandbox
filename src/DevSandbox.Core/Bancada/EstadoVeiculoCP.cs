namespace DevSandbox.Core.Bancada
{
    /// <summary>
    /// Os 11 estados que o veículo emula no Piloto de Controle (CP), conforme a Tabela II da IEC 61851-1.
    /// A ordem e os valores numéricos são os mesmos do <c>CodigoEstadoCP</c> do firmware
    /// (<c>firmware/Bancada/lib/EmuladorVeiculoCP/TabelaEstadosCP.h</c>). Não há Estado F: ele é gerado só pelo EVSE.
    /// </summary>
    public enum EstadoVeiculoCP
    {
        /// <summary>12 V — veículo não conectado (S7 aberto).</summary>
        AX = 0,
        /// <summary>10 V — conectado (standby), limite superior.</summary>
        BXSuperior = 1,
        /// <summary>9 V — conectado (standby), nominal.</summary>
        BXNominal = 2,
        /// <summary>8 V — conectado (standby), limite inferior.</summary>
        BXInferior = 3,
        /// <summary>7 V — carga sem ventilação, limite superior.</summary>
        CXSuperior = 4,
        /// <summary>6 V — carga sem ventilação, nominal.</summary>
        CXNominal = 5,
        /// <summary>5 V — carga sem ventilação, limite inferior.</summary>
        CXInferior = 6,
        /// <summary>4 V — carga com ventilação, limite superior.</summary>
        DXSuperior = 7,
        /// <summary>3 V — carga com ventilação, nominal.</summary>
        DXNominal = 8,
        /// <summary>2 V — carga com ventilação, limite inferior.</summary>
        DXInferior = 9,
        /// <summary>1 V — falha / curto CP-PE antes do diodo (S8).</summary>
        E = 10
    }

    /// <summary>Estado primário do CP (nível 1 da seleção na UI).</summary>
    public enum GrupoEstadoCP
    {
        A,
        B,
        C,
        D,
        E
    }

    /// <summary>
    /// Variação de tolerância do estado (nível 2 da seleção na UI). Só B, C e D têm Superior/Inferior;
    /// A e E são sempre <see cref="Nominal"/>.
    /// </summary>
    public enum VariacaoEstadoCP
    {
        Superior,
        Nominal,
        Inferior
    }

    public static class EstadoVeiculoCPExtensions
    {
        /// <summary>Os 11 estados na ordem do firmware (AX, BX+, BX, BX-, …, E).</summary>
        public static IReadOnlyList<EstadoVeiculoCP> Todos { get; } =
        [
            EstadoVeiculoCP.AX,
            EstadoVeiculoCP.BXSuperior, EstadoVeiculoCP.BXNominal, EstadoVeiculoCP.BXInferior,
            EstadoVeiculoCP.CXSuperior, EstadoVeiculoCP.CXNominal, EstadoVeiculoCP.CXInferior,
            EstadoVeiculoCP.DXSuperior, EstadoVeiculoCP.DXNominal, EstadoVeiculoCP.DXInferior,
            EstadoVeiculoCP.E
        ];

        public static string ObterDescricao(this EstadoVeiculoCP estado) => estado switch
        {
            EstadoVeiculoCP.AX         => "AX - Veículo não conectado (+12 V)",
            EstadoVeiculoCP.BXSuperior => "BX+ - Conectado (standby), limite superior (+10 V)",
            EstadoVeiculoCP.BXNominal  => "BX - Conectado (standby), nominal (+9 V)",
            EstadoVeiculoCP.BXInferior => "BX- - Conectado (standby), limite inferior (+8 V)",
            EstadoVeiculoCP.CXSuperior => "CX+ - Carga sem ventilação, limite superior (+7 V)",
            EstadoVeiculoCP.CXNominal  => "CX - Carga sem ventilação, nominal (+6 V)",
            EstadoVeiculoCP.CXInferior => "CX- - Carga sem ventilação, limite inferior (+5 V)",
            EstadoVeiculoCP.DXSuperior => "DX+ - Carga com ventilação, limite superior (+4 V)",
            EstadoVeiculoCP.DXNominal  => "DX - Carga com ventilação, nominal (+3 V)",
            EstadoVeiculoCP.DXInferior => "DX- - Carga com ventilação, limite inferior (+2 V)",
            EstadoVeiculoCP.E          => "E - Falha / curto CP-PE (+1 V)",
            _                          => "Desconhecido"
        };

        /// <summary>Tensão nominal do CP em volts (Tabela II da IEC 61851-1).</summary>
        public static double ObterTensaoNominal(this EstadoVeiculoCP estado) => estado switch
        {
            EstadoVeiculoCP.AX         => 12.0,
            EstadoVeiculoCP.BXSuperior => 10.0,
            EstadoVeiculoCP.BXNominal  => 9.0,
            EstadoVeiculoCP.BXInferior => 8.0,
            EstadoVeiculoCP.CXSuperior => 7.0,
            EstadoVeiculoCP.CXNominal  => 6.0,
            EstadoVeiculoCP.CXInferior => 5.0,
            EstadoVeiculoCP.DXSuperior => 4.0,
            EstadoVeiculoCP.DXNominal  => 3.0,
            EstadoVeiculoCP.DXInferior => 2.0,
            EstadoVeiculoCP.E          => 1.0,
            _ => throw new ArgumentOutOfRangeException(nameof(estado), estado, "Estado CP desconhecido.")
        };

        public static GrupoEstadoCP ObterGrupo(this EstadoVeiculoCP estado) => estado switch
        {
            EstadoVeiculoCP.AX => GrupoEstadoCP.A,
            EstadoVeiculoCP.BXSuperior or EstadoVeiculoCP.BXNominal or EstadoVeiculoCP.BXInferior => GrupoEstadoCP.B,
            EstadoVeiculoCP.CXSuperior or EstadoVeiculoCP.CXNominal or EstadoVeiculoCP.CXInferior => GrupoEstadoCP.C,
            EstadoVeiculoCP.DXSuperior or EstadoVeiculoCP.DXNominal or EstadoVeiculoCP.DXInferior => GrupoEstadoCP.D,
            EstadoVeiculoCP.E => GrupoEstadoCP.E,
            _ => throw new ArgumentOutOfRangeException(nameof(estado), estado, "Estado CP desconhecido.")
        };

        public static VariacaoEstadoCP ObterVariacao(this EstadoVeiculoCP estado) => estado switch
        {
            EstadoVeiculoCP.BXSuperior or EstadoVeiculoCP.CXSuperior or EstadoVeiculoCP.DXSuperior => VariacaoEstadoCP.Superior,
            EstadoVeiculoCP.BXInferior or EstadoVeiculoCP.CXInferior or EstadoVeiculoCP.DXInferior => VariacaoEstadoCP.Inferior,
            EstadoVeiculoCP.AX or EstadoVeiculoCP.BXNominal or EstadoVeiculoCP.CXNominal
                or EstadoVeiculoCP.DXNominal or EstadoVeiculoCP.E => VariacaoEstadoCP.Nominal,
            _ => throw new ArgumentOutOfRangeException(nameof(estado), estado, "Estado CP desconhecido.")
        };

        /// <summary><c>true</c> para os grupos B, C e D (os que têm limite superior/inferior).</summary>
        public static bool TemVariacao(this GrupoEstadoCP grupo) =>
            grupo is GrupoEstadoCP.B or GrupoEstadoCP.C or GrupoEstadoCP.D;

        /// <summary>
        /// Combina a seleção em dois níveis da UI. Para A e E a variação é ignorada (sempre AX / E).
        /// </summary>
        public static EstadoVeiculoCP ObterEstado(GrupoEstadoCP grupo, VariacaoEstadoCP variacao)
        {
            if (!Enum.IsDefined(variacao))
                throw new ArgumentOutOfRangeException(nameof(variacao), variacao, "Variação desconhecida.");

            // Mesma regra do firmware: base do grupo = Superior; +1 Nominal; +2 Inferior
            int deslocamento = (int)variacao;
            return grupo switch
            {
                GrupoEstadoCP.A => EstadoVeiculoCP.AX,
                GrupoEstadoCP.B => EstadoVeiculoCP.BXSuperior + deslocamento,
                GrupoEstadoCP.C => EstadoVeiculoCP.CXSuperior + deslocamento,
                GrupoEstadoCP.D => EstadoVeiculoCP.DXSuperior + deslocamento,
                GrupoEstadoCP.E => EstadoVeiculoCP.E,
                _ => throw new ArgumentOutOfRangeException(nameof(grupo), grupo, "Grupo desconhecido.")
            };
        }

        /// <summary>Código curto, como no <c>[STATUS CP]</c> do firmware: AX, BX+, BX, BX-, …, E.</summary>
        public static string ObterCodigo(this EstadoVeiculoCP estado) => estado switch
        {
            EstadoVeiculoCP.AX         => "AX",
            EstadoVeiculoCP.BXSuperior => "BX+",
            EstadoVeiculoCP.BXNominal  => "BX",
            EstadoVeiculoCP.BXInferior => "BX-",
            EstadoVeiculoCP.CXSuperior => "CX+",
            EstadoVeiculoCP.CXNominal  => "CX",
            EstadoVeiculoCP.CXInferior => "CX-",
            EstadoVeiculoCP.DXSuperior => "DX+",
            EstadoVeiculoCP.DXNominal  => "DX",
            EstadoVeiculoCP.DXInferior => "DX-",
            EstadoVeiculoCP.E          => "E",
            _ => throw new ArgumentOutOfRangeException(nameof(estado), estado, "Estado CP desconhecido.")
        };

        /// <summary>Comando serial idêntico ao aceito pelo firmware: <c>estado ax</c>, <c>estado bx+</c>, …, <c>estado e</c>.</summary>
        public static string ObterComandoSerial(this EstadoVeiculoCP estado) =>
            $"{ComandoEstadoPrefixo}{estado.ObterCodigo().ToLowerInvariant()}";

        /// <summary>Comando que pede o relatório do CP ao firmware.</summary>
        public const string ComandoStatus = "estado status";

        private const string ComandoEstadoPrefixo = "estado ";

        /// <summary>
        /// Interpreta o código do estado (AX, BX+, BX, BX-, …, E; sem diferenciar maiúsculas), como aparece no
        /// <c>[STATUS CP]</c>. Não aceita os atalhos a/b/c/d do firmware, nem F.
        /// </summary>
        public static bool TentarInterpretarCodigo(string? codigo, out EstadoVeiculoCP estado)
        {
            string normalizado = codigo?.Trim().ToUpperInvariant() ?? "";
            foreach (EstadoVeiculoCP candidato in Todos)
            {
                if (candidato.ObterCodigo() == normalizado)
                {
                    estado = candidato;
                    return true;
                }
            }

            estado = EstadoVeiculoCP.AX;
            return false;
        }
    }
}
