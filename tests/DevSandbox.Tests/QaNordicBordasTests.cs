using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DevSandbox.Core.Bancada;
using DevSandbox.Core.Converters;
using DevSandbox.Core.Pomodoro;
using DevSandbox.Core.Serial;
using DevSandbox.Tests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace DevSandbox.Tests
{
    // QA do visual Nordic: casos de borda e regressão sobre o Core novo (complementa NordicCoreTests).

    [TestFixture]
    public class QaNordicFimDeLinhaTests
    {
        [Test]
        public void ObterTerminador_NaoDeveCompartilharEstadoEntreChamadas()
        {
            foreach (FimDeLinha fim in Enum.GetValues<FimDeLinha>())
            {
                Assert.That(fim.ObterTerminador(), Is.Not.Null);
                Assert.That(fim.ObterRotulo(), Is.Not.Empty);
            }
        }

        [Test]
        public void Rotulos_DevemSerUnicos()
        {
            var rotulos = Enum.GetValues<FimDeLinha>().Select(f => f.ObterRotulo()).ToList();
            Assert.That(rotulos, Is.Unique);
        }

        [TestCase(FimDeLinha.CR, 1)]
        [TestCase(FimDeLinha.LF, 1)]
        [TestCase(FimDeLinha.CRLF, 2)]
        [TestCase(FimDeLinha.Nenhum, 0)]
        public void Terminador_ComprimentoEsperado(FimDeLinha fim, int tamanho)
        {
            Assert.That(fim.ObterTerminador(), Has.Length.EqualTo(tamanho));
        }
    }

    [TestFixture]
    public class QaNordicSupervisorioEnvioTests
    {
        private const string Porta = "COM7";
        private List<FakeSerialPort> _portas;
        private SupervisorioSerialEngine _engine;

        [SetUp]
        public void Setup()
        {
            _portas = [];
            _engine = new SupervisorioSerialEngine(
                (nome, baud) =>
                {
                    var p = new FakeSerialPort(nome, baud);
                    _portas.Add(p);
                    return p;
                },
                () => [Porta],
                intervaloWatchdog: null);
            _engine.Conectar(Porta, 9600);
        }

        [TearDown]
        public void TearDown() => _engine.Dispose();

        [Test]
        public void EnviarDados_TextoNuloComTerminador_DeveEnviarSoOTerminador()
        {
            Assert.That(_engine.EnviarDados(null!, FimDeLinha.LF), Is.True);
            Assert.That(_portas[^1].Escritos, Is.EqualTo(new[] { "\n" }));
        }

        [Test]
        public void EnviarDados_TextoNuloSemTerminador_DeveRetornarFalse()
        {
            Assert.That(_engine.EnviarDados(null!, FimDeLinha.Nenhum), Is.False);
            Assert.That(_portas[^1].Escritos, Is.Empty);
        }

        [Test]
        public void EnviarDados_TextoJaTerminadoEmLFComCRLF_NaoDeveDeduplicar()
        {
            // O motor não "adivinha" a intenção: texto + terminador, literalmente
            Assert.That(_engine.EnviarDados("a\n", FimDeLinha.CRLF), Is.True);
            Assert.That(_portas[^1].Escritos, Is.EqualTo(new[] { "a\n\r\n" }));
        }

        [Test]
        public void EnviarDados_EspacosNasPontas_DevemSerPreservados()
        {
            Assert.That(_engine.EnviarDados("  x  ", FimDeLinha.CR), Is.True);
            Assert.That(_portas[^1].Escritos, Is.EqualTo(new[] { "  x  \r" }));
        }

        [Test]
        public void EnviarDados_TextoSoDeEspacosSemTerminador_DeveSerEnviado()
        {
            // Diferente da Bancada (que faz Trim), o Supervisório é um terminal cru
            Assert.That(_engine.EnviarDados("   ", FimDeLinha.Nenhum), Is.True);
            Assert.That(_portas[^1].Escritos, Is.EqualTo(new[] { "   " }));
        }

        [Test]
        public void EnviarDados_AposDesconectar_DeveRetornarFalse()
        {
            _engine.Desconectar();
            Assert.That(_engine.EnviarDados("x", FimDeLinha.LF), Is.False);
        }

        [Test]
        public void EnviarDados_AposDispose_DeveRetornarFalseSemLancar()
        {
            _engine.Dispose();
            Assert.That(() => _engine.EnviarDados("x", FimDeLinha.LF), Throws.Nothing);
            Assert.That(_engine.EnviarDados("x", FimDeLinha.LF), Is.False);
        }

        [Test]
        public void EnviarDados_VariosEnvios_DevemManterAOrdem()
        {
            _engine.EnviarDados("a", FimDeLinha.LF);
            _engine.EnviarDados("b", FimDeLinha.CR);
            _engine.EnviarDados("c", FimDeLinha.Nenhum);
            Assert.That(_portas[^1].Escritos, Is.EqualTo(new[] { "a\n", "b\r", "c" }));
        }
    }

    [TestFixture]
    public class QaNordicBancadaManualTests
    {
        private FakeSerialEngine _serial;
        private BancadaEngine _engine;
        private List<ConclusaoComando> _conclusoes;

        [SetUp]
        public void Setup()
        {
            _serial = new FakeSerialEngine { EstaConectado = true };
            _engine = new BancadaEngine(_serial);
            _conclusoes = [];
            _engine.ComandoConcluido += (_, c) => _conclusoes.Add(c);
        }

        [TearDown]
        public void TearDown()
        {
            _engine.Dispose();
            _serial.Dispose();
        }

        [TestCase("help\n")]
        [TestCase("help\r\n")]
        [TestCase("\thelp\r")]
        public void EnviarComandoManual_EolNasPontas_DeveSerAparadoEEnviado(string entrada)
        {
            Assert.That(_engine.EnviarComandoManual(entrada, FimDeLinha.CR), Is.True);
            Assert.That(_serial.ComandosTransmitidos, Is.EqualTo(new[] { "help" }));
            Assert.That(_serial.FinsDeLinha, Is.EqualTo(new[] { FimDeLinha.CR }));
        }

        [TestCase("cabo 1\ncabo 2")]
        [TestCase("cabo 1\rcabo 2")]
        [TestCase("a\r\nb")]
        public void EnviarComandoManual_EolNoMeio_DeveSerRejeitadoSemTransmitir(string entrada)
        {
            Assert.That(_engine.EnviarComandoManual(entrada, FimDeLinha.LF), Is.False);
            Assert.That(_serial.ComandosTransmitidos, Is.Empty);
            Assert.That(_conclusoes, Has.Count.EqualTo(1));
            Assert.That(_conclusoes[0].Resultado, Is.EqualTo(ResultadoComando.NaoEnviado));
            Assert.That(_conclusoes[0].Detalhe, Is.EqualTo(BancadaEngine.DetalheCaractereInvalido));
        }

        [Test]
        public void EnviarComandoManual_Nulo_NaoDeveLancar()
        {
            Assert.That(() => _engine.EnviarComandoManual(null!, FimDeLinha.LF), Throws.Nothing);
        }

        [Test]
        public void EnviarComandoManual_VazioComLF_RepetidoVariasVezes_NaoDeveAcumularPendencias()
        {
            for (int i = 0; i < 5; i++)
                Assert.That(_engine.EnviarComandoManual("", FimDeLinha.LF), Is.True);

            Assert.That(_serial.ComandosTransmitidos, Has.Count.EqualTo(5));
            Assert.That(_conclusoes, Is.Empty, "Linha vazia não deve gerar conclusão nem timeout.");
        }

        [Test]
        public void EnviarComandoManual_SerialRecusa_DeveConcluirComoNaoEnviado()
        {
            _serial.FalharEnvio = true;
            Assert.That(_engine.EnviarComandoManual("cabo 2", FimDeLinha.CRLF), Is.False);
            Assert.That(_conclusoes.Single().Resultado, Is.EqualTo(ResultadoComando.NaoEnviado));
        }

        [Test]
        public void EnviarComandoManual_AposDispose_DeveRetornarFalse()
        {
            _engine.Dispose();
            Assert.That(_engine.EnviarComandoManual("help", FimDeLinha.LF), Is.False);
        }

        [TestCase(FimDeLinha.Nenhum)]
        [TestCase(FimDeLinha.CR)]
        [TestCase(FimDeLinha.LF)]
        [TestCase(FimDeLinha.CRLF)]
        public void EnviarComandoManual_ComandoDeCabo_ContinuaInterpretandoOAlvoQualquerQuSejaOEol(FimDeLinha fim)
        {
            Assert.That(_engine.EnviarComandoManual("cabo 3", fim), Is.True);
            _serial.SimularRecepcao("OK\r\n");

            Assert.That(_conclusoes.Single().Resultado, Is.EqualTo(ResultadoComando.Confirmado));
            Assert.That(_conclusoes[0].Cabo, Is.EqualTo(EstadoCaboPP.Cabo20A));
        }
    }

    [TestFixture]
    public class QaNordicOperacoesBitsTests
    {
        [Test]
        public void InverterBit_EmTodasAsPosicoes_DeveAcenderExatamenteUmBitESerInvolutivo()
        {
            for (int bit = 0; bit < OperacoesBits.TotalBits; bit++)
            {
                ulong ligado = OperacoesBits.InverterBit(0, bit);
                Assert.That(ulong.PopCount(ligado), Is.EqualTo(1), $"bit {bit}");
                Assert.That(OperacoesBits.LerBit(ligado, bit), Is.True, $"bit {bit}");
                Assert.That(OperacoesBits.InverterBit(ligado, bit), Is.Zero, $"bit {bit}");
            }
        }

        [Test]
        public void InverterBit_Bit63_NaoDeveSerTratadoComoSinal()
        {
            ulong v = OperacoesBits.InverterBit(0, 63);
            Assert.That(v, Is.EqualTo(0x8000_0000_0000_0000UL));
            Assert.That(OperacoesBits.ParaInt64(v), Is.EqualTo(long.MinValue));
        }

        [Test]
        public void LerBit_ForaDaFaixa_DeveLancarComNomeDoParametro()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => OperacoesBits.LerBit(0, 64))!;
            Assert.That(ex.ParamName, Is.EqualTo("bit"));
        }

        [TestCase(0UL)]
        [TestCase(1UL)]
        [TestCase(0x0102_0304_0506_0708UL)]
        [TestCase(ulong.MaxValue)]
        [TestCase(0x8000_0000_0000_0000UL)]
        public void TrocarEndian_DuasVezes_DeveVoltarAoOriginal(ulong valor)
        {
            Assert.That(OperacoesBits.TrocarEndian(OperacoesBits.TrocarEndian(valor)), Is.EqualTo(valor));
        }

        [Test]
        public void TrocarEndian_Palindromo_DeveManterOValor()
        {
            Assert.That(OperacoesBits.TrocarEndian(0xAABB_CCDD_DDCC_BBAAUL), Is.EqualTo(0xAABB_CCDD_DDCC_BBAAUL));
        }

        [Test]
        public void DeslocarEsquerda_64Vezes_DeveZerar()
        {
            ulong v = ulong.MaxValue;
            for (int i = 0; i < 64; i++) v = OperacoesBits.DeslocarEsquerda(v);
            Assert.That(v, Is.Zero);
        }

        [Test]
        public void DeslocarDireita_64Vezes_DeveZerarSemPreencherSinal()
        {
            ulong v = ulong.MaxValue;
            for (int i = 0; i < 64; i++) v = OperacoesBits.DeslocarDireita(v);
            Assert.That(v, Is.Zero);
            // Lógico (não aritmético): Int64 negativo não propaga o sinal
            Assert.That(OperacoesBits.DeslocarDireita(0x8000_0000_0000_0000UL), Is.EqualTo(0x4000_0000_0000_0000UL));
        }

        [Test]
        public void DeslocarEsquerda_Bit63_DeveSerDescartado()
        {
            Assert.That(OperacoesBits.DeslocarEsquerda(0x8000_0000_0000_0000UL), Is.Zero);
        }

        [Test]
        public void DeslocarEsquerdaSeguidoDeDireita_PerdeOBitMaisAlto()
        {
            ulong v = 0xC000_0000_0000_0001UL;
            Assert.That(OperacoesBits.DeslocarDireita(OperacoesBits.DeslocarEsquerda(v)), Is.EqualTo(0x4000_0000_0000_0001UL));
        }

        [Test]
        public void Not_DuasVezes_DeveVoltarAoOriginal()
        {
            const ulong v = 0x1234_5678_9ABC_DEF0UL;
            Assert.That(OperacoesBits.Not(OperacoesBits.Not(v)), Is.EqualTo(v));
            Assert.That(OperacoesBits.Not(OperacoesBits.Maximo), Is.Zero);
        }

        [TestCase(0x20UL, ' ')]
        [TestCase(0x7EUL, '~')]
        [TestCase(0x1FUL, '.')]
        [TestCase(0x7FUL, '.')]
        [TestCase(0x80UL, '.')]
        [TestCase(0xFFUL, '.')]
        [TestCase(0x0AUL, '.')]
        [TestCase(0x0DUL, '.')]
        [TestCase(0x00UL, '.')]
        public void ParaAscii_FronteirasDeImprimivel_NoByteMenosSignificativo(ulong b, char esperado)
        {
            string s = OperacoesBits.ParaAscii(b);
            Assert.That(s, Has.Length.EqualTo(8));
            Assert.That(s[7], Is.EqualTo(esperado));
            Assert.That(s[..7], Is.EqualTo("......."));
        }

        [Test]
        public void ParaAscii_SempreTem8Caracteres_EOByteMaisSignificativoVemPrimeiro()
        {
            Assert.That(OperacoesBits.ParaAscii(0x4100_0000_0000_0000UL), Is.EqualTo("A......."));
            Assert.That(OperacoesBits.ParaAscii(ulong.MaxValue), Is.EqualTo("........"));
        }

        [Test]
        public void ParaAscii_AposTrocarEndian_DeveInverterAOrdemDosCaracteres()
        {
            const ulong v = 0x4142_4344_4546_4748UL; // "ABCDEFGH"
            Assert.That(OperacoesBits.ParaAscii(OperacoesBits.TrocarEndian(v)), Is.EqualTo("HGFEDCBA"));
        }

        [Test]
        public void ParaInt64_EParaAscii_ConsistentesComOMaximo()
        {
            Assert.That(OperacoesBits.ParaInt64(OperacoesBits.Maximo), Is.EqualTo(-1L));
            Assert.That(OperacoesBits.ParaAscii(OperacoesBits.Maximo), Is.EqualTo("........"));
        }
    }

    [TestFixture]
    public class QaNordicHistoricoTests
    {
        private HistoricoComandos _h;

        [SetUp]
        public void Setup() => _h = new HistoricoComandos();

        [Test]
        public void Limite_DeveSer100()
        {
            Assert.That(HistoricoComandos.Limite, Is.EqualTo(100));
        }

        [Test]
        public void Adicionar_Exatamente100_NaoDescartaNada()
        {
            for (int i = 1; i <= 100; i++) _h.Adicionar($"c{i}");
            Assert.That(_h.Quantidade, Is.EqualTo(100));
            Assert.That(_h.Itens[0], Is.EqualTo("c1"));
            Assert.That(_h.Itens[^1], Is.EqualTo("c100"));
        }

        [Test]
        public void Adicionar_250Itens_DeveManterSoOsUltimos100EmOrdem()
        {
            for (int i = 1; i <= 250; i++) _h.Adicionar($"c{i}");
            Assert.That(_h.Quantidade, Is.EqualTo(100));
            Assert.That(_h.Itens[0], Is.EqualTo("c151"));
            Assert.That(_h.Itens[^1], Is.EqualTo("c250"));
        }

        [Test]
        public void Navegacao_AposEstourarOLimite_DeveCaminharPelosItensRestantes()
        {
            for (int i = 1; i <= 101; i++) _h.Adicionar($"c{i}");

            string atual = "";
            for (int i = 0; i < 200; i++) atual = _h.Anterior(atual);
            Assert.That(atual, Is.EqualTo("c2"), "O mais antigo restante após descartar c1.");
        }

        [Test]
        public void Repeticao_NaoConsecutiva_DeveSerAceita()
        {
            _h.Adicionar("a");
            _h.Adicionar("b");
            Assert.That(_h.Adicionar("a"), Is.True);
            Assert.That(_h.Itens, Is.EqualTo(new[] { "a", "b", "a" }));
        }

        [Test]
        public void Repeticao_ConsecutivaRejeitada_NaoDeveAlterarQuantidade()
        {
            _h.Adicionar("a");
            Assert.That(_h.Adicionar("a"), Is.False);
            Assert.That(_h.Quantidade, Is.EqualTo(1));
        }

        [Test]
        public void Repeticao_ComCaixaOuEspacoDiferente_ContaComoOutroItem()
        {
            _h.Adicionar("Help");
            Assert.That(_h.Adicionar("help"), Is.True);
            Assert.That(_h.Adicionar("help "), Is.True);
            Assert.That(_h.Quantidade, Is.EqualTo(3));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("\t")]
        [TestCase("\r\n")]
        public void Adicionar_VazioNuloOuSoEspacos_DeveSerIgnorado(string? texto)
        {
            Assert.That(_h.Adicionar(texto), Is.False);
            Assert.That(_h.Quantidade, Is.Zero);
        }

        [Test]
        public void Adicionar_Rejeitado_AindaDeveEncerrarANavegacao()
        {
            _h.Adicionar("a");
            _h.Anterior("rascunho");
            Assert.That(_h.EstaNavegando, Is.True);

            _h.Adicionar("");
            Assert.That(_h.EstaNavegando, Is.False);
            Assert.That(_h.Proximo(), Is.Null);
        }

        [Test]
        public void Anterior_ComTextoAtualNuloEHistoricoVazio_DeveDevolverVazio()
        {
            Assert.That(_h.Anterior(null), Is.EqualTo(""));
            Assert.That(_h.EstaNavegando, Is.False);
        }

        [Test]
        public void Anterior_ComTextoAtualNulo_RascunhoDeveVoltarComoVazio()
        {
            _h.Adicionar("a");
            _h.Anterior(null);
            Assert.That(_h.Proximo(), Is.EqualTo(""));
        }

        [Test]
        public void Rascunho_NaoDeveSerSobrescritoPorSubidasSeguintes()
        {
            _h.Adicionar("a");
            _h.Adicionar("b");
            _h.Anterior("meu rascunho");
            _h.Anterior("b");          // texto "atual" já é o item mostrado
            _h.Anterior("a");

            Assert.That(_h.Proximo(), Is.EqualTo("b"));
            Assert.That(_h.Proximo(), Is.EqualTo("meu rascunho"));
            Assert.That(_h.EstaNavegando, Is.False);
        }

        [Test]
        public void Proximo_ForaDaNavegacao_DeveSerNullRepetidamente()
        {
            _h.Adicionar("a");
            Assert.That(_h.Proximo(), Is.Null);
            Assert.That(_h.Proximo(), Is.Null);
        }

        [Test]
        public void CicloCompleto_SubirTudoEDescerTudo_DeveRestaurarORascunho()
        {
            for (int i = 1; i <= 5; i++) _h.Adicionar($"c{i}");

            string t = "digitando";
            for (int i = 0; i < 5; i++) t = _h.Anterior(t);
            Assert.That(t, Is.EqualTo("c1"));

            string? p = null;
            for (int i = 0; i < 5; i++) p = _h.Proximo();
            Assert.That(p, Is.EqualTo("digitando"));
            Assert.That(_h.EstaNavegando, Is.False);
        }

        [Test]
        public void Rascunho_VazioDeveSerRestauradoComoVazioENaoComoNull()
        {
            _h.Adicionar("a");
            _h.Anterior("");
            Assert.That(_h.Proximo(), Is.EqualTo(""));
        }

        [Test]
        public void NovaNavegacao_AposAdicionar_NaoDeveReaproveitarRascunhoAntigo()
        {
            _h.Adicionar("a");
            _h.Anterior("velho");
            _h.Adicionar("b");

            _h.Anterior("novo");
            _h.Anterior("b");
            Assert.That(_h.Proximo(), Is.EqualTo("b"));
            Assert.That(_h.Proximo(), Is.EqualTo("novo"));
        }

        [Test]
        public void ResetarNavegacao_SemItens_NaoDeveLancar()
        {
            Assert.That(() => _h.ResetarNavegacao(), Throws.Nothing);
            Assert.That(_h.EstaNavegando, Is.False);
        }

        [Test]
        public void Adicionar_PreservaTextoOriginalSemTrim()
        {
            _h.Adicionar("  cabo 2  ");
            Assert.That(_h.Itens[0], Is.EqualTo("  cabo 2  "));
        }

        [Test]
        public void Itens_DeveSerSomenteLeitura()
        {
            _h.Adicionar("a");
            Assert.That(_h.Itens, Is.InstanceOf<IReadOnlyList<string>>());
        }
    }

    [TestFixture]
    public class QaNordicPomodoroTests
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

        [Test]
        public void Progresso_RecemIniciado_DeveSer1ParaQualquerDuracao()
        {
            foreach (var d in new[] { TimeSpan.FromMilliseconds(1), TimeSpan.FromMinutes(25), TimeSpan.FromHours(10) })
            {
                _engine.Iniciar(d);
                Assert.That(_engine.Progresso, Is.EqualTo(1.0), d.ToString());
            }
        }

        [Test]
        public void Progresso_NuncaForaDoIntervalo0a1_DuranteUmCiclo()
        {
            _engine.Iniciar(TimeSpan.FromMinutes(10));
            for (int i = 0; i < 5; i++)
            {
                AvancarAte(TimeSpan.FromMinutes(10 - 2 * (i + 1)), TimeSpan.FromMinutes(2));
                Assert.That(_engine.Progresso, Is.InRange(0.0, 1.0));
            }
        }

        [Test]
        public void Progresso_AposPausarERetomar_DeveContinuarDeOndeParou()
        {
            _engine.Iniciar(TimeSpan.FromMinutes(10));
            AvancarAte(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            _engine.Pausar();
            double pausado = _engine.Progresso;
            _engine.Retomar();

            Assert.That(_engine.Progresso, Is.EqualTo(pausado).Within(0.01));
        }

        [Test]
        public void Progresso_IniciarOutroCicloPorCima_DeveReiniciarEm1()
        {
            _engine.Iniciar(TimeSpan.FromMinutes(10));
            AvancarAte(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            _engine.Iniciar(TimeSpan.FromMinutes(5), EstadoPomodoro.DescansoCurto);

            Assert.That(_engine.Progresso, Is.EqualTo(1.0));
        }

        [Test]
        public void Progresso_PararSemIniciar_DeveSerZeroSemLancar()
        {
            Assert.That(() => _engine.Parar(), Throws.Nothing);
            Assert.That(_engine.Progresso, Is.Zero);
        }

        [Test]
        public void Estatisticas_PararSemIniciar_NaoDeveContarNada()
        {
            _engine.Parar();
            Assert.That(_stats.Interrupcoes, Is.Zero);
            Assert.That(_stats.CicloAtual, Is.EqualTo(1));
        }

        [Test]
        public void Estatisticas_PausarSemRodar_NaoDeveContarInterrupcao()
        {
            Assert.That(_engine.Pausar(), Is.False);
            Assert.That(_stats.Interrupcoes, Is.Zero);
        }

        [Test]
        public void Estatisticas_DuasPausasNoMesmoFoco_ContamDuasInterrupcoes()
        {
            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);
            AvancarAte(TimeSpan.FromMinutes(22), TimeSpan.FromMinutes(3));
            _engine.Pausar();
            _engine.Retomar();
            AvancarAte(TimeSpan.FromMinutes(19), TimeSpan.FromMinutes(3));
            _engine.Pausar();

            Assert.That(_stats.Interrupcoes, Is.EqualTo(2));
            Assert.That(_stats.TempoEmFoco, Is.EqualTo(TimeSpan.FromMinutes(6)));
        }

        [Test]
        public void Estatisticas_IniciarPorCimaDeFocoPausado_NaoDeveContarSegundaInterrupcao()
        {
            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);
            AvancarAte(TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(5));
            _engine.Pausar();                       // 1 interrupção
            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);

            Assert.That(_stats.Interrupcoes, Is.EqualTo(1));
            Assert.That(_stats.TempoEmFoco, Is.EqualTo(TimeSpan.FromMinutes(5)));
        }

        [Test]
        public void Estatisticas_FocoInterrompidoEDepoisConcluido_SomaOsDoisTrechos()
        {
            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);
            AvancarAte(TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(5));
            _engine.Parar();                        // 5 min, 1 interrupção

            _engine.Iniciar(TimeSpan.FromMinutes(10), EstadoPomodoro.Foco);
            Concluir();
            Assert.That(SpinWait.SpinUntil(() => _stats.FocosConcluidos == 1, Timeout), Is.True);

            Assert.That(_stats.TempoEmFoco, Is.EqualTo(TimeSpan.FromMinutes(15)));
            Assert.That(_stats.Interrupcoes, Is.EqualTo(1));
        }

        [Test]
        public void Estatisticas_SerieDe4FocosSemInterrupcao_DeveAcumular4xDuracao()
        {
            for (int i = 1; i <= 4; i++)
            {
                _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);
                Concluir();
                int esperado = i;
                Assert.That(SpinWait.SpinUntil(() => _stats.FocosConcluidos == esperado, Timeout), Is.True);
            }

            Assert.That(_stats.TempoEmFoco, Is.EqualTo(TimeSpan.FromMinutes(100)));
            Assert.That(_stats.CicloAtual, Is.EqualTo(4));
            Assert.That(_stats.Interrupcoes, Is.Zero);
        }

        [Test]
        public void Estatisticas_DescansoNoMeioDaSerie_NaoDeveAlterarOCiclo()
        {
            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);
            Concluir();
            Assert.That(SpinWait.SpinUntil(() => _stats.FocosConcluidos == 1, Timeout), Is.True);

            _engine.Iniciar(TimeSpan.FromMinutes(5), EstadoPomodoro.DescansoCurto);
            Assert.That(_stats.CicloAtual, Is.EqualTo(2));
            Concluir();
            Assert.That(_stats.CicloAtual, Is.EqualTo(2));
        }

        [Test]
        public void Estatisticas_EventoMudou_DeveDispararEmCadaTransicaoEAposDisposeNao()
        {
            int mudou = 0;
            _stats.Mudou += (_, _) => Interlocked.Increment(ref mudou);

            _engine.Iniciar(TimeSpan.FromMinutes(5));   // Iniciado
            _engine.Pausar();                           // Pausado
            _engine.Retomar();                          // Retomado
            _engine.Parar();                            // Parado
            Assert.That(mudou, Is.EqualTo(4));

            _stats.Dispose();
            _engine.Iniciar(TimeSpan.FromMinutes(5));
            Assert.That(mudou, Is.EqualTo(4));
        }

        [Test]
        public void Estatisticas_DisposeDuasVezes_NaoDeveLancar()
        {
            _stats.Dispose();
            Assert.That(() => _stats.Dispose(), Throws.Nothing);
        }

        [Test]
        public void Estatisticas_TempoEmFoco_NuncaNegativoNemMaiorQueADuracao()
        {
            _engine.Iniciar(TimeSpan.FromMinutes(25), EstadoPomodoro.Foco);
            for (int i = 1; i <= 5; i++)
            {
                AvancarAte(TimeSpan.FromMinutes(25 - 5 * i), TimeSpan.FromMinutes(5));
                Assert.That(_stats.TempoEmFoco, Is.InRange(TimeSpan.Zero, TimeSpan.FromMinutes(25)));
            }
        }
    }

    [TestFixture]
    public class QaNordicCorrenteMaximaTests
    {
        [Test]
        public void ObterCorrenteMaxima_TodosOsValoresDoEnum_NaoDeveLancar()
        {
            foreach (EstadoCaboPP cabo in Enum.GetValues<EstadoCaboPP>())
                Assert.That(() => cabo.ObterCorrenteMaxima(), Throws.Nothing, cabo.ToString());
        }

        [Test]
        public void ObterCorrenteMaxima_DeveSerEstritamenteCrescenteNosCabosValidos()
        {
            var correntes = new[] { EstadoCaboPP.Cabo13A, EstadoCaboPP.Cabo20A, EstadoCaboPP.Cabo32A, EstadoCaboPP.Cabo63A }
                .Select(c => c.ObterCorrenteMaxima()!.Value).ToList();
            Assert.That(correntes, Is.Ordered.Ascending.And.Unique);
        }

        [Test]
        public void ObterCorrenteMaxima_ApenasQuatroEstadosTemCorrente()
        {
            int comCorrente = Enum.GetValues<EstadoCaboPP>().Count(c => c.ObterCorrenteMaxima().HasValue);
            Assert.That(comCorrente, Is.EqualTo(4));
        }
    }
}
