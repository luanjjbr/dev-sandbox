using System;
using System.Globalization;
using System.Text;

namespace DevSandbox.Core.Converters
{
    public class CalculadoraHex
    {
        /// <summary>
        /// Converte uma representação hexadecimal (com ou sem prefixo '0x'/'0X', suportando separadores como espaços ou underscores) para decimal (64 bits).
        /// Suporta valores de 64 bits completos (0x0 até 0xFFFFFFFFFFFFFFFF).
        /// </summary>
        public long HexParaDecimal(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
            {
                throw new ArgumentException("O valor hexadecimal não pode ser vazio.", nameof(hex));
            }

            string normalizado = hex.Trim().Replace(" ", "").Replace("_", "").Replace("-", "");
            if (normalizado.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                normalizado = normalizado.Substring(2);
            }

            if (string.IsNullOrWhiteSpace(normalizado))
            {
                throw new ArgumentException("O valor hexadecimal não contém dígitos válidos.", nameof(hex));
            }

            ulong valorUnsigned = ulong.Parse(normalizado, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return unchecked((long)valorUnsigned);
        }

        /// <summary>
        /// Converte uma representação binária (com ou sem prefixo '0b'/'0B', suportando espaços e underscores) para decimal (64 bits).
        /// </summary>
        public long BinarioParaDecimal(string binario)
        {
            if (string.IsNullOrWhiteSpace(binario))
            {
                throw new ArgumentException("O valor binário não pode ser vazio.", nameof(binario));
            }

            string normalizado = binario.Trim().Replace(" ", "").Replace("_", "").Replace("-", "");
            if (normalizado.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
            {
                normalizado = normalizado.Substring(2);
            }

            if (string.IsNullOrWhiteSpace(normalizado))
            {
                throw new ArgumentException("O valor binário não contém dígitos válidos.", nameof(binario));
            }

            if (normalizado.Length > 64)
            {
                throw new OverflowException("O valor binário excede o limite de 64 bits.");
            }

            ulong resultado = 0;
            foreach (char c in normalizado)
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
            return valor.ToString("X");
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
    }
}

