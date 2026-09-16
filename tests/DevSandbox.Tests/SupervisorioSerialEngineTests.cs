using System;
using DevSandbox.Core.Serial;
using NUnit.Framework;

namespace DevSandbox.Tests
{
    [TestFixture]
    public class SupervisorioSerialEngineTests
    {
        private SupervisorioSerialEngine _engine;

        [SetUp]
        public void Setup()
        {
            _engine = new SupervisorioSerialEngine();
        }

        [TearDown]
        public void TearDown()
        {
            _engine.Dispose();
        }

        [Test]
        public void EstadoInicial_DeveEstarDesconectadoENaoReconectando()
        {
            Assert.That(_engine.EstaConectado, Is.False);
            Assert.That(_engine.TentandoReconectar, Is.False);
        }

        [Test]
        public void ObterPortasDisponiveis_NaoDeveRetornarNulo()
        {
            var portas = _engine.ObterPortasDisponiveis();
            Assert.That(portas, Is.Not.Null);
        }

        [Test]
        public void Conectar_DeveLancarArgumentException_QuandoPortaVazia()
        {
            Assert.Throws<ArgumentException>(() => _engine.Conectar("", 9600));
            Assert.Throws<ArgumentException>(() => _engine.Conectar("   ", 9600));
        }

        [Test]
        public void Conectar_DeveLancarArgumentOutOfRangeException_QuandoBaudRateInvalido()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _engine.Conectar("COM1", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _engine.Conectar("COM1", -9600));
        }

        [Test]
        public void Desconectar_DevePoderSerChamadoSemConexaoAtiva()
        {
            Assert.DoesNotThrow(() => _engine.Desconectar());
            Assert.That(_engine.EstaConectado, Is.False);
        }

        [Test]
        public void EnviarDados_SemConexaoOuVazio_NaoDeveLancarExcecao()
        {
            Assert.DoesNotThrow(() => _engine.EnviarDados("TESTE"));
            Assert.DoesNotThrow(() => _engine.EnviarDados(""));
        }

        [Test]
        public void Dispose_DevePoderSerChamadoMultiplasVezesComSeguranca()
        {
            Assert.DoesNotThrow(() =>
            {
                _engine.Dispose();
                _engine.Dispose();
            });
        }
    }
}
