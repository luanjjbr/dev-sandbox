using System;
using System.IO;
using DevSandbox.Core.Converters;
using DevSandbox.Core.Serial;
using DevSandbox.Tests.Fakes;
using NUnit.Framework;

namespace DevSandbox.Tests
{
    /// <summary>Testes de borda e regressão adicionais (QA etapa 2).</summary>
    [TestFixture]
    public class QaCalculadoraBordasTests
    {
        private CalculadoraHex _calc;

        [SetUp]
        public void Setup() => _calc = new CalculadoraHex();

        [Test]
        public void HexParaDecimal_FFHifenFF_DeveSer0xFFFF()
        {
            Assert.That(_calc.HexParaDecimal("FF-FF"), Is.EqualTo(0xFFFFL));
        }

        [TestCase("7FFFFFFFFFFFFFFF", long.MaxValue)]
        [TestCase("8000000000000000", long.MinValue)]
        [TestCase("FFFFFFFFFFFFFFFF", -1L)]
        [TestCase("0x0000000000000000FF", 255L)]
        public void HexParaDecimal_LimitesDe64Bits(string entrada, long esperado)
        {
            Assert.That(_calc.HexParaDecimal(entrada), Is.EqualTo(esperado));
        }

        [Test]
        public void HexParaDecimal_17DigitosSignificativos_DeveLancarOverflow()
        {
            Assert.Throws<OverflowException>(() => _calc.HexParaDecimal("10000000000000000"));
        }

        [TestCase("0xGG")]
        [TestCase("12 G4")]
        public void HexParaDecimal_DigitoInvalido_DeveLancarFormatException(string entrada)
        {
            Assert.Throws<FormatException>(() => _calc.HexParaDecimal(entrada));
        }

        [TestCase("  0xFF  ", 255L)]
        [TestCase("0x F F", 255L)]
        [TestCase("0X_FF", 255L)]
        public void HexParaDecimal_EspacosESeparadores_SaoIgnorados(string entrada, long esperado)
        {
            Assert.That(_calc.HexParaDecimal(entrada), Is.EqualTo(esperado));
        }

        [Test]
        public void HexParaDecimal_SinalSeparadoPorEspaco_AindaERecusado()
        {
            var ex = Assert.Throws<FormatException>(() => _calc.HexParaDecimal("- FF"));
            Assert.That(ex!.Message, Is.EqualTo(CalculadoraHex.MensagemNegativo));
        }

        [Test]
        public void Binario_64Bits_Limites()
        {
            Assert.That(_calc.BinarioParaDecimal(new string('1', 64)), Is.EqualTo(-1L));
            Assert.That(_calc.BinarioParaDecimal("0b1" + new string('0', 63)), Is.EqualTo(long.MinValue));
        }

        [Test]
        public void Binario_65Digitos_DeveLancarOverflow()
        {
            Assert.Throws<OverflowException>(() => _calc.BinarioParaDecimal("1" + new string('0', 64)));
        }

        [Test]
        public void Binario_DigitoInvalido_DeveLancarFormatException()
        {
            Assert.Throws<FormatException>(() => _calc.BinarioParaDecimal("0b1012"));
        }

        [TestCase("18446744073709551615", -1L)]
        [TestCase("9223372036854775807", long.MaxValue)]
        [TestCase("9223372036854775808", long.MinValue)]
        [TestCase(" 18446744073709551615 ", -1L)]
        public void DecimalParaLong_LimitesDe64Bits(string entrada, long esperado)
        {
            Assert.That(_calc.DecimalParaLong(entrada), Is.EqualTo(esperado));
        }

        [TestCase("18446744073709551616")]
        [TestCase("99999999999999999999")]
        public void DecimalParaLong_AcimaDoMaximo_DeveLancarOverflow(string entrada)
        {
            Assert.Throws<OverflowException>(() => _calc.DecimalParaLong(entrada));
        }

        [Test]
        public void DecimalParaLong_HifenSeparador_Aceita()
        {
            Assert.That(_calc.DecimalParaLong("1-000"), Is.EqualTo(1000L));
        }

