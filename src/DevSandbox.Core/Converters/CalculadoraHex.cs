using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace DevSandbox.Core.Converters
{
    /// <summary>
    /// Conversões entre hexadecimal, decimal e binário em 64 bits (sem sinal na entrada).
    /// </summary>
    /// <remarks>
    /// Convenção de entrada: espaços, tabs e <c>_</c> são separadores livres; <c>-</c> só é aceito como separador
    /// entre dígitos (<c>FF-FF</c>, <c>1100-0011</c>). Sinal negativo é recusado com <see cref="FormatException"/>.
    /// Entradas sem nenhum dígito (vazias, só prefixo ou só separadores) lançam <see cref="ArgumentException"/>.
    /// </remarks>
    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "API pública de instância usada pela FormCalculadora e pelos testes; tornar os métodos " +
                        "estáticos quebraria as chamadas existentes sem ganho mensurável.")]
    public sealed class CalculadoraHex
    {
        public const string MensagemNegativo =
            "Valores negativos não são suportados. Forneça inteiros positivos ou hexadecimal sem sinal.";

        public const string MensagemHifen =
            "O hífen só é aceito como separador entre dígitos (ex.: FF-FF).";

        /// <summary>
        /// Converte uma representação hexadecimal (com ou sem prefixo '0x'/'0X', com separadores) para decimal (64 bits).
        /// Suporta valores de 64 bits completos (0x0 até 0xFFFFFFFFFFFFFFFF; acima de 0x7FFF... o resultado é negativo em complemento de dois).
        /// </summary>
        public long HexParaDecimal(string hex)
        {
            string digitos = Normalizar(hex, "0x", "hexadecimal", nameof(hex));

            ulong valorUnsigned = ulong.Parse(digitos, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
            return unchecked((long)valorUnsigned);
        }

        /// <summary>
        /// Converte uma representação binária (com ou sem prefixo '0b'/'0B', com separadores) para decimal (64 bits).
        /// </summary>
        public long BinarioParaDecimal(string binario)
        {
            string digitos = Normalizar(binario, "0b", "binário", nameof(binario));

            // Zeros à esquerda não contam para o limite de 64 bits
            if (digitos.TrimStart('0').Length > 64)
            {
                throw new OverflowException("O valor binário excede o limite de 64 bits.");
            }

            ulong resultado = 0;
            foreach (char c in digitos)
            {
                if (c != '0' && c != '1')
                {
                    throw new FormatException($"Caractere inválido '{c}' encontrado na sequência binária.");
                }
                resultado = (resultado << 1) | (ulong)(uint)(c - '0');
            }

            return unchecked((long)resultado);
        }

        /// <summary>
        /// Converte uma representação decimal sem sinal (0 até 18446744073709551615, com separadores) para 64 bits.
        /// Valores acima de <see cref="long.MaxValue"/> resultam negativos em complemento de dois.
        /// </summary>
        public long DecimalParaLong(string dec)
        {
            string digitos = Normalizar(dec, null, "decimal", nameof(dec));

            ulong valorUnsigned = ulong.Parse(digitos, NumberStyles.None, CultureInfo.InvariantCulture);
            return unchecked((long)valorUnsigned);
        }

        /// <summary>
        /// Detecta a base da entrada: prefixo '0x' → hexadecimal; prefixo '0b' → binário;
        /// presença de letras A–F → hexadecimal; caso contrário → decimal.
        /// </summary>
        public BaseNumerica DetectarBase(string entrada)
        {
            if (string.IsNullOrWhiteSpace(entrada))
            {
                throw new ArgumentException("Digite um valor para converter.", nameof(entrada));
            }

            // O sinal é ignorado na detecção; a conversão é que recusa negativos com mensagem clara.
            string texto = entrada.Trim().TrimStart('-');

            if (texto.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return BaseNumerica.Hexadecimal;
            // "0b" só é prefixo binário se o resto for apenas 0/1 e separadores; "0BAD" e "0B12" são hexadecimais
            if (texto.StartsWith("0b", StringComparison.OrdinalIgnoreCase) && ApenasBitsESeparadores(texto.AsSpan(2)))
                return BaseNumerica.Binario;

            foreach (char c in texto)
            {
                if (char.IsAsciiHexDigit(c) && !char.IsAsciiDigit(c)) return BaseNumerica.Hexadecimal;
            }

            return BaseNumerica.Decimal;
        }

        private static bool ApenasBitsESeparadores(ReadOnlySpan<char> resto)
        {
            foreach (char c in resto)
            {
                if (c is not ('0' or '1' or '-' or '_') && !char.IsWhiteSpace(c)) return false;
            }
            return true;
        }

        /// <summary>
        /// Detecta a base da entrada (<see cref="DetectarBase"/>) e converte para 64 bits.
        /// </summary>
        public long ConverterEntrada(string entrada, out BaseNumerica baseDetectada)
        {
            baseDetectada = DetectarBase(entrada);
            return Converter(entrada, baseDetectada);
        }

        /// <summary>Converte a entrada na base informada.</summary>
        public long Converter(string entrada, BaseNumerica baseNumerica) => baseNumerica switch
        {
            BaseNumerica.Hexadecimal => HexParaDecimal(entrada),
            BaseNumerica.Binario => BinarioParaDecimal(entrada),
            BaseNumerica.Decimal => DecimalParaLong(entrada),
            _ => throw new ArgumentOutOfRangeException(nameof(baseNumerica), baseNumerica, "Base numérica desconhecida.")
        };

        /// <summary>
        /// Converte uma representação binária diretamente para hexadecimal maiúsculo.
        /// </summary>
        public string BinarioParaHex(string binario)
        {
            long valor = BinarioParaDecimal(binario);
            return DecimalParaHex(valor);
        }

        /// <summary>
        /// Converte um valor decimal em representação hexadecimal maiúscula.
        /// </summary>
        public string DecimalParaHex(long valor)
        {
            return valor.ToString("X", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converte um valor decimal em representação binária.
        /// </summary>
        public string DecimalParaBinario(long valor)
        {
            return Convert.ToString(valor, 2);
        }

        /// <summary>
        /// Formata uma cadeia binária agrupando os bits (padrão em nibbles de 4 bits) para facilitar a leitura.
        /// </summary>
        public string FormatarBinario(string binario, int agrupamento = 4)
        {
            if (string.IsNullOrWhiteSpace(binario) || agrupamento <= 0)
                return binario;

            string limpo = binario.Trim().Replace(" ", "").Replace("_", "").Replace("-", "");
            if (limpo.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
                limpo = limpo.Substring(2);

            int resto = limpo.Length % agrupamento;
            var sb = new StringBuilder();

            int i = 0;
            if (resto > 0)
            {
                sb.Append(limpo.AsSpan(0, resto));
                i = resto;
            }

            while (i < limpo.Length)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(limpo.AsSpan(i, agrupamento));
                i += agrupamento;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Aplica a convenção de entrada e devolve apenas os dígitos (sem prefixo e sem separadores).
        /// A validação dos dígitos em si fica com o conversor de cada base.
        /// </summary>
        private static string Normalizar(string entrada, string? prefixo, string nomeBase, string nomeParametro)
        {
            if (string.IsNullOrWhiteSpace(entrada))
            {
                throw new ArgumentException($"O valor {nomeBase} não pode ser vazio.", nomeParametro);
            }

            // Espaços em branco (inclusive tab) e '_' são separadores livres
            string texto = string.Concat(entrada.Trim().Where(c => c != '_' && !char.IsWhiteSpace(c)));

            // Sinal antes do prefixo ("-0xFF", "-FF")
            int hifensIniciais = texto.Length - texto.TrimStart('-').Length;
            string corpo = texto.Substring(hifensIniciais);

            if (prefixo != null && corpo.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase))
            {
                corpo = corpo.Substring(prefixo.Length);
            }

            // Sinal depois do prefixo ("0x-FF", "0b-1010")
            int hifensAposPrefixo = corpo.Length - corpo.TrimStart('-').Length;

            if (corpo.Replace("-", "").Length == 0)
            {
                throw new ArgumentException($"O valor {nomeBase} não contém dígitos válidos.", nomeParametro);
            }

            if (hifensIniciais + hifensAposPrefixo == 1)
            {
                throw new FormatException(MensagemNegativo);
            }

            if (hifensIniciais + hifensAposPrefixo > 1 || corpo.EndsWith('-') || corpo.Contains("--", StringComparison.Ordinal))
            {
                throw new FormatException(MensagemHifen);
            }

            return corpo.Replace("-", "");
        }
    }
}
