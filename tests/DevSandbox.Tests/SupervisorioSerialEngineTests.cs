using System;
using System.IO;
using DevSandbox.Core.Serial;
using DevSandbox.Tests.Fakes;
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
        public void EnviarDados_SemConexaoOuVazio_DeveRetornarFalseSemExcecao()
        {
            Assert.That(_engine.EnviarDados("TESTE"), Is.False);
            Assert.That(_engine.EnviarDados(""), Is.False);
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

    /// <summary>
    /// Testes do motor serial com porta e enumeração simuladas (sem hardware) e watchdog acionado manualmente.
    /// </summary>
    [TestFixture]
    public class SupervisorioSerialEngineSimuladoTests
    {
        private const string Porta = "COM7";

        private List<FakeSerialPort> _portasCriadas;
        private string[] _portasListadas;
        private SupervisorioSerialEngine _engine;
        private int _perdas;
        private int _reconexoes;

        private FakeSerialPort PortaAtual => _portasCriadas[^1];

        [SetUp]
        public void Setup()
        {
            _portasCriadas = [];
            _portasListadas = [Porta];
            _perdas = 0;
            _reconexoes = 0;

            _engine = new SupervisorioSerialEngine(
                (nome, baud) =>
                {
                    var porta = new FakeSerialPort(nome, baud);
                    _portasCriadas.Add(porta);
                    return porta;
                },
                () => _portasListadas,
                intervaloWatchdog: null);

            _engine.ConexaoPerdida += (s, e) => _perdas++;
            _engine.Reconectado += (s, e) => _reconexoes++;
        }

        [TearDown]
        public void TearDown()
        {
            _engine.Dispose();
        }

        [Test]
        public void Conectar_DeveAbrirPortaComParametrosInformados()
        {
            _engine.Conectar(Porta, 115200);

            Assert.That(_engine.EstaConectado, Is.True);
            Assert.That(PortaAtual.Nome, Is.EqualTo(Porta));
            Assert.That(PortaAtual.BaudRate, Is.EqualTo(115200));
        }

        [Test]
        public void EnviarDados_Conectado_DeveRetornarTrueEEscreverComOuSemNovaLinha()
        {
            _engine.Conectar(Porta, 9600);

            Assert.That(_engine.EnviarDados("cabo 1"), Is.True);
            Assert.That(_engine.EnviarDados("ab", adicionarNovaLinha: false), Is.True);
            Assert.That(PortaAtual.Escritos, Is.EqualTo(new[] { "cabo 1\n", "ab" }));
        }

        [TestCase(typeof(IOException))]
        [TestCase(typeof(InvalidOperationException))]
        [TestCase(typeof(TimeoutException))]
        public void EnviarDados_FalhaDeEscrita_DeveRetornarFalseDispararPerdaEEntrarEmReconexao(Type tipoExcecao)
        {
            _engine.Conectar(Porta, 9600);
            FakeSerialPort porta = PortaAtual;
            porta.FalhaNaEscrita = (Exception)Activator.CreateInstance(tipoExcecao)!;

            bool enviado = true;
            Assert.DoesNotThrow(() => enviado = _engine.EnviarDados("cabo 2"));

            Assert.That(enviado, Is.False);
            Assert.That(_perdas, Is.EqualTo(1));
            Assert.That(_engine.TentandoReconectar, Is.True);
            Assert.That(_engine.EstaConectado, Is.False);
            Assert.That(porta.Descartada, Is.True);
        }

        [Test]
        public void EnviarDados_AposFalha_NaoDeveDispararPerdaDeNovo()
        {
            _engine.Conectar(Porta, 9600);
            PortaAtual.FalhaNaEscrita = new IOException();

            _engine.EnviarDados("a");
            Assert.That(_engine.EnviarDados("b"), Is.False);

            Assert.That(_perdas, Is.EqualTo(1));
        }

        [Test]
        public void Watchdog_PortaRemovidaDaEnumeracao_DeveDetectarPerdaMesmoComIsOpenVerdadeiro()
        {
            _engine.Conectar(Porta, 9600);
            FakeSerialPort porta = PortaAtual;
            Assert.That(porta.IsOpen, Is.True);

            _portasListadas = [];
            _engine.ExecutarCicloWatchdog();

            Assert.That(_perdas, Is.EqualTo(1));
            Assert.That(_engine.TentandoReconectar, Is.True);
            Assert.That(porta.Descartada, Is.True);
            Assert.That(_reconexoes, Is.EqualTo(0), "Não deve reconectar enquanto a porta não reaparece.");
        }

        [Test]
        public void Watchdog_PortaFechadaPeloDriverMasAindaListada_DeveDetectarPerdaEReabrirNoMesmoCiclo()
        {
            _engine.Conectar(Porta, 9600);
            FakeSerialPort portaAntiga = PortaAtual;
            portaAntiga.IsOpen = false;

            _engine.ExecutarCicloWatchdog();

            Assert.That(_perdas, Is.EqualTo(1));
            Assert.That(_reconexoes, Is.EqualTo(1));
            Assert.That(portaAntiga.Descartada, Is.True);
            Assert.That(PortaAtual, Is.Not.SameAs(portaAntiga));
            Assert.That(_engine.EstaConectado, Is.True);
        }

        [Test]
        public void Watchdog_PortaReaparece_DeveReconectarEDispararReconectado()
        {
            _engine.Conectar(Porta, 9600);
            _portasListadas = [];
            _engine.ExecutarCicloWatchdog();

            _engine.ExecutarCicloWatchdog();
            Assert.That(_reconexoes, Is.EqualTo(0));

            _portasListadas = ["COM3", Porta.ToLowerInvariant()];
            _engine.ExecutarCicloWatchdog();

            Assert.That(_reconexoes, Is.EqualTo(1));
            Assert.That(_engine.EstaConectado, Is.True);
            Assert.That(_engine.TentandoReconectar, Is.False);
            Assert.That(_portasCriadas, Has.Count.EqualTo(2));
            Assert.That(_engine.EnviarDados("help"), Is.True);
        }

        [Test]
        public void Watchdog_FalhaAoReabrir_DeveManterReconexaoSemExcecao()
        {
            Exception? falhaAbertura = null;
            using var engine = new SupervisorioSerialEngine(
                (nome, baud) => new FakeSerialPort(nome, baud) { FalhaNaAbertura = falhaAbertura },
                () => _portasListadas,
                intervaloWatchdog: null);
            int reconexoes = 0;
            engine.Reconectado += (s, e) => reconexoes++;

            engine.Conectar(Porta, 9600);
            _portasListadas = [];
            engine.ExecutarCicloWatchdog();

            // A porta reaparece, mas ainda está ocupada por outro processo
            _portasListadas = [Porta];
            falhaAbertura = new UnauthorizedAccessException();
            Assert.DoesNotThrow(() => engine.ExecutarCicloWatchdog());
            Assert.That(engine.TentandoReconectar, Is.True);
            Assert.That(reconexoes, Is.EqualTo(0));

            falhaAbertura = null;
            engine.ExecutarCicloWatchdog();
            Assert.That(reconexoes, Is.EqualTo(1));
            Assert.That(engine.EstaConectado, Is.True);
        }

        [Test]
        public void Watchdog_PortaNuncaListada_NaoDeveDerrubarConexaoSaudavel()
        {
            // Drivers de portas virtuais que não aparecem em GetPortNames()
            _portasListadas = [];
            _engine.Conectar(Porta, 9600);

            _engine.ExecutarCicloWatchdog();

            Assert.That(_perdas, Is.EqualTo(0));
            Assert.That(_engine.EstaConectado, Is.True);
        }

        [Test]
        public void Watchdog_AposDesconectar_NaoDeveAgir()
        {
            _engine.Conectar(Porta, 9600);
            _engine.Desconectar();
            _portasListadas = [];

            _engine.ExecutarCicloWatchdog();

            Assert.That(_perdas, Is.EqualTo(0));
            Assert.That(_engine.TentandoReconectar, Is.False);
        }

        [Test]
        public void Recepcao_DeveRepassarDadosRecebidos()
        {
            string? recebido = null;
            _engine.DadosRecebidos += (s, dados) => recebido = dados;
            _engine.Conectar(Porta, 9600);

            PortaAtual.SimularRecepcao("OK\r\n");

            Assert.That(recebido, Is.EqualTo("OK\r\n"));
        }

        [Test]
        public void Desconectar_DeveDesassinarEDescartarPorta()
        {
            _engine.Conectar(Porta, 9600);
            FakeSerialPort porta = PortaAtual;

            _engine.Desconectar();

            Assert.That(porta.Descartada, Is.True);
            Assert.That(porta.AssinantesDataReceived, Is.EqualTo(0));
            Assert.That(_engine.EstaConectado, Is.False);
        }

        [Test]
        public void Conectar_AposDispose_DeveLancarObjectDisposedException()
        {
            _engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _engine.Conectar(Porta, 9600));
        }
    }
}