        [Test]
        public void Converter_BaseDesconhecida_DeveLancarArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _calc.Converter("1", (BaseNumerica)99));
        }
    }

    /// <summary>Ciclo de vida e reconexão do motor serial com porta simulada.</summary>
    [TestFixture]
    public class QaSerialCicloDeVidaTests
    {
        private const string Porta = "COM7";

        private List<FakeSerialPort> _portas;
        private string[] _listadas;
        private SupervisorioSerialEngine _engine;
        private int _perdas, _reconexoes;

        [SetUp]
        public void Setup()
        {
            _portas = [];
            _listadas = [Porta];
            _perdas = _reconexoes = 0;
            _engine = new SupervisorioSerialEngine(
                (n, b) => { var p = new FakeSerialPort(n, b); _portas.Add(p); return p; },
                () => _listadas,
                intervaloWatchdog: null);
            _engine.ConexaoPerdida += (s, e) => _perdas++;
            _engine.Reconectado += (s, e) => _reconexoes++;
        }

        [TearDown]
        public void TearDown() => _engine.Dispose();

        [Test]
        public void Dispose_ComPortaAberta_DescartaPortaEFicaIdempotente()
        {
            _engine.Conectar(Porta, 9600);
            var porta = _portas[0];

            _engine.Dispose();
            _engine.Dispose();

            Assert.That(porta.Descartada, Is.True);
            Assert.That(porta.AssinantesDataReceived, Is.EqualTo(0));
            Assert.That(_engine.EstaConectado, Is.False);
            Assert.That(_perdas, Is.EqualTo(0), "Dispose não deve sinalizar perda de conexão.");
        }

        [Test]
        public void AposDispose_EnviarDadosRetornaFalse_EWatchdogNaoAge()
        {
            _engine.Conectar(Porta, 9600);
            _engine.Dispose();
            _listadas = [];

            Assert.That(_engine.EnviarDados("x"), Is.False);
            Assert.DoesNotThrow(() => _engine.ExecutarCicloWatchdog());
            Assert.DoesNotThrow(() => _engine.Desconectar());
            Assert.That(_perdas, Is.EqualTo(0));
            Assert.That(_reconexoes, Is.EqualTo(0));
        }

        [Test]
        public void PerdaEReconexao_Repetidas_CadaCicloGeraUmEventoDeCada()
        {
            _engine.Conectar(Porta, 9600);

            for (int i = 1; i <= 3; i++)
            {
                _listadas = [];
                _engine.ExecutarCicloWatchdog();
                _listadas = [Porta];
                _engine.ExecutarCicloWatchdog();

                Assert.That(_perdas, Is.EqualTo(i));
                Assert.That(_reconexoes, Is.EqualTo(i));
                Assert.That(_engine.EstaConectado, Is.True);
            }

            Assert.That(_portas, Has.Count.EqualTo(4));
            Assert.That(_portas.Take(3).All(p => p.Descartada), Is.True, "Portas antigas devem ser descartadas.");
        }

        [Test]
        public void ReconexaoAposFalhaDeEscrita_DeveVoltarAEnviar()
        {
            _engine.Conectar(Porta, 9600);
            _portas[0].FalhaNaEscrita = new IOException();
            Assert.That(_engine.EnviarDados("a"), Is.False);

            _engine.ExecutarCicloWatchdog();

            Assert.That(_reconexoes, Is.EqualTo(1));
            Assert.That(_engine.EnviarDados("b"), Is.True);
            Assert.That(_portas[1].Escritos, Is.EqualTo(new[] { "b\n" }));
        }

        [Test]
        public void Desconectar_Manual_NaoDeveReconectarNemDispararPerda()
        {
            _engine.Conectar(Porta, 9600);
            _engine.Desconectar();

            _engine.ExecutarCicloWatchdog();
            _engine.ExecutarCicloWatchdog();

            Assert.That(_portas, Has.Count.EqualTo(1));
            Assert.That(_reconexoes, Is.EqualTo(0));
            Assert.That(_perdas, Is.EqualTo(0));
        }

        [Test]
        public void ConectarDuasVezes_DeveDescartarPortaAnterior()
        {
            _engine.Conectar(Porta, 9600);
            _engine.Conectar(Porta, 19200);

            Assert.That(_portas[0].Descartada, Is.True);
            Assert.That(_portas[1].BaudRate, Is.EqualTo(19200));
            Assert.That(_engine.EstaConectado, Is.True);
        }

        [Test]
        public void Conectar_FalhaAoAbrir_DevePropagarENaoFicarConectado()
        {
            using var engine = new SupervisorioSerialEngine(
                (n, b) => new FakeSerialPort(n, b) { FalhaNaAbertura = new UnauthorizedAccessException() },
                () => _listadas,
                intervaloWatchdog: null);

            Assert.That(() => engine.Conectar(Porta, 9600), Throws.Exception);
            Assert.That(engine.EstaConectado, Is.False);
        }
    }
}
