using System;
using DevSandbox.Core.Bancada;
using DevSandbox.Tests.Fakes;
using NUnit.Framework;

namespace DevSandbox.Tests
{
    [TestFixture]
    public class BancadaEngineTests
    {
        private FakeSerialEngine _serial;
        private BancadaEngine _engine;

        [SetUp]
        public void Setup()
        {
            _serial = new FakeSerialEngine { EstaConectado = true };
            _engine = new BancadaEngine(_serial);
        }

        [TearDown]
        public void TearDown()
        {
            _engine.Dispose();
            _serial.Dispose(); // idempotente; o BancadaEngine já o descartou
        }

        [Test]
        public void EstadoInicial_DeveSerCircuitoAbertoEEstadoAX()
        {
            using var engine = new BancadaEngine();

            Assert.That(engine.CaboAtual, Is.EqualTo(EstadoCaboPP.CircuitoAberto));
            Assert.That(engine.EstadoAtual, Is.EqualTo(EstadoVeiculoCP.AX));
            Assert.That(engine.EstaConectado, Is.False);
        }

        [TestCase(EstadoCaboPP.CircuitoAberto, "cabo 0")]
        [TestCase(EstadoCaboPP.Desconectado, "cabo 1")]
        [TestCase(EstadoCaboPP.Cabo13A, "cabo 2")]
        [TestCase(EstadoCaboPP.Cabo20A, "cabo 3")]
        [TestCase(EstadoCaboPP.Cabo32A, "cabo 4")]
        [TestCase(EstadoCaboPP.Cabo63A, "cabo 5")]
        [TestCase(EstadoCaboPP.FalhaCurto, "cabo 6")]
        public void ObterComandoSerial_Cabo_DeveFormatarCorretamente(EstadoCaboPP cabo, string comandoEsperado)
        {
            Assert.That(cabo.ObterComandoSerial(), Is.EqualTo(comandoEsperado));
        }

        [TestCase(EstadoVeiculoCP.AX, "estado ax")]
        [TestCase(EstadoVeiculoCP.BXNominal, "estado bx")]
        [TestCase(EstadoVeiculoCP.CXNominal, "estado cx")]
        [TestCase(EstadoVeiculoCP.DXNominal, "estado dx")]
        [TestCase(EstadoVeiculoCP.E, "estado e")]
        public void ObterComandoSerial_Estado_DeveFormatarCorretamente(EstadoVeiculoCP estado, string comandoEsperado)
        {
            Assert.That(estado.ObterComandoSerial(), Is.EqualTo(comandoEsperado));
        }

        [Test]
        public void DefinirCabo_DeveAtualizarPropriedadeTransmitirEDispararEventos()
        {
            EstadoCaboPP? caboNotificado = null;
            string? comandoNotificado = null;

            _engine.CaboAlterado += (s, cabo) => caboNotificado = cabo;
            _engine.ComandoEnviado += (s, cmd) => comandoNotificado = cmd;

            bool enviado = _engine.DefinirCabo(EstadoCaboPP.Cabo32A);

            Assert.That(enviado, Is.True);
            Assert.That(_engine.CaboAtual, Is.EqualTo(EstadoCaboPP.Cabo32A));
            Assert.That(caboNotificado, Is.EqualTo(EstadoCaboPP.Cabo32A));
            Assert.That(comandoNotificado, Is.EqualTo("cabo 4"));
            Assert.That(_serial.ComandosTransmitidos, Is.EqualTo(new[] { "cabo 4" }));
        }

        [Test]
        public void DefinirEstado_DeveAtualizarPropriedadeTransmitirEDispararEventos()
        {
            EstadoVeiculoCP? estadoNotificado = null;
            string? comandoNotificado = null;

            _engine.EstadoVeiculoAlterado += (s, estado) => estadoNotificado = estado;
            _engine.ComandoEnviado += (s, cmd) => comandoNotificado = cmd;

            bool enviado = _engine.DefinirEstado(EstadoVeiculoCP.CXNominal);

            Assert.That(enviado, Is.True);
            Assert.That(_engine.EstadoAtual, Is.EqualTo(EstadoVeiculoCP.CXNominal));
            Assert.That(estadoNotificado, Is.EqualTo(EstadoVeiculoCP.CXNominal));
            Assert.That(comandoNotificado, Is.EqualTo("estado cx"));
        }

