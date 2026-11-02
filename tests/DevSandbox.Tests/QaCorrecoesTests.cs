using System;
using System.IO;
using System.Threading;
using DevSandbox.Core.Converters;
using DevSandbox.Core.Pomodoro;
using DevSandbox.Core.Serial;
using DevSandbox.Tests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace DevSandbox.Tests
{
    /// <summary>Testes das correções da revisão (T1–T7 serial/Pomodoro).</summary>
    [TestFixture]
    public class QaCorrecoesSerialTests
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
        public void T1a_FalhaDeLeitura_SoSinalizaEWatchdogTrataAPerdaEReabre()
        {
            _engine.Conectar(Porta, 9600);
            FakeSerialPort antiga = _portas[0];
            antiga.FalhaNaLeitura = new IOException();

            antiga.SimularRecepcao("x");

            Assert.That(_perdas, Is.EqualTo(0));
            Assert.That(_engine.FalhaDeLeituraPendente, Is.True);

            _engine.ExecutarCicloWatchdog();

            Assert.That(_perdas, Is.EqualTo(1));
            Assert.That(_engine.FalhaDeLeituraPendente, Is.False);
            Assert.That(antiga.Descartada, Is.True);
            Assert.That(_reconexoes, Is.EqualTo(1));
            Assert.That(_engine.EstaConectado, Is.True);
            Assert.That(_portas, Has.Count.EqualTo(2));
        }

        [Test]
        public void T1b_DadosDePortaDerrubadaPeloWatchdog_NaoDevemSerRepassados()
        {
            int recebidos = 0;
            _engine.DadosRecebidos += (s, d) => recebidos++;
            _engine.Conectar(Porta, 9600);
            FakeSerialPort antiga = _portas[0];

            _listadas = [];
            _engine.ExecutarCicloWatchdog();
            Assert.That(_perdas, Is.EqualTo(1));

            antiga.SimularRecepcao("lixo");

            Assert.That(recebidos, Is.EqualTo(0));
        }

        [Test]
        public void T1c_FalhaDeLeituraSeguidaDeDesconectar_WatchdogNaoDisparaPerda()
        {
            _engine.Conectar(Porta, 9600);
            _portas[0].FalhaNaLeitura = new IOException();
            _portas[0].SimularRecepcao("x");

            _engine.Desconectar();
            _engine.ExecutarCicloWatchdog();

            Assert.That(_perdas, Is.EqualTo(0));
            Assert.That(_engine.FalhaDeLeituraPendente, Is.False);
            Assert.That(_engine.TentandoReconectar, Is.False);
        }

        [Test]
        public void T4_ConectarAposDispose_LancaObjectDisposedESemCriarPorta()
        {
            _engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => _engine.Conectar(Porta, 9600));
            Assert.That(_portas, Is.Empty);
        }

        [Test]
        public void T5a_HandlerDePerdaQueDesconectaEConecta_NaoDeadlockaNemLanca()
        {
            _engine.Conectar(Porta, 9600);
            _engine.ConexaoPerdida += (s, e) =>
            {
                _engine.Desconectar();
                _engine.Conectar(Porta, 9600);
            };
            _portas[0].FalhaNaEscrita = new IOException();

            bool concluiu = false;
            var t = new Thread(() =>
            {
                Assert.DoesNotThrow(() => _engine.EnviarDados("a"));
                concluiu = true;
            });
            t.Start();

            Assert.That(t.Join(TimeSpan.FromSeconds(5)), Is.True, "Deadlock no handler de ConexaoPerdida.");
            Assert.That(concluiu, Is.True);
            Assert.That(_engine.EstaConectado, Is.True);
            Assert.That(_engine.TentandoReconectar, Is.False);
        }

        [Test]
        public void T6_FalhaDeEscritaComObjectDisposed_RetornaFalseEDisparaPerda()
        {
            _engine.Conectar(Porta, 9600);
            _portas[0].FalhaNaEscrita = new ObjectDisposedException("x");

            bool enviado = true;
            Assert.DoesNotThrow(() => enviado = _engine.EnviarDados("a"));

            Assert.That(enviado, Is.False);
            Assert.That(_perdas, Is.EqualTo(1));
        }

        [Test]
        public void T6b_SistemaSerialPort_TimeoutPadraoDe1000ms()
        {
            Assert.That(SistemaSerialPort.TimeoutMs, Is.EqualTo(1000));
        }

        [Test]
        public void T7_ConectarComFalhaAoAbrir_PropagaEWatchdogNaoFazNada()
        {
            Exception? falha = null;
            var criadas = new List<FakeSerialPort>();
            using var engine = new SupervisorioSerialEngine(
                (n, b) => { var p = new FakeSerialPort(n, b) { FalhaNaAbertura = falha }; criadas.Add(p); return p; },
                () => _listadas,
                intervaloWatchdog: null);
            int perdas = 0, reconexoes = 0;
            engine.ConexaoPerdida += (s, e) => perdas++;
            engine.Reconectado += (s, e) => reconexoes++;

            engine.Conectar(Porta, 9600);
            falha = new UnauthorizedAccessException();
            Assert.Throws<UnauthorizedAccessException>(() => engine.Conectar(Porta, 9600));
            int portasAntes = criadas.Count;

            falha = null;
            _listadas = [];
            Assert.DoesNotThrow(() => engine.ExecutarCicloWatchdog());
            _listadas = [Porta];
            Assert.DoesNotThrow(() => engine.ExecutarCicloWatchdog());

            Assert.That(perdas, Is.EqualTo(0));
            Assert.That(reconexoes, Is.EqualTo(0));
            Assert.That(criadas, Has.Count.EqualTo(portasAntes));
            Assert.That(engine.EstaConectado, Is.False);
        }
    }

    [TestFixture]
    public class QaCorrecoesPomodoroTests
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
        public void TearDown() => _engine.Dispose();

        private static void AguardarAte(Func<bool> condicao, string mensagem) =>
            Assert.That(SpinWait.SpinUntil(condicao, Timeout), Is.True, mensagem);

        // Avança o relógio virtual, um tick por vez, até a condição ou o timeout de segurança.
        private void AvancarAte(Func<bool> condicao, string mensagem) =>
            Assert.That(SpinWait.SpinUntil(() =>
            {
                if (condicao()) return true;
                _relogio.Advance(TimeSpan.FromMinutes(1)); // salto grande: cada Delay registrado dispara de uma vez
                Thread.Yield();
                return false;
            }, Timeout), Is.True, mensagem);

        [Test]
        public void T2a_OrdemTick0DepoisEstado_Mantida()
        {
            var ordem = new List<string>();
            _engine.OnTick += r => { if (r == TimeSpan.Zero) lock (ordem) ordem.Add("tick0"); };
            _engine.OnEstadoMudou += e => { lock (ordem) ordem.Add("estado"); };

            _engine.Iniciar(TimeSpan.FromMinutes(1), EstadoPomodoro.Foco);
            AvancarAte(() => !_engine.EstaRodando, "Ciclo não concluiu.");
            AguardarAte(() => { lock (ordem) return ordem.Count >= 2; }, "Evento de estado não chegou.");

            lock (ordem) Assert.That(ordem, Is.EqualTo(new[] { "tick0", "estado" }));
        }

        [Test]
        public void T2b_NoTickZero_EstadoJaEParadoETempoRestanteZero()
        {
            EstadoPomodoro? estado = null;
            TimeSpan? restante = null;
            _engine.OnTick += r =>
            {
                if (r == TimeSpan.Zero)
                {
                    estado = _engine.EstadoAtual;
                    restante = _engine.TempoRestante;
                }
            };

            _engine.Iniciar(TimeSpan.FromMinutes(1), EstadoPomodoro.Foco);
            AvancarAte(() => estado != null, "OnTick(0) não foi notificado.");

            Assert.That(estado, Is.EqualTo(EstadoPomodoro.Parado));
            Assert.That(restante, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void T2c_PausarDentroDoOnTickPerto_DoFim_NuncaTerminaPausadoComZero()
        {
            int pausas = 0;
            _engine.OnTick += r =>
            {
                if (r > TimeSpan.Zero && r <= TimeSpan.FromSeconds(30) && Interlocked.Increment(ref pausas) == 1)
                    _engine.Pausar();
            };

            _engine.Iniciar(TimeSpan.FromMinutes(2), EstadoPomodoro.Foco);
            _relogio.Advance(TimeSpan.FromSeconds(105));
            AguardarAte(() => _engine.EstaPausado, "Não pausou.");

            Assert.That(_engine.TempoRestante, Is.GreaterThan(TimeSpan.Zero));
            // Tempo passando em pausa não consome o ciclo
            _relogio.Advance(TimeSpan.FromMinutes(10));
            Assert.That(_engine.EstaPausado, Is.True);

            Assert.That(_engine.Retomar(), Is.True);
            AvancarAte(() => !_engine.EstaRodando, "Não concluiu após retomar.");
            Assert.That(_engine.EstadoAtual, Is.EqualTo(EstadoPomodoro.Parado));
            Assert.That(_engine.TempoRestante, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void T2c_PausarDentroDoTickZero_RetornaFalseEFicaParado()
        {
            bool? pausou = null;
            _engine.OnTick += r => { if (r == TimeSpan.Zero) pausou = _engine.Pausar(); };

            _engine.Iniciar(TimeSpan.FromMinutes(1), EstadoPomodoro.Foco);
            AvancarAte(() => pausou != null, "OnTick(0) não foi notificado.");

            Assert.That(pausou, Is.False);
            Assert.That(_engine.EstaPausado, Is.False);
            Assert.That(_engine.EstadoAtual, Is.EqualTo(EstadoPomodoro.Parado));
        }

        [Test]
        public void T3a_SegundoIniciar_CicloAntigoNaoAfetaOStatusNovo()
        {
            _engine.Iniciar(TimeSpan.FromMinutes(1), EstadoPomodoro.Foco);
            _engine.Iniciar(TimeSpan.FromMinutes(10), EstadoPomodoro.DescansoLongo);

            _relogio.Advance(TimeSpan.FromMinutes(1));
            AguardarAte(() => _engine.TempoRestante <= TimeSpan.FromMinutes(9), "O ciclo novo não refletiu o avanço.");

            Assert.That(_engine.EstadoAtual, Is.EqualTo(EstadoPomodoro.DescansoLongo));
            Assert.That(_engine.TempoRestante, Is.EqualTo(TimeSpan.FromMinutes(9)));
        }

        [Test]
        public void T3b_DoisIniciarSeguidos_NenhumOnEstadoMudouDoCicloAntigo()
        {
            var eventos = new List<EstadoPomodoro>();
            _engine.OnEstadoMudou += e => { lock (eventos) eventos.Add(e); };

            _engine.Iniciar(TimeSpan.FromMinutes(1), EstadoPomodoro.Foco);
            _engine.Iniciar(TimeSpan.FromMinutes(10), EstadoPomodoro.DescansoLongo);
            _relogio.Advance(TimeSpan.FromMinutes(1));
            AguardarAte(() => _engine.TempoRestante <= TimeSpan.FromMinutes(9), "O ciclo novo não refletiu o avanço.");

            lock (eventos) Assert.That(eventos, Is.Empty);
        }
    }

    [TestFixture]
    public class QaCorrecoesCalculadoraTests
    {
        private CalculadoraHex _calc;

        [SetUp]
        public void Setup() => _calc = new CalculadoraHex();

        [TestCase("0BAD")]
        [TestCase("0B12")]
        public void T8_PrefixoBComDigitosHex_EhHexadecimal(string entrada)
        {
            Assert.That(_calc.DetectarBase(entrada), Is.EqualTo(BaseNumerica.Hexadecimal));
        }

        [Test]
        public void T8_ConverterEntrada_0B12_EhHex()
        {
            Assert.That(_calc.ConverterEntrada("0B12", out BaseNumerica b), Is.EqualTo(0x0B12L));
            Assert.That(b, Is.EqualTo(BaseNumerica.Hexadecimal));
        }

        [TestCase("0b1010")]
        [TestCase("0b 1100_0011")]
        [TestCase("0b")]
        public void T8_PrefixoBinario_EhBinario(string entrada)
        {
            Assert.That(_calc.DetectarBase(entrada), Is.EqualTo(BaseNumerica.Binario));
        }

        [Test]
        public void T8_ConverterEntrada_SoPrefixoBinario_LancaArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _calc.ConverterEntrada("0b", out _));
        }

        [Test]
        public void T9a_ZerosAEsquerdaNaoContamParaOLimiteDe64Bits()
        {
            Assert.That(_calc.BinarioParaDecimal(new string('0', 10) + new string('1', 64)), Is.EqualTo(-1L));
            Assert.That(_calc.BinarioParaDecimal("0" + new string('1', 64)), Is.EqualTo(-1L));
            Assert.Throws<OverflowException>(() => _calc.BinarioParaDecimal(new string('1', 65)));
        }

        [Test]
        public void T9b_TabulacaoEhSeparador()
        {
            Assert.That(_calc.HexParaDecimal("DE\tAD"), Is.EqualTo(0xDEADL));
            Assert.That(_calc.BinarioParaDecimal("1010\t0101"), Is.EqualTo(0xA5L));
        }
    }
}
