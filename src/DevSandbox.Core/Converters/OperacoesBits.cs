using System.Buffers.Binary;

namespace DevSandbox.Core.Converters
{
    /// <summary>
    /// Operações de bits de 64 bits da Calculadora (matriz 4×16, Swap Endian, deslocamentos, NOT e vistas).
    /// Os bits são numerados de 0 (LSB) a 63 (MSB).
    /// </summary>
    public static class OperacoesBits
    {
        /// <summary>Quantidade de bits do valor.</summary>
        public const int TotalBits = 64;

        /// <summary>Todos os bits ligados (<see cref="ulong.MaxValue"/>).</summary>
        public const ulong Maximo = ulong.MaxValue;

        /// <summary>Inverte o bit <paramref name="bit"/> (0 = LSB, 63 = MSB).</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bit"/> fora de 0–63.</exception>
        public static ulong InverterBit(ulong valor, int bit)
        {
            ValidarBit(bit);
            return valor ^ (1UL << bit);
        }

        /// <summary><c>true</c> se o bit <paramref name="bit"/> (0 = LSB, 63 = MSB) está ligado.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bit"/> fora de 0–63.</exception>
        public static bool LerBit(ulong valor, int bit)
        {
            ValidarBit(bit);
            return (valor & (1UL << bit)) != 0;
        }

        /// <summary>Inverte a ordem dos 8 bytes (Swap Endian).</summary>
        public static ulong TrocarEndian(ulong valor) => BinaryPrimitives.ReverseEndianness(valor);

        /// <summary>Deslocamento de 1 bit para a esquerda (o MSB é descartado, entra 0 no LSB).</summary>
        public static ulong DeslocarEsquerda(ulong valor) => valor << 1;

        /// <summary>Deslocamento lógico de 1 bit para a direita (o LSB é descartado, entra 0 no MSB).</summary>
        public static ulong DeslocarDireita(ulong valor) => valor >> 1;

        /// <summary>Complemento bit a bit.</summary>
        public static ulong Not(ulong valor) => ~valor;

        /// <summary>
        /// Os 8 bytes como texto ASCII em big-endian (byte mais significativo primeiro). Byte fora do ASCII
        /// imprimível (0x20–0x7E) vira <c>.</c>. Sempre retorna 8 caracteres.
        /// </summary>
        public static string ParaAscii(ulong valor)
        {
            Span<byte> bytes = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64BigEndian(bytes, valor);

            Span<char> texto = stackalloc char[sizeof(ulong)];
            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                texto[i] = b is >= 0x20 and <= 0x7E ? (char)b : '.';
            }

            return new string(texto);
        }

        /// <summary>O mesmo padrão de bits lido como inteiro com sinal (complemento de dois).</summary>
        public static long ParaInt64(ulong valor) => unchecked((long)valor);

        private static void ValidarBit(int bit)
        {
            if (bit is < 0 or >= TotalBits)
                throw new ArgumentOutOfRangeException(nameof(bit), bit, "O bit deve estar entre 0 e 63.");
        }
    }
}
