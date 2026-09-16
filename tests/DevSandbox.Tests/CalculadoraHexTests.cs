using System;
using DevSandbox.Core.Converters;
using NUnit.Framework;

namespace DevSandbox.Tests
{
    [TestFixture]
    public class CalculadoraHexTests
    {
        private CalculadoraHex _calculadora;

        [SetUp]
        public void Setup()
        {
            _calculadora = new CalculadoraHex();
        }

        [Test]
        public void HexParaDecimal_DeveConverterValoresValidos()
        {
            Assert.That(_calculadora.HexParaDecimal("FF"), Is.EqualTo(255));
            Assert.That(_calculadora.HexParaDecimal("1A"), Is.EqualTo(26));
            Assert.That(_calculadora.HexParaDecimal("0"), Is.EqualTo(0));
        }

        [Test]
        public void HexParaDecimal_DeveSuportarPrefixo0xEEspacosESeparadores()
        {
            Assert.That(_calculadora.HexParaDecimal("0xFF"), Is.EqualTo(255));
            Assert.That(_calculadora.HexParaDecimal("0X1A"), Is.EqualTo(26));
            Assert.That(_calculadora.HexParaDecimal("  FF  "), Is.EqualTo(255));
            Assert.That(_calculadora.HexParaDecimal("0xDEAD_BEEF"), Is.EqualTo(3735928559L));
            Assert.That(_calculadora.HexParaDecimal("DE-AD-BE-EF"), Is.EqualTo(3735928559L));
            Assert.That(_calculadora.HexParaDecimal("DE AD BE EF"), Is.EqualTo(3735928559L));
        }

        [Test]
        public void HexParaDecimal_DeveSuportarEnderecos32BitsSemOverflow()
        {
            // 0xFFFFFFFF em 32 bits não deve estourar
            Assert.That(_calculadora.HexParaDecimal("FFFFFFFF"), Is.EqualTo(4294967295L));
        }

        [Test]
        public void HexParaDecimal_DeveSuportarValoresCompletos64BitsSemEstouro()
        {
            // 0xFFFFFFFFFFFFFFFF (16 digitos Hex) representa -1L em complemento de dois
            Assert.That(_calculadora.HexParaDecimal("FFFFFFFFFFFFFFFF"), Is.EqualTo(-1L));
            Assert.That(_calculadora.HexParaDecimal("8000000000000000"), Is.EqualTo(long.MinValue));
            Assert.That(_calculadora.HexParaDecimal("7FFFFFFFFFFFFFFF"), Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void HexParaDecimal_DeveLancarExcecao_QuandoTextoVazio()
        {
            Assert.Throws<ArgumentException>(() => _calculadora.HexParaDecimal(""));
            Assert.Throws<ArgumentException>(() => _calculadora.HexParaDecimal("   "));
            Assert.Throws<ArgumentException>(() => _calculadora.HexParaDecimal("0x"));
            Assert.Throws<ArgumentException>(() => _calculadora.HexParaDecimal("0X_--"));
        }

        [Test]
        public void HexParaDecimal_DeveLancarExcecao_QuandoFormatoInvalido()
        {
            Assert.Throws<FormatException>(() => _calculadora.HexParaDecimal("G12"));
            Assert.Throws<FormatException>(() => _calculadora.HexParaDecimal("XYZ"));
        }

        [Test]
        public void HexParaDecimal_DeveLancarOverflowException_QuandoUltrapassa64Bits()
        {
            // Valor superior a 16 dígitos hexadecimais (64 bits)
            Assert.Throws<OverflowException>(() => _calculadora.HexParaDecimal("FFFFFFFFFFFFFFFFF"));
        }

        [Test]
        public void DecimalParaHex_DeveConverterCorretamente()
        {
            Assert.That(_calculadora.DecimalParaHex(255), Is.EqualTo("FF"));
            Assert.That(_calculadora.DecimalParaHex(26), Is.EqualTo("1A"));
            Assert.That(_calculadora.DecimalParaHex(0), Is.EqualTo("0"));
            Assert.That(_calculadora.DecimalParaHex(-1L), Is.EqualTo("FFFFFFFFFFFFFFFF"));
        }

        [Test]
        public void DecimalParaBinario_DeveConverterCorretamente()
        {
            Assert.That(_calculadora.DecimalParaBinario(10), Is.EqualTo("1010"));
            Assert.That(_calculadora.DecimalParaBinario(255), Is.EqualTo("11111111"));
            Assert.That(_calculadora.DecimalParaBinario(0), Is.EqualTo("0"));
        }

        [Test]
        public void BinarioParaDecimal_DeveConverterValoresValidos()
        {
            Assert.That(_calculadora.BinarioParaDecimal("1010"), Is.EqualTo(10L));
            Assert.That(_calculadora.BinarioParaDecimal("0b11110000"), Is.EqualTo(240L));
            Assert.That(_calculadora.BinarioParaDecimal("0B1010_0101"), Is.EqualTo(165L));
            Assert.That(_calculadora.BinarioParaDecimal("1111 0000 1010 0101"), Is.EqualTo(61605L));
        }

        [Test]
        public void BinarioParaDecimal_DeveLancarExcecoesApropriadas()
        {
            Assert.Throws<ArgumentException>(() => _calculadora.BinarioParaDecimal(""));
            Assert.Throws<ArgumentException>(() => _calculadora.BinarioParaDecimal("0b"));
            Assert.Throws<FormatException>(() => _calculadora.BinarioParaDecimal("10102"));
            Assert.Throws<OverflowException>(() => _calculadora.BinarioParaDecimal(new string('1', 65)));
        }

        [Test]
        public void BinarioParaHex_DeveConverterDiretamente()
        {
            Assert.That(_calculadora.BinarioParaHex("1010"), Is.EqualTo("A"));
            Assert.That(_calculadora.BinarioParaHex("11111111"), Is.EqualTo("FF"));
            Assert.That(_calculadora.BinarioParaHex("0b1111_0000"), Is.EqualTo("F0"));
        }

        [Test]
        public void FormatarBinario_DeveAgruparBitsEmNibbles()
        {
            Assert.That(_calculadora.FormatarBinario("10101111"), Is.EqualTo("1010 1111"));
            Assert.That(_calculadora.FormatarBinario("10101"), Is.EqualTo("1 0101"));
            Assert.That(_calculadora.FormatarBinario("0b11110000"), Is.EqualTo("1111 0000"));
            Assert.That(_calculadora.FormatarBinario(""), Is.EqualTo(""));
        }
    }
}
