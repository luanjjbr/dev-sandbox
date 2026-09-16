using System;
using System.Threading.Tasks;
using DevSandbox.Core.Pomodoro;
using NUnit.Framework;

namespace DevSandbox.Tests
{
    [TestFixture]
    public class PomodoroEngineTests
    {
        private PomodoroEngine _engine;

        [SetUp]
        public void Setup()
        {
            _engine = new PomodoroEngine();
        }

        [TearDown]
        public void TearDown()
        {
            _engine.Parar();
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
            Assert.That(_engine.TempoTotal, Is.EqualTo(duracao));
            Assert.That(_engine.EstaPausado, Is.False);
        }

        [Test]
        public void Iniciar_ComDuracaoInvalida_DeveLancarArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _engine.Iniciar(TimeSpan.Zero));
            Assert.Throws<ArgumentException>(() => _engine.Iniciar(TimeSpan.FromSeconds(-5)));
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
            Assert.That(_engine.EstaPausado, Is.False);
            Assert.That(_engine.EstadoAtual, Is.EqualTo(EstadoPomodoro.Parado));
            Assert.That(_engine.TempoRestante, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void Pausar_DeveDefinirEstadoPausadoEInterromperContagem()
        {
            // Arrange
            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);

            // Act
            bool pausou = _engine.Pausar();

            // Assert
            Assert.That(pausou, Is.True);
            Assert.That(_engine.EstaPausado, Is.True);
            Assert.That(_engine.EstadoAtual, Is.EqualTo(EstadoPomodoro.Pausado));
            Assert.That(_engine.EstadoAnterior, Is.EqualTo(EstadoPomodoro.Foco));
            Assert.That(_engine.TempoRestante, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public void Pausar_QuandoJaParadoOuPausado_DeveRetornarFalse()
        {
            Assert.That(_engine.Pausar(), Is.False);

            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);
            _engine.Pausar();
            Assert.That(_engine.Pausar(), Is.False);
        }

        [Test]
        public async Task Retomar_AposPausa_DeveContinuarContagemEResolverCiclo()
        {
            // Arrange
            var duracao = TimeSpan.FromMilliseconds(150);
            var tcs = new TaskCompletionSource<EstadoPomodoro>();
            _engine.OnEstadoMudou += (estado) =>
            {
                if (estado != EstadoPomodoro.Pausado)
                {
                    tcs.TrySetResult(estado);
                }
            };

            // Act
            _engine.Iniciar(duracao, EstadoPomodoro.DescansoCurto);
            await Task.Delay(30);
            _engine.Pausar();
            Assert.That(_engine.EstaPausado, Is.True);

            bool retomou = _engine.Retomar();
            Assert.That(retomou, Is.True);
            Assert.That(_engine.EstadoAtual, Is.EqualTo(EstadoPomodoro.DescansoCurto));

            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(2000));

            // Assert
            Assert.That(completedTask, Is.EqualTo(tcs.Task), "O ciclo não concluiu após retomar.");
            Assert.That(await tcs.Task, Is.EqualTo(EstadoPomodoro.DescansoCurto));
            Assert.That(_engine.EstaRodando, Is.False);
        }

        [Test]
        public async Task Timer_DeveDispararEventoOnEstadoMudou_AoFinalizarTempo()
        {
            // Arrange
            var duracaoCurta = TimeSpan.FromMilliseconds(100);
            var tcs = new TaskCompletionSource<EstadoPomodoro>();

            _engine.OnEstadoMudou += (estado) =>
            {
                tcs.TrySetResult(estado);
            };

            // Act
            _engine.Iniciar(duracaoCurta, EstadoPomodoro.Foco);
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(2000));

            // Assert
            Assert.That(completedTask, Is.EqualTo(tcs.Task), "O evento OnEstadoMudou não disparou no tempo esperado.");
            Assert.That(await tcs.Task, Is.EqualTo(EstadoPomodoro.Foco));
            Assert.That(_engine.EstaRodando, Is.False);
        }

        [Test]
        public async Task Timer_DeveNotificarOnTickComZero_AoEncerrar()
        {
            // Arrange
            var duracaoCurta = TimeSpan.FromMilliseconds(80);
            var tcs = new TaskCompletionSource<TimeSpan>();

            _engine.OnTick += (restante) =>
            {
                if (restante == TimeSpan.Zero)
                {
                    tcs.TrySetResult(restante);
                }
            };

            // Act
            _engine.Iniciar(duracaoCurta, EstadoPomodoro.DescansoCurto);
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(2000));

            // Assert
            Assert.That(completedTask, Is.EqualTo(tcs.Task), "O evento OnTick com TimeSpan.Zero não foi disparado.");
        }
    }
}
