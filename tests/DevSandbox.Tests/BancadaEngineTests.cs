using System;
using DevSandbox.Core.Bancada;
using NUnit.Framework;

namespace DevSandbox.Tests
{
    [TestFixture]
    public class BancadaEngineTests
    {
        private BancadaEngine _engine;

        [SetUp]
        public void Setup()
        {
            _engine = new BancadaEngine();
        }

        [TearDown]
        public void TearDown()
        {
            _engine.Dispose();
        }

        [Test]
        public void EstadoInicial_DeveSerCircuitoAbertoEEstadoA()
        {
            Assert.That(_engine.CaboAtual, Is.EqualTo(EstadoCaboPP.CircuitoAberto));
            Assert.That(_engine.EstadoAtual, Is.EqualTo(EstadoVeiculoCP.EstadoA));
            Assert.That(_engine.EstaConectado, Is.False);
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

        [TestCase(EstadoVeiculoCP.EstadoA, "estado a")]
        [TestCase(EstadoVeiculoCP.EstadoB, "estado b")]
        [TestCase(EstadoVeiculoCP.EstadoC, "estado c")]
        [TestCase(EstadoVeiculoCP.EstadoD, "estado d")]
        [TestCase(EstadoVeiculoCP.EstadoE, "estado e")]
        public void ObterComandoSerial_Estado_DeveFormatarCorretamente(EstadoVeiculoCP estado, string comandoEsperado)
        {
            Assert.That(estado.ObterComandoSerial(), Is.EqualTo(comandoEsperado));
        }

        [Test]
        public void DefinirCabo_DeveAtualizarPropriedadeEDispararEventos()
        {
            EstadoCaboPP? caboNotificado = null;
            string? comandoNotificado = null;

            _engine.CaboAlterado += (s, cabo) => caboNotificado = cabo;
            _engine.ComandoEnviado += (s, cmd) => comandoNotificado = cmd;

            _engine.DefinirCabo(EstadoCaboPP.Cabo32A);

            Assert.That(_engine.CaboAtual, Is.EqualTo(EstadoCaboPP.Cabo32A));
            Assert.That(caboNotificado, Is.EqualTo(EstadoCaboPP.Cabo32A));
            Assert.That(comandoNotificado, Is.EqualTo("cabo 4"));
        }

        [Test]
        public void DefinirEstado_DeveAtualizarPropriedadeEDispararEventos()
        {
            EstadoVeiculoCP? estadoNotificado = null;
            string? comandoNotificado = null;

            _engine.EstadoVeiculoAlterado += (s, estado) => estadoNotificado = estado;
            _engine.ComandoEnviado += (s, cmd) => comandoNotificado = cmd;

            _engine.DefinirEstado(EstadoVeiculoCP.EstadoC);

            Assert.That(_engine.EstadoAtual, Is.EqualTo(EstadoVeiculoCP.EstadoC));
            Assert.That(estadoNotificado, Is.EqualTo(EstadoVeiculoCP.EstadoC));
            Assert.That(comandoNotificado, Is.EqualTo("estado c"));
        }

        [Test]
        public void EnviarComandoManual_NaoDeveDispararEventoSeVazio()
        {
            string? comandoNotificado = null;
            _engine.ComandoEnviado += (s, cmd) => comandoNotificado = cmd;

            _engine.EnviarComandoManual("   ");
            Assert.That(comandoNotificado, Is.Null);

            _engine.EnviarComandoManual("help");
            Assert.That(comandoNotificado, Is.EqualTo("help"));
        }
    }
}

