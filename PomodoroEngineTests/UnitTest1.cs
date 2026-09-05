using System;
using System.Threading.Tasks;
using dev_sandbox.Core;
using NUnit.Framework;

namespace PomodoroEngineTests
{
    [TestFixture]
    public class Tests
    {
        private PomodoroEngine _engine;

        [SetUp]
        public void Setup()
        {
            _engine = new PomodoroEngine();
        }

        [Test]
        public void Iniciar_DeveAtivarEstadoEStatusCorretamente()
        {
            // Arrange
            var duracao = TimeSpan.FromMinutes(25);

            // Act
            _engine.Iniciar(duracao, EstadoPomodoro.Foco);

            // Assert
            Assert.That(_engine.EstaRodando, Is.True);
            Assert.That(_engine.EstadoAtual, Is.EqualTo(EstadoPomodoro.Foco));
        }

        [Test]
        public void Parar_DeveResetarEstadoEStatus()
        {
            // Arrange
            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);

            // Act
            _engine.Parar();

            // Assert
            Assert.That(_engine.EstaRodando, Is.False);
            Assert.That(_engine.EstadoAtual, Is.EqualTo(EstadoPomodoro.Parado));
        }

        [Test]
        public async Task Timer_DeveDispararEventoOnEstadoMudou_AoFinalizarTempo()
        {
            // Arrange
            var duracaoCurta = TimeSpan.FromMilliseconds(100);
            EstadoPomodoro? estadoNotificado = null;

            _engine.OnEstadoMudou += (estado) =>
            {
                estadoNotificado = estado;
            };

            // Act
            _engine.Iniciar(duracaoCurta, EstadoPomodoro.Foco);
            await Task.Delay(300); // Aguarda o encerramento em segundo plano

            // Assert
            Assert.That(estadoNotificado, Is.EqualTo(EstadoPomodoro.Foco));
            Assert.That(_engine.EstaRodando, Is.False);
        }
    }
}