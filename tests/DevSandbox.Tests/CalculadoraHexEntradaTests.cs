using System;
using DevSandbox.Core.Converters;
using NUnit.Framework;

namespace DevSandbox.Tests
{
    /// <summary>
    /// Convenção de sinal/hífen e detecção de base trazidas da FormCalculadora para o Core.
    /// </summary>
    [TestFixture]
    public class CalculadoraHexEntradaTests
    {
        private CalculadoraHex _calculadora;

        [SetUp]
        public void Setup()
        {
            _calculadora = new CalculadoraHex();
        }

        [TestCase("-FF")]
        [TestCase("-0xFF")]
        [TestCase("0x-FF")]
        [TestCase(" -1A ")]
        public void HexParaDecimal_Negativo_DeveRecusarComMensagemClara(string entrada)
        {
            var ex = Assert.Throws<FormatException>(() => _calculadora.HexParaDecimal(entrada));
            Assert.That(ex!.Message, Is.EqualTo(CalculadoraHex.MensagemNegativo));
        }

        [TestCase("-0b1010")]
        [TestCase("0b-1010")]
        [TestCase("-1010")]
        public void BinarioParaDecimal_Negativo_DeveRecusarComMensagemClara(string entrada)
        {
            var ex = Assert.Throws<FormatException>(() => _calculadora.BinarioParaDecimal(entrada));
            Assert.That(ex!.Message, Is.EqualTo(CalculadoraHex.MensagemNegativo));
        }

        [TestCase("-5")]
        [TestCase("-9223372036854775808")]
        public void DecimalParaLong_Negativo_DeveRecusarComMensagemClara(string entrada)
        {
            var ex = Assert.Throws<FormatException>(() => _calculadora.DecimalParaLong(entrada));
            Assert.That(ex!.Message, Is.EqualTo(CalculadoraHex.MensagemNegativo));
        }

        [TestCase("FF-AA-12", 0xFFAA12L)]
        [TestCase("0xAA-BB-CC", 0xAABBCCL)]
        [TestCase("DE-AD BE_EF", 0xDEADBEEFL)]
        public void HexParaDecimal_HifenEntreDigitos_DeveAceitar(string entrada, long esperado)
        {
            Assert.That(_calculadora.HexParaDecimal(entrada), Is.EqualTo(esperado));
        }

        [Test]
        public void BinarioParaDecimal_HifenEntreDigitos_DeveAceitar()
        {
            Assert.That(_calculadora.BinarioParaDecimal("1100-0011"), Is.EqualTo(0b1100_0011L));
            Assert.That(_calculadora.BinarioParaDecimal("0b1111-0000"), Is.EqualTo(0xF0L));
        }

        [TestCase("--FF")]
        [TestCase("FF--AA")]
        [TestCase("FF-")]
        [TestCase("0xFF-")]
        public void HexParaDecimal_HifenForaDaPosicaoDeSeparador_DeveRecusar(string entrada)
        {
            var ex = Assert.Throws<FormatException>(() => _calculadora.HexParaDecimal(entrada));
            Assert.That(ex!.Message, Is.EqualTo(CalculadoraHex.MensagemHifen));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("-")]
        [TestCase("--")]
        [TestCase("0x")]
        [TestCase("0x-")]
        public void HexParaDecimal_SemDigitos_DeveLancarArgumentException(string entrada)
        {
            Assert.Throws<ArgumentException>(() => _calculadora.HexParaDecimal(entrada));
        }

        [TestCase("0b")]
        [TestCase("-")]
        public void BinarioParaDecimal_SemDigitos_DeveLancarArgumentException(string entrada)
        {
            Assert.Throws<ArgumentException>(() => _calculadora.BinarioParaDecimal(entrada));
        }

        [Test]
        public void HexParaDecimal_64BitsComBitMaisSignificativo_DeveRetornarMenosUm()
        {
            Assert.That(_calculadora.HexParaDecimal("0xFFFFFFFFFFFFFFFF"), Is.EqualTo(-1L));
            Assert.That(_calculadora.HexParaDecimal("FFFF-FFFF-FFFF-FFFF"), Is.EqualTo(-1L));
        }