        [Test]
        public void EnviarComandoManual_NaoDeveDispararEventoSeVazio()
        {
            string? comandoNotificado = null;
            _engine.ComandoEnviado += (s, cmd) => comandoNotificado = cmd;

            Assert.That(_engine.EnviarComandoManual("   ", Core.Serial.FimDeLinha.Nenhum), Is.False);
            Assert.That(comandoNotificado, Is.Null);

            Assert.That(_engine.EnviarComandoManual("  help "), Is.True);
            Assert.That(comandoNotificado, Is.EqualTo("help"));
        }

        [Test]
        public void ComandoEnviado_NaoDeveDispararSemConexao()
        {
            _serial.EstaConectado = false;
            int comandos = 0;
            EstadoCaboPP? caboNotificado = null;
            _engine.ComandoEnviado += (s, cmd) => comandos++;
            _engine.CaboAlterado += (s, cabo) => caboNotificado = cabo;

            bool enviado = _engine.DefinirCabo(EstadoCaboPP.Cabo20A);

            Assert.That(enviado, Is.False);
            Assert.That(comandos, Is.EqualTo(0));
            Assert.That(_serial.ComandosTransmitidos, Is.Empty);
            // A seleção local continua sendo registrada (o reenvio é decisão da UI)
            Assert.That(_engine.CaboAtual, Is.EqualTo(EstadoCaboPP.Cabo20A));
            Assert.That(caboNotificado, Is.EqualTo(EstadoCaboPP.Cabo20A));
        }

        [Test]
        public void ComandoEnviado_NaoDeveDispararQuandoTransmissaoFalha()
        {
            _serial.FalharEnvio = true;
            int comandos = 0;
            _engine.ComandoEnviado += (s, cmd) => comandos++;

            Assert.That(_engine.EnviarComandoManual("help"), Is.False);
            Assert.That(_engine.DefinirEstado(EstadoVeiculoCP.BXNominal), Is.False);
            Assert.That(comandos, Is.EqualTo(0));
        }

        [Test]
        public void EventosDoSerial_DevemSerRepassados()
        {
            string? dados = null;
            int perdas = 0, reconexoes = 0;
            _engine.DadosRecebidos += (s, d) => dados = d;
            _engine.ConexaoPerdida += (s, e) => perdas++;
            _engine.Reconectado += (s, e) => reconexoes++;

            _serial.SimularRecepcao("OK\r\n");
            _serial.SimularDesconexao();
            Assert.That(_engine.TentandoReconectar, Is.True);
            _serial.SimularReconexao();

            Assert.That(dados, Is.EqualTo("OK\r\n"));
            Assert.That(perdas, Is.EqualTo(1));
            Assert.That(reconexoes, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_DeveDesassinarHandlersEDescartarSerial()
        {
            Assert.That(_serial.TotalAssinantes, Is.EqualTo(3));

            _engine.Dispose();

            Assert.That(_serial.TotalAssinantes, Is.EqualTo(0));
            Assert.That(_serial.Descartado, Is.True);
            Assert.That(_engine.DefinirCabo(EstadoCaboPP.Cabo13A), Is.False);
            Assert.DoesNotThrow(() => _engine.Dispose());
        }

        [Test]
        public void Dispose_ComDescartarSerialFalse_NaoDeveDescartarSerial()
        {
            var serial = new FakeSerialEngine();
            var engine = new BancadaEngine(serial, descartarSerial: false);

            engine.Dispose();

            Assert.That(serial.Descartado, Is.False);
            Assert.That(serial.TotalAssinantes, Is.EqualTo(0));
        }
    }
}
