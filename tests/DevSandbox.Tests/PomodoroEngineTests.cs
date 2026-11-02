using System;
using System.Threading;
using System.Threading.Tasks;
using DevSandbox.Core.Pomodoro;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace DevSandbox.Tests
{
    /// <summary>
    /// Testes do Pomodoro com relógio virtual (<see cref="FakeTimeProvider"/>): nenhum depende do tempo real.
    /// O timeout de 2 s nos awaits só protege contra travamento; em execução normal tudo conclui em milissegundos.
    /// </summary>
    [TestFixture]
    public class PomodoroEngineTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

        private FakeTimeProvider _relogio;
        private PomodoroEngine _engine;

        [SetUp]
        public void Setup()
        {
            _relogio = new FakeTimeProvider();
            _engine = new PomodoroEngine(_relogio);
        }

        [TearDown]
        public void TearDown()
        {
            _engine.Dispose();
        }

        private static void AguardarAte(Func<bool> condicao, string mensagem)
        {
            Assert.That(SpinWait.SpinUntil(condicao, Timeout), Is.True, mensagem);
        }

        [Test]
        public void Iniciar_DeveAtivarEstadoEStatusCorretamente()
        {
            var duracao = TimeSpan.FromMinutes(25);

            _engine.Iniciar(duracao, EstadoPomodoro.Foco);

            Assert.That(_engine.EstaRodando, Is.True);
            Assert.That(_engine.EstadoAtual, Is.EqualTo(EstadoPomodoro.Foco));
            Assert.That(_engine.TempoTotal, Is.EqualTo(duracao));
            Assert.That(_engine.TempoRestante, Is.EqualTo(duracao));
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
            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);

            _engine.Parar();

            Assert.That(_engine.EstaRodando, Is.False);
            Assert.That(_engine.EstaPausado, Is.False);
            Assert.That(_engine.EstadoAtual, Is.EqualTo(EstadoPomodoro.Parado));
            Assert.That(_engine.TempoRestante, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void Pausar_DeveDefinirEstadoPausadoEInterromperContagem()
        {
            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);

            bool pausou = _engine.Pausar();

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
        public async Task Timer_DeveDispararOnEstadoMudou_AoFinalizarTempo()
        {
            var tcs = new TaskCompletionSource<EstadoPomodoro>(TaskCreationOptions.RunContinuationsAsynchronously);
            _engine.OnEstadoMudou += estado => tcs.TrySetResult(estado);

            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);
            _relogio.Advance(TimeSpan.FromMinutes(25));

            Assert.That(await tcs.Task.WaitAsync(Timeout), Is.EqualTo(EstadoPomodoro.Foco));
            Assert.That(_engine.EstaRodando, Is.False);
            Assert.That(_engine.TempoRestante, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public async Task Timer_DeveNotificarOnTickComZero_AntesDeMudarEstado()
        {
            var ordem = new List<string>();
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _engine.OnTick += restante =>
            {
                if (restante == TimeSpan.Zero) lock (ordem) ordem.Add("tick0");
            };
            _engine.OnEstadoMudou += estado =>
            {
                lock (ordem) ordem.Add("estado");
                tcs.TrySetResult();
            };

            _engine.Iniciar(TimeSpan.FromMinutes(5), EstadoPomodoro.DescansoCurto);
            _relogio.Advance(TimeSpan.FromMinutes(5));

            await tcs.Task.WaitAsync(Timeout);
            Assert.That(ordem, Is.EqualTo(new[] { "tick0", "estado" }));
        }

        [Test]
        public void Timer_NaoDeveFinalizarAntesDoTempo()
        {
            int mudancas = 0;
            _engine.OnEstadoMudou += _ => Interlocked.Increment(ref mudancas);

            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);
            _relogio.Advance(TimeSpan.FromMinutes(10));
            AguardarAte(() => _engine.TempoRestante <= TimeSpan.FromMinutes(15), "O tick não refletiu o avanço do relógio.");

            Assert.That(_engine.TempoRestante, Is.EqualTo(TimeSpan.FromMinutes(15)));
            Assert.That(_engine.EstaRodando, Is.True);
            Assert.That(mudancas, Is.EqualTo(0));
        }

        [Test]
        public async Task Retomar_AposPausa_DeveContinuarDoTempoRestanteEResolverCiclo()
        {
            var tcs = new TaskCompletionSource<EstadoPomodoro>(TaskCreationOptions.RunContinuationsAsynchronously);
            _engine.OnEstadoMudou += estado =>
            {
                if (estado != EstadoPomodoro.Pausado) tcs.TrySetResult(estado);
            };

            _engine.Iniciar(TimeSpan.FromMinutes(10), EstadoPomodoro.DescansoCurto);
            _relogio.Advance(TimeSpan.FromMinutes(4));
            AguardarAte(() => _engine.TempoRestante <= TimeSpan.FromMinutes(6), "O tick não refletiu o avanço do relógio.");

            Assert.That(_engine.Pausar(), Is.True);

            // Tempo passando durante a pausa não consome o ciclo
            _relogio.Advance(TimeSpan.FromHours(1));
            Assert.That(_engine.EstaPausado, Is.True);
            Assert.That(_engine.TempoRestante, Is.EqualTo(TimeSpan.FromMinutes(6)));

            Assert.That(_engine.Retomar(), Is.True);
            Assert.That(_engine.EstadoAtual, Is.EqualTo(EstadoPomodoro.DescansoCurto));

            _relogio.Advance(TimeSpan.FromMinutes(5));
            AguardarAte(() => _engine.TempoRestante <= TimeSpan.FromMinutes(1), "O tick não refletiu o avanço do relógio.");
            Assert.That(tcs.Task.IsCompleted, Is.False, "Concluiu antes de esgotar o tempo restante.");

            _relogio.Advance(TimeSpan.FromMinutes(1));
            await GarantirConclusaoAsync(tcs.Task);
            Assert.That(await tcs.Task.WaitAsync(Timeout), Is.EqualTo(EstadoPomodoro.DescansoCurto));
            Assert.That(_engine.EstaRodando, Is.False);
        }

        /// <summary>
        /// Após o primeiro ciclo, o próximo <c>Task.Delay</c> pode ser registrado depois do <c>Advance</c>
        /// (a continuação roda no pool). Avança o relógio virtual um tick por vez, cedendo a CPU, até concluir
        /// ou estourar o timeout de segurança. Não usa <c>Task.Delay</c> de tempo real.
        /// </summary>
        private Task GarantirConclusaoAsync(Task tarefa)
        {
            SpinWait.SpinUntil(() =>
            {
                if (tarefa.IsCompleted) return true;
                _relogio.Advance(TimeSpan.FromMilliseconds(50));
                Thread.Yield();
                return false;
            }, Timeout);
            return Task.CompletedTask;
        }

        [Test]
        public void Iniciar_DuranteCicloAtivo_CicloAnteriorNaoDeveFinalizarONovo()
        {
            _engine.Iniciar(TimeSpan.FromMinutes(1), EstadoPomodoro.Foco);
            _engine.Iniciar(TimeSpan.FromMinutes(10), EstadoPomodoro.DescansoLongo);

            _relogio.Advance(TimeSpan.FromMinutes(1));
            // Sincroniza pelo tick do ciclo NOVO (relógio virtual), sem espera em tempo real
            AguardarAte(() => _engine.TempoRestante <= TimeSpan.FromMinutes(9), "O ciclo novo não refletiu o avanço do relógio.");

            Assert.That(_engine.EstaRodando, Is.True);
            Assert.That(_engine.EstadoAtual, Is.EqualTo(EstadoPomodoro.DescansoLongo));
        }

        [Test]
        public async Task Assinante_QueLancaExcecao_NaoDeveInterromperCronometroNemDemaisAssinantes()
        {
            int ticksDoSegundoAssinante = 0;
            var tcs = new TaskCompletionSource<EstadoPomodoro>(TaskCreationOptions.RunContinuationsAsynchronously);

            _engine.OnTick += _ => throw new InvalidOperationException("falha do assinante");
            _engine.OnTick += _ => Interlocked.Increment(ref ticksDoSegundoAssinante);
            _engine.OnEstadoMudou += _ => throw new InvalidOperationException("falha do assinante");
            _engine.OnEstadoMudou += estado => tcs.TrySetResult(estado);

            Assert.DoesNotThrow(() => _engine.Iniciar(TimeSpan.FromMinutes(1), EstadoPomodoro.Foco));
            _relogio.Advance(TimeSpan.FromMinutes(1));

            Assert.That(await tcs.Task.WaitAsync(Timeout), Is.EqualTo(EstadoPomodoro.Foco));
            Assert.That(ticksDoSegundoAssinante, Is.GreaterThanOrEqualTo(2));
            Assert.That(_engine.EstaRodando, Is.False);
        }

        [Test]
        public void Dispose_DevePararCicloEImpedirNovoInicio()
        {
            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);

            _engine.Dispose();

            Assert.That(_engine.EstaRodando, Is.False);
            Assert.Throws<ObjectDisposedException>(() => _engine.Iniciar(TimeSpan.FromMinutes(1)));
            Assert.That(_engine.Retomar(), Is.False);
            Assert.DoesNotThrow(() => _engine.Dispose());
        }

        [Test]
        public void Construtor_SemRelogio_DeveUsarRelogioDoSistema()
        {
            using var engine = new PomodoroEngine();
            engine.Iniciar(TimeSpan.FromMinutes(1));
            Assert.That(engine.EstaRodando, Is.True);
        }
    }
}