        [Test]
        public void DecimalParaLong_DeveConverterValoresSemSinalAte64Bits()
        {
            Assert.That(_calculadora.DecimalParaLong("255"), Is.EqualTo(255L));
            Assert.That(_calculadora.DecimalParaLong("1 000_000"), Is.EqualTo(1_000_000L));
            Assert.That(_calculadora.DecimalParaLong("18446744073709551615"), Is.EqualTo(-1L));
            Assert.Throws<OverflowException>(() => _calculadora.DecimalParaLong("18446744073709551616"));
            Assert.Throws<FormatException>(() => _calculadora.DecimalParaLong("12A"));
            Assert.Throws<FormatException>(() => _calculadora.DecimalParaLong("+5"));
            Assert.Throws<ArgumentException>(() => _calculadora.DecimalParaLong(""));
        }

        [TestCase("0xFF", BaseNumerica.Hexadecimal)]
        [TestCase("0X1a", BaseNumerica.Hexadecimal)]
        [TestCase("DEAD", BaseNumerica.Hexadecimal)]
        [TestCase("ff-ff", BaseNumerica.Hexadecimal)]
        [TestCase("0b1010", BaseNumerica.Binario)]
        [TestCase("0B1111_0000", BaseNumerica.Binario)]
        [TestCase("1010", BaseNumerica.Decimal)]
        [TestCase("1100-0011", BaseNumerica.Decimal)]
        [TestCase("255", BaseNumerica.Decimal)]
        [TestCase("-0b101", BaseNumerica.Binario)]
        [TestCase("-FF", BaseNumerica.Hexadecimal)]
        public void DetectarBase_DeveIdentificarPelaConvencaoDaCalculadora(string entrada, BaseNumerica esperada)
        {
            Assert.That(_calculadora.DetectarBase(entrada), Is.EqualTo(esperada));
        }

        [Test]
        public void DetectarBase_Vazio_DeveLancarArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _calculadora.DetectarBase(""));
            Assert.Throws<ArgumentException>(() => _calculadora.DetectarBase("   "));
        }

        [Test]
        public void ConverterEntrada_DeveDetectarBaseEConverter()
        {
            Assert.That(_calculadora.ConverterEntrada("0xFF", out BaseNumerica b1), Is.EqualTo(255L));
            Assert.That(b1, Is.EqualTo(BaseNumerica.Hexadecimal));

            // "0b1010" contém 'b' (dígito hex): o prefixo binário tem precedência sobre as letras
            Assert.That(_calculadora.ConverterEntrada("0b1010", out BaseNumerica b2), Is.EqualTo(10L));
            Assert.That(b2, Is.EqualTo(BaseNumerica.Binario));

            Assert.That(_calculadora.ConverterEntrada("1010", out BaseNumerica b3), Is.EqualTo(1010L));
            Assert.That(b3, Is.EqualTo(BaseNumerica.Decimal));
        }

        [Test]
        public void ConverterEntrada_Negativo_DeveRecusarEmQualquerBase()
        {
            Assert.Throws<FormatException>(() => _calculadora.ConverterEntrada("-10", out _));
            Assert.Throws<FormatException>(() => _calculadora.ConverterEntrada("-0x10", out _));
            Assert.Throws<FormatException>(() => _calculadora.ConverterEntrada("-0b10", out _));
        }

        [Test]
        public void Converter_BaseInformada_DeveUsarConversorCorrespondente()
        {
            Assert.That(_calculadora.Converter("10", BaseNumerica.Hexadecimal), Is.EqualTo(16L));
            Assert.That(_calculadora.Converter("10", BaseNumerica.Binario), Is.EqualTo(2L));
            Assert.That(_calculadora.Converter("10", BaseNumerica.Decimal), Is.EqualTo(10L));
        }
    }
}
