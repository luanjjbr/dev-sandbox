using System;
using dev_sandbox.Core;
using NUnit.Framework;

namespace PomodoroEngineTests
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
        public void HexParaDecimal_DeveLancarExcecao_QuandoTextoVazio()
        {
            Assert.Throws<ArgumentException>(() => _calculadora.HexParaDecimal(""));
        }

        [Test]
        public void HexParaDecimal_DeveLancarExcecao_QuandoFormatoInvalido()
        {
            Assert.Throws<FormatException>(() => _calculadora.HexParaDecimal("G12"));
        }

        [Test]
        public void Teste_NumeroMuitoGrande_Dispara_OverflowException()
        {
            Assert.Throws<OverflowException>(() => _calculadora.HexParaDecimal("FFFFFFFFF"));
        }
    }
}