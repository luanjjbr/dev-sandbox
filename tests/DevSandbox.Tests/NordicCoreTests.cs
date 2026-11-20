using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevSandbox.Core.Bancada;
using DevSandbox.Core.Converters;
using DevSandbox.Core.Pomodoro;
using DevSandbox.Core.Serial;
using DevSandbox.Tests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace DevSandbox.Tests
{
    // Testes do Core do visual Nordic: fim de linha, operações de bits da Calculadora, progresso e
    // estatísticas do Pomodoro.

    [TestFixture]
    public class FimDeLinhaTests
    {
        [TestCase(FimDeLinha.Nenhum, "", "—")]
        [TestCase(FimDeLinha.CR, "\r", "CR")]
        [TestCase(FimDeLinha.LF, "\n", "LF")]
        [TestCase(FimDeLinha.CRLF, "\r\n", "CRLF")]
        public void TerminadorERotulo_DevemCorresponderAoValor(FimDeLinha fim, string terminador, string rotulo)
        {
            Assert.That(fim.ObterTerminador(), Is.EqualTo(terminador));
            Assert.That(fim.ObterRotulo(), Is.EqualTo(rotulo));
        }

        [Test]
        public void ValorForaDoEnum_DeveLancarArgumentOutOfRange()
        {
            var invalido = (FimDeLinha)99;
            Assert.Throws<ArgumentOutOfRangeException>(() => invalido.ObterTerminador());
            Assert.Throws<ArgumentOutOfRangeException>(() => invalido.ObterRotulo());
        }
    }

    [TestFixture]
    public class SupervisorioFimDeLinhaTests
    {
        private const string Porta = "COM7";
        private List<FakeSerialPort> _portas;
        private SupervisorioSerialEngine _engine;

        private FakeSerialPort PortaAtual => _portas[^1];

        [SetUp]
        public void Setup()
        {
            _portas = [];
            _engine = new SupervisorioSerialEngine(
                (nome, baud) =>
                {
                    var porta = new FakeSerialPort(nome, baud);
                    _portas.Add(porta);
                    return porta;
                },
                () => [Porta],
                intervaloWatchdog: null);
        }

        [TearDown]
        public void TearDown() => _engine.Dispose();

        [TestCase(FimDeLinha.Nenhum, "help")]
        [TestCase(FimDeLinha.CR, "help\r")]
        [TestCase(FimDeLinha.LF, "help\n")]
        [TestCase(FimDeLinha.CRLF, "help\r\n")]
        public void EnviarDados_ComFimDeLinha_DeveEscreverTextoETerminadorNumaUnicaEscrita(FimDeLinha fim, string esperado)
        {
            _engine.Conectar(Porta, 9600);

            Assert.That(_engine.EnviarDados("help", fim), Is.True);
            Assert.That(PortaAtual.Escritos, Is.EqualTo(new[] { esperado }));
        }

        [Test]
        public void EnviarDados_OverloadBoolPelaInterface_DeveMapearParaLFOuNenhum()
        {
            ISerialEngine engine = _engine;
            engine.Conectar(Porta, 9600);

            Assert.That(engine.EnviarDados("a"), Is.True);
            Assert.That(engine.EnviarDados("b", adicionarNovaLinha: false), Is.True);
            Assert.That(PortaAtual.Escritos, Is.EqualTo(new[] { "a\n", "b" }));
        }

        [Test]
        public void EnviarDados_SemConexao_DeveRetornarFalse()
        {
            Assert.That(_engine.EnviarDados("a", FimDeLinha.CRLF), Is.False);
            Assert.That(_engine.EnviarDados("", FimDeLinha.LF), Is.False);
        }

        [TestCase(FimDeLinha.CR, "\r")]
        [TestCase(FimDeLinha.LF, "\n")]
        [TestCase(FimDeLinha.CRLF, "\r\n")]
        public void EnviarDados_TextoVazioComTerminador_DeveEnviarSoOTerminador(FimDeLinha fim, string esperado)
        {
            _engine.Conectar(Porta, 9600);

            Assert.That(_engine.EnviarDados("", fim), Is.True);
            Assert.That(PortaAtual.Escritos, Is.EqualTo(new[] { esperado }));
        }

        [Test]
        public void EnviarDados_TextoVazioSemTerminador_DeveRetornarFalseSemEscrever()
        {
            _engine.Conectar(Porta, 9600);

            Assert.That(_engine.EnviarDados("", FimDeLinha.Nenhum), Is.False);
            Assert.That(PortaAtual.Escritos, Is.Empty);
        }

        [Test]
        public void EnviarDados_OverloadBoolComTextoVazio_ContinuaRetornandoFalse()
        {
            ISerialEngine pelaInterface = _engine;
            _engine.Conectar(Porta, 9600);

            Assert.That(_engine.EnviarDados(""), Is.False);
            Assert.That(pelaInterface.EnviarDados(""), Is.False);
            Assert.That(PortaAtual.Escritos, Is.Empty);
        }

        [Test]
        public void EnviarDados_FimDeLinhaInvalido_DeveLancarSemEscrever()
        {
            _engine.Conectar(Porta, 9600);

            Assert.Throws<ArgumentOutOfRangeException>(() => _engine.EnviarDados("a", (FimDeLinha)42));
            Assert.That(PortaAtual.Escritos, Is.Empty);
        }

        [Test]
        public void EnviarDados_FalhaDeEscritaComFimDeLinha_DeveRetornarFalseEEntrarEmReconexao()
        {
            int perdas = 0;
            _engine.ConexaoPerdida += (_, _) => perdas++;
            _engine.Conectar(Porta, 9600);
            PortaAtual.FalhaNaEscrita = new System.IO.IOException("cabo removido");

            Assert.That(_engine.EnviarDados("a", FimDeLinha.CRLF), Is.False);
            Assert.That(_engine.TentandoReconectar, Is.True);
            Assert.That(perdas, Is.EqualTo(1));
        }
    }

    [TestFixture]
    public class BancadaFimDeLinhaTests
    {
        private FakeSerialEngine _serial;
        private BancadaEngine _engine;

        [SetUp]
        public void Setup()
        {
            _serial = new FakeSerialEngine { EstaConectado = true };
            _engine = new BancadaEngine(_serial, relogio: new FakeTimeProvider());
        }

        [TearDown]
        public void TearDown()
        {
            _engine.Dispose();
            _serial.Dispose(); // idempotente; o BancadaEngine já o descartou
        }

        [Test]
        public void EnviarComandoManual_SemFimDeLinha_DeveUsarLF()
        {
            Assert.That(_engine.EnviarComandoManual("help"), Is.True);

            Assert.That(_serial.ComandosTransmitidos[^1], Is.EqualTo("help"));
            Assert.That(_serial.FinsDeLinha[^1], Is.EqualTo(FimDeLinha.LF));
        }

        [TestCase(FimDeLinha.Nenhum)]
        [TestCase(FimDeLinha.CR)]
        [TestCase(FimDeLinha.LF)]
        [TestCase(FimDeLinha.CRLF)]
        public void EnviarComandoManual_ComFimDeLinha_DeveRepassarAoSerial(FimDeLinha fim)
        {
            Assert.That(_engine.EnviarComandoManual("  cabo 3 ", fim), Is.True);

            Assert.That(_serial.ComandosTransmitidos[^1], Is.EqualTo("cabo 3"));
            Assert.That(_serial.FinsDeLinha[^1], Is.EqualTo(fim));
        }

        [TestCase("", FimDeLinha.CR)]
        [TestCase("   ", FimDeLinha.LF)]
        [TestCase("", FimDeLinha.CRLF)]
        public void EnviarComandoManual_Vazio_DeveEnviarSoOTerminadorSemEsperarResposta(string comando, FimDeLinha fim)
        {
            string? enviado = null;
            int concluidos = 0;
            _engine.ComandoEnviado += (_, c) => enviado = c;
            _engine.ComandoConcluido += (_, _) => concluidos++;

            Assert.That(_engine.EnviarComandoManual(comando, fim), Is.True);

            Assert.That(_serial.ComandosTransmitidos, Is.EqualTo(new[] { "" }));
            Assert.That(_serial.FinsDeLinha, Is.EqualTo(new[] { fim }));
            Assert.That(enviado, Is.EqualTo(""));
            Assert.That(concluidos, Is.Zero);

            // A linha vazia não ocupa a fila: o próximo comando é confirmado normalmente pelo OK
            ConclusaoComando? conclusao = null;
            _engine.ComandoConcluido += (_, c) => conclusao = c;
            _engine.DefinirCabo(EstadoCaboPP.Cabo20A);
            _serial.SimularRecepcao("OK\r\n");
            Assert.That(conclusao?.Resultado, Is.EqualTo(ResultadoComando.Confirmado));
            Assert.That(conclusao?.Comando, Is.EqualTo("cabo 3"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void EnviarComandoManual_VazioSemTerminador_DeveRetornarFalse(string comando)
        {
            Assert.That(_engine.EnviarComandoManual(comando, FimDeLinha.Nenhum), Is.False);
            Assert.That(_serial.ComandosTransmitidos, Is.Empty);
        }

        [Test]
        public void EnviarComandoManual_VazioSemConexao_DeveRetornarFalse()
        {
            _serial.EstaConectado = false;
            Assert.That(_engine.EnviarComandoManual("", FimDeLinha.LF), Is.False);
        }

        [Test]
        public void ComandosAutomaticos_DevemSempreUsarLF()
        {
            _engine.EnviarComandoManual("help", FimDeLinha.CR);
            _engine.DefinirCabo(EstadoCaboPP.Cabo20A);
            _engine.DefinirEstado(EstadoVeiculoCP.BXNominal);
            _engine.SolicitarStatusCP();

            Assert.That(_serial.FinsDeLinha.Skip(1), Is.All.EqualTo(FimDeLinha.LF));
            Assert.That(_serial.FinsDeLinha, Has.Count.EqualTo(4));
        }

        [Test]
        public void EnviarComandoManual_FimDeLinhaInvalido_DeveLancarSemTransmitir()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _engine.EnviarComandoManual("help", (FimDeLinha)7));
            Assert.That(_serial.ComandosTransmitidos, Is.Empty);
        }
    }

    [TestFixture]
    public class OperacoesBitsTests
    {
        [TestCase(0UL, 0, 1UL)]
        [TestCase(1UL, 0, 0UL)]
        [TestCase(0UL, 63, 0x8000_0000_0000_0000UL)]
        [TestCase(0xFFUL, 4, 0xEFUL)]
        public void InverterBit_DeveAlternarSoOBitIndicado(ulong valor, int bit, ulong esperado)
        {
            Assert.That(OperacoesBits.InverterBit(valor, bit), Is.EqualTo(esperado));
            Assert.That(OperacoesBits.InverterBit(esperado, bit), Is.EqualTo(valor));
        }

        [Test]
        public void LerBit_DeveRefletirCadaPosicao()
        {
            const ulong valor = 0x8000_0000_0000_0005UL;

            Assert.That(OperacoesBits.LerBit(valor, 0), Is.True);
            Assert.That(OperacoesBits.LerBit(valor, 1), Is.False);
            Assert.That(OperacoesBits.LerBit(valor, 2), Is.True);
            Assert.That(OperacoesBits.LerBit(valor, 62), Is.False);
            Assert.That(OperacoesBits.LerBit(valor, 63), Is.True);
        }

        [TestCase(-1)]
        [TestCase(64)]
        [TestCase(int.MinValue)]
        public void BitForaDaFaixa_DeveLancarArgumentOutOfRange(int bit)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => OperacoesBits.InverterBit(0, bit));
            Assert.Throws<ArgumentOutOfRangeException>(() => OperacoesBits.LerBit(0, bit));
        }

        [Test]
        public void TrocarEndian_DeveInverterOs8Bytes()
        {
            Assert.That(OperacoesBits.TrocarEndian(0x0102_0304_0506_0708UL), Is.EqualTo(0x0807_0605_0403_0201UL));
            Assert.That(OperacoesBits.TrocarEndian(0xFFUL), Is.EqualTo(0xFF00_0000_0000_0000UL));
        }

        [Test]
        public void Deslocamentos_DevemSerLogicosEDescartarOBitQueSai()
        {
            Assert.That(OperacoesBits.DeslocarEsquerda(0x8000_0000_0000_0001UL), Is.EqualTo(2UL));
            Assert.That(OperacoesBits.DeslocarDireita(0x8000_0000_0000_0001UL), Is.EqualTo(0x4000_0000_0000_0000UL));
            Assert.That(OperacoesBits.DeslocarDireita(ulong.MaxValue), Is.EqualTo(0x7FFF_FFFF_FFFF_FFFFUL));
        }

        [Test]
        public void NotEMaximo_DevemComplementarTodosOsBits()
        {
            Assert.That(OperacoesBits.Maximo, Is.EqualTo(ulong.MaxValue));
            Assert.That(OperacoesBits.Not(0UL), Is.EqualTo(OperacoesBits.Maximo));
            Assert.That(OperacoesBits.Not(0x00FF_00FF_00FF_00FFUL), Is.EqualTo(0xFF00_FF00_FF00_FF00UL));
        }

        [TestCase(0x4142_4344_4546_4748UL, "ABCDEFGH")]
        [TestCase(0x0000_0000_0000_0041UL, ".......A")]
        [TestCase(0x2000_7E7F_1F80_FF30UL, " .~....0")]
        [TestCase(0UL, "........")]
        public void ParaAscii_DeveSerBigEndianComNaoImprimivelComoPonto(ulong valor, string esperado)
        {
            Assert.That(OperacoesBits.ParaAscii(valor), Is.EqualTo(esperado));
        }

        [TestCase(0UL, 0L)]
        [TestCase(0x7FFF_FFFF_FFFF_FFFFUL, long.MaxValue)]
        [TestCase(0x8000_0000_0000_0000UL, long.MinValue)]
        [TestCase(ulong.MaxValue, -1L)]
        public void ParaInt64_DeveUsarComplementoDeDois(ulong valor, long esperado)
        {
            Assert.That(OperacoesBits.ParaInt64(valor), Is.EqualTo(esperado));
        }
    }

    /// <summary>
    /// Progresso e estatísticas do Pomodoro com relógio virtual (mesmo padrão de <see cref="PomodoroEngineTests"/>).
    /// </summary>
    [TestFixture]
    public class PomodoroNordicTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

        private FakeTimeProvider _relogio;
        private PomodoroEngine _engine;
        private EstatisticasSessao _stats;

        [SetUp]
        public void Setup()
        {
            _relogio = new FakeTimeProvider();
            _engine = new PomodoroEngine(_relogio);
            _stats = new EstatisticasSessao(_engine);
        }

        [TearDown]
        public void TearDown()
        {
            _stats.Dispose();
            _engine.Dispose();
        }

        private void AvancarAte(TimeSpan restante, TimeSpan avanco)
        {
            _relogio.Advance(avanco);
            Assert.That(SpinWait.SpinUntil(() => _engine.TempoRestante <= restante, Timeout), Is.True,
                "O tick não refletiu o avanço do relógio.");
        }

        // Conclui o ciclo em andamento avançando o relógio virtual até o motor parar.
        private void Concluir()
        {
            Assert.That(SpinWait.SpinUntil(() =>
            {
                if (!_engine.EstaRodando) return true;
                _relogio.Advance(TimeSpan.FromSeconds(30));
                Thread.Yield();
                return false;
            }, Timeout), Is.True, "O ciclo não concluiu.");
        }

        private void AguardarFocos(int focos) =>
            Assert.That(SpinWait.SpinUntil(() => _stats.FocosConcluidos == focos, Timeout), Is.True,
                "A conclusão não chegou às estatísticas.");

        [Test]
        public void Progresso_DeveIrDe1A0EZerarAoParar()
        {
            Assert.That(_engine.Progresso, Is.EqualTo(0.0));

            _engine.Iniciar(TimeSpan.FromMinutes(10));
            Assert.That(_engine.Progresso, Is.EqualTo(1.0));

            AvancarAte(TimeSpan.FromMinutes(7.5), TimeSpan.FromMinutes(2.5));
            Assert.That(_engine.Progresso, Is.EqualTo(0.75).Within(1e-9));

            _engine.Pausar();
            _relogio.Advance(TimeSpan.FromHours(1));
            Assert.That(_engine.Progresso, Is.EqualTo(0.75).Within(1e-9), "Na pausa o anel não deve mudar.");

            _engine.Parar();
            Assert.That(_engine.Progresso, Is.EqualTo(0.0));
        }

        [Test]
        public void Progresso_AoConcluir_DeveSerZero()
        {
            _engine.Iniciar(TimeSpan.FromMinutes(1));
            Concluir();

            Assert.That(_engine.Progresso, Is.EqualTo(0.0));
        }

        [Test]
        public void Estatisticas_Iniciais_DevemEstarZeradasNoCiclo1()
        {
            Assert.That(_stats.CicloAtual, Is.EqualTo(1));
            Assert.That(_stats.FocosConcluidos, Is.Zero);
            Assert.That(_stats.Interrupcoes, Is.Zero);
            Assert.That(_stats.TempoEmFoco, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void FocoConcluido_DeveSomarTempoEAvancarCiclo()
        {
            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);
            Concluir();
            AguardarFocos(1);

            Assert.That(_stats.TempoEmFoco, Is.EqualTo(TimeSpan.FromMinutes(25)));
            Assert.That(_stats.CicloAtual, Is.EqualTo(2));
            Assert.That(_stats.Interrupcoes, Is.Zero);
        }

        [Test]
        public void CicloAtual_DeveAvancarAteO4oFocoEFicarEm4DuranteODescansoLongo()
        {
            int[] esperadoAposConcluir = [2, 3, 4, 4];
            for (int i = 1; i <= 4; i++)
            {
                _engine.Iniciar(TimeSpan.FromMinutes(1), EstadoPomodoro.Foco);
                Assert.That(_stats.CicloAtual, Is.EqualTo(i), $"durante o foco {i}");
                Concluir();
                AguardarFocos(i);
                Assert.That(_stats.CicloAtual, Is.EqualTo(esperadoAposConcluir[i - 1]), $"após {i} foco(s)");
            }

            // Descanso longo após o 4º foco: continua 4/4 (inclusive pausado e concluído)
            _engine.Iniciar(TimeSpan.FromMinutes(15), EstadoPomodoro.DescansoLongo);
            Assert.That(_stats.CicloAtual, Is.EqualTo(4));
            _engine.Pausar();
            Assert.That(_stats.CicloAtual, Is.EqualTo(4));
            _engine.Retomar();
            Concluir();
            Assert.That(_stats.CicloAtual, Is.EqualTo(4));

            // Ao iniciar o próximo foco, volta a 1/4 (mesmo se esse foco for interrompido)
            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);
            Assert.That(_stats.CicloAtual, Is.EqualTo(1));
            _engine.Parar();
            Assert.That(_stats.CicloAtual, Is.EqualTo(1));

            _engine.Iniciar(TimeSpan.FromMinutes(1), EstadoPomodoro.Foco);
            Concluir();
            AguardarFocos(5);
            Assert.That(_stats.CicloAtual, Is.EqualTo(2));
        }

        [Test]
        public void Descansos_NaoContamFocoNemInterrupcao()
        {
            _engine.Iniciar(TimeSpan.FromMinutes(5), EstadoPomodoro.DescansoCurto);
            AvancarAte(TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(2));
            _engine.Pausar();
            _engine.Retomar();
            _engine.Parar();

            _engine.Iniciar(TimeSpan.FromMinutes(1), EstadoPomodoro.DescansoLongo);
            Concluir();

            Assert.That(_stats.TempoEmFoco, Is.EqualTo(TimeSpan.Zero));
            Assert.That(_stats.Interrupcoes, Is.Zero);
            Assert.That(_stats.FocosConcluidos, Is.Zero);
        }

        [Test]
        public void PausarERetomarFoco_ContaInterrupcaoENaoContaOTempoPausado()
        {
            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);
            AvancarAte(TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(10));

            Assert.That(_stats.TempoEmFoco, Is.EqualTo(TimeSpan.FromMinutes(10)), "Valor ao vivo durante o foco.");

            _engine.Pausar();
            _relogio.Advance(TimeSpan.FromHours(1));
            Assert.That(_stats.Interrupcoes, Is.EqualTo(1));
            Assert.That(_stats.TempoEmFoco, Is.EqualTo(TimeSpan.FromMinutes(10)));

            _engine.Retomar();
            Concluir();
            AguardarFocos(1);

            Assert.That(_stats.TempoEmFoco, Is.EqualTo(TimeSpan.FromMinutes(25)));
            Assert.That(_stats.Interrupcoes, Is.EqualTo(1));
            Assert.That(_stats.CicloAtual, Is.EqualTo(2));
        }

        [Test]
        public void PararFoco_ContaInterrupcaoESomaSoOTempoCorrido()
        {
            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);
            AvancarAte(TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(5));

            _engine.Parar();

            Assert.That(_stats.Interrupcoes, Is.EqualTo(1));
            Assert.That(_stats.TempoEmFoco, Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(_stats.FocosConcluidos, Is.Zero);
            Assert.That(_stats.CicloAtual, Is.EqualTo(1));
        }

        [Test]
        public void PararFocoJaPausado_NaoContaInterrupcaoDeNovo()
        {
            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);
            AvancarAte(TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(5));
            _engine.Pausar();
            _engine.Parar();
            _engine.Parar(); // já parado: sem transição

            Assert.That(_stats.Interrupcoes, Is.EqualTo(1));
            Assert.That(_stats.TempoEmFoco, Is.EqualTo(TimeSpan.FromMinutes(5)));
        }

        [Test]
        public void IniciarPorCimaDeFocoAtivo_ContaComoInterrupcao()
        {
            var transicoes = new List<TransicaoPomodoro>();
            _engine.OnTransicao += t => { lock (transicoes) transicoes.Add(t); };

            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);
            AvancarAte(TimeSpan.FromMinutes(22), TimeSpan.FromMinutes(3));
            _engine.Iniciar(TimeSpan.FromMinutes(5), EstadoPomodoro.DescansoCurto);

            Assert.That(_stats.Interrupcoes, Is.EqualTo(1));
            Assert.That(_stats.TempoEmFoco, Is.EqualTo(TimeSpan.FromMinutes(3)));

            TipoTransicaoPomodoro[] tipos;
            lock (transicoes) tipos = transicoes.Select(t => t.Tipo).ToArray();
            Assert.That(tipos, Is.EqualTo(new[]
            {
                TipoTransicaoPomodoro.Iniciado, TipoTransicaoPomodoro.Parado, TipoTransicaoPomodoro.Iniciado
            }));
        }

        [Test]
        public async Task OnTransicao_Concluido_DeveVirDepoisDoTick0EAntesDoOnEstadoMudou()
        {
            var ordem = new List<string>();
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _engine.OnTick += r => { if (r == TimeSpan.Zero) lock (ordem) ordem.Add("tick0"); };
            _engine.OnTransicao += t =>
            {
                if (t.Tipo == TipoTransicaoPomodoro.Concluido) lock (ordem) ordem.Add($"concluido:{t.Estado}");
            };
            _engine.OnEstadoMudou += _ =>
            {
                lock (ordem) ordem.Add("estado");
                tcs.TrySetResult();
            };

            _engine.Iniciar(TimeSpan.FromMinutes(1), EstadoPomodoro.Foco);
            _relogio.Advance(TimeSpan.FromMinutes(1));
            await tcs.Task.WaitAsync(Timeout);

            Assert.That(ordem, Is.EqualTo(new[] { "tick0", "concluido:Foco", "estado" }));
        }

        [Test]
        public void OnTransicao_AssinanteQueLanca_NaoDerrubaOMotorNemAsEstatisticas()
        {
            _engine.OnTransicao += _ => throw new InvalidOperationException("assinante com defeito");

            Assert.DoesNotThrow(() => _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco));
            Assert.DoesNotThrow(() => _engine.Parar());
            Assert.That(_stats.Interrupcoes, Is.EqualTo(1));
        }

        [Test]
        public void Estatisticas_AposDispose_NaoRecebemMaisTransicoes()
        {
            int mudancas = 0;
            _stats.Mudou += (_, _) => mudancas++;

            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);
            Assert.That(mudancas, Is.EqualTo(1));

            _stats.Dispose();
            _engine.Parar();

            Assert.That(_stats.Interrupcoes, Is.Zero);
            Assert.That(mudancas, Is.EqualTo(1));
        }

        [Test]
        public void Transicoes_DevemCarregarONumeroDoCiclo()
        {
            var transicoes = new List<TransicaoPomodoro>();
            _engine.OnTransicao += t => { lock (transicoes) transicoes.Add(t); };

            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);
            _engine.Pausar();
            _engine.Retomar();
            _engine.Iniciar(TimeSpan.FromMinutes(5), EstadoPomodoro.DescansoCurto); // Parado(1) + Iniciado(2)
            _engine.Parar();

            (TipoTransicaoPomodoro, long)[] obtido;
            lock (transicoes) obtido = transicoes.Select(t => (t.Tipo, t.Ciclo)).ToArray();
            Assert.That(obtido, Is.EqualTo(new[]
            {
                (TipoTransicaoPomodoro.Iniciado, 1L), (TipoTransicaoPomodoro.Pausado, 1L),
                (TipoTransicaoPomodoro.Retomado, 1L), (TipoTransicaoPomodoro.Parado, 1L),
                (TipoTransicaoPomodoro.Iniciado, 2L), (TipoTransicaoPomodoro.Parado, 2L)
            }));
        }

        /// <summary>
        /// Regressão (revisão Sentinela, item 2): o timer conclui o foco 1 e solta o lock; antes de notificar o
        /// Concluido, a UI chama Iniciar(Foco). O Concluido do ciclo 1 chega DEPOIS do Iniciado do ciclo 2.
        /// Antes: somava 25 min fantasmas do ciclo 2, perdia o trecho dele e zerava o "foco iniciado".
        /// </summary>
        [Test]
        public void ConclusaoAtrasadaDepoisDeIniciarNovoFoco_NaoSomaTempoFantasmaNemPerdeOCicloNovo()
        {
            var foco = TimeSpan.FromMinutes(25);
            _stats.ProcessarTransicao(new(TipoTransicaoPomodoro.Iniciado, EstadoPomodoro.Foco, foco, foco, 1));
            _stats.ProcessarTransicao(new(TipoTransicaoPomodoro.Iniciado, EstadoPomodoro.Foco, foco, foco, 2));
            _stats.ProcessarTransicao(new(TipoTransicaoPomodoro.Concluido, EstadoPomodoro.Foco, TimeSpan.Zero, foco, 1));

            Assert.That(_stats.FocosConcluidos, Is.EqualTo(1), "O foco 1 concluiu de verdade.");
            Assert.That(_stats.TempoEmFoco, Is.EqualTo(foco), "Só os 25 min do foco 1.");
            Assert.That(_stats.CicloAtual, Is.EqualTo(2), "O foco 2 está em andamento.");

            // O foco 2 é interrompido aos 5 min: o trecho dele continua aberto e é somado
            _stats.ProcessarTransicao(new(TipoTransicaoPomodoro.Parado, EstadoPomodoro.Foco, TimeSpan.FromMinutes(20), foco, 2));

            Assert.That(_stats.TempoEmFoco, Is.EqualTo(TimeSpan.FromMinutes(30)));
            Assert.That(_stats.Interrupcoes, Is.EqualTo(1));
            Assert.That(_stats.FocosConcluidos, Is.EqualTo(1));
        }

        [Test]
        public void ConclusaoAtrasadaDo4oFoco_DepoisDeIniciarOProximo_DeveMostrar1de4()
        {
            var foco = TimeSpan.FromMinutes(1);
            for (long c = 1; c <= 3; c++)
            {
                _stats.ProcessarTransicao(new(TipoTransicaoPomodoro.Iniciado, EstadoPomodoro.Foco, foco, foco, c));
                _stats.ProcessarTransicao(new(TipoTransicaoPomodoro.Concluido, EstadoPomodoro.Foco, TimeSpan.Zero, foco, c));
            }

            _stats.ProcessarTransicao(new(TipoTransicaoPomodoro.Iniciado, EstadoPomodoro.Foco, foco, foco, 4));
            _stats.ProcessarTransicao(new(TipoTransicaoPomodoro.Iniciado, EstadoPomodoro.Foco, foco, foco, 5));
            _stats.ProcessarTransicao(new(TipoTransicaoPomodoro.Concluido, EstadoPomodoro.Foco, TimeSpan.Zero, foco, 4));

            Assert.That(_stats.FocosConcluidos, Is.EqualTo(4));
            Assert.That(_stats.CicloAtual, Is.EqualTo(1), "O 5º foco (1º da nova série) já começou.");
            Assert.That(_stats.TempoEmFoco, Is.EqualTo(TimeSpan.FromMinutes(4)));
        }

        [Test]
        public void TransicoesDeCicloAntigo_QueNaoSaoConclusao_SaoIgnoradas()
        {
            var foco = TimeSpan.FromMinutes(25);
            _stats.ProcessarTransicao(new(TipoTransicaoPomodoro.Iniciado, EstadoPomodoro.Foco, foco, foco, 1));
            _stats.ProcessarTransicao(new(TipoTransicaoPomodoro.Parado, EstadoPomodoro.Foco, TimeSpan.FromMinutes(20), foco, 1));
            _stats.ProcessarTransicao(new(TipoTransicaoPomodoro.Iniciado, EstadoPomodoro.Foco, foco, foco, 2));

            // Atrasadas do ciclo 1: não podem fechar o trecho do ciclo 2 nem contar de novo
            _stats.ProcessarTransicao(new(TipoTransicaoPomodoro.Pausado, EstadoPomodoro.Foco, TimeSpan.FromMinutes(20), foco, 1));
            _stats.ProcessarTransicao(new(TipoTransicaoPomodoro.Concluido, EstadoPomodoro.Foco, TimeSpan.Zero, foco, 1));

            Assert.That(_stats.Interrupcoes, Is.EqualTo(1));
            Assert.That(_stats.FocosConcluidos, Is.Zero, "O ciclo 1 foi parado, não concluído.");
            Assert.That(_stats.TempoEmFoco, Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(_stats.CicloAtual, Is.EqualTo(1));
        }

        [Test]
        public void Estatisticas_EngineNulo_DeveLancar()
        {
            Assert.Throws<ArgumentNullException>(() => _ = new EstatisticasSessao(null!));
        }
    }
}
