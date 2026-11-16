using System;
using DevSandbox.Core.Serial;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace DevSandbox.Tests
{
    /// <summary>
    /// Testes do montador de linhas da serial com relógio virtual (<see cref="FakeTimeProvider"/>).
    /// </summary>
    [TestFixture]
    public class MontadorLinhasSerialTests
    {
        private FakeTimeProvider _relogio;
        private MontadorLinhasSerial _montador;

        [SetUp]
        public void Setup()
        {
            _relogio = new FakeTimeProvider();
            _montador = new MontadorLinhasSerial(_relogio);
        }

        [Test]
        public void Adicionar_MensagemPicadaEmVariosPedacos_ViraUmaLinha()
        {
            Assert.That(_montador.Adicionar("Coma"), Is.Empty);
            Assert.That(_montador.Adicionar("ndos"), Is.Empty);
            Assert.That(_montador.Adicionar(":\r"), Is.Empty);

            var linhas = _montador.Adicionar("\n");

            Assert.That(linhas, Is.EqualTo(new[] { "Comandos:" }));
            Assert.That(_montador.Descarregar(), Is.Null);
        }

        [Test]
        public void Adicionar_CrLf_RemoveCrFinal()
        {
            Assert.That(_montador.Adicionar("OK\r\n"), Is.EqualTo(new[] { "OK" }));
        }

        [Test]
        public void Adicionar_SoLf_DevolveLinha()
        {
            Assert.That(_montador.Adicionar("OK\n"), Is.EqualTo(new[] { "OK" }));
        }

        [Test]
        public void Adicionar_CrNoMeioDaLinha_EhPreservado()
        {
            Assert.That(_montador.Adicionar("a\rb\r\n"), Is.EqualTo(new[] { "a\rb" }));
        }

        [Test]
        public void Adicionar_VariasLinhasNumPedaco_DevolveTodasEGuardaResto()
        {
            var linhas = _montador.Adicionar("Comandos:\r\n  help\r\n  estado\r\n  cab");

            Assert.That(linhas, Is.EqualTo(new[] { "Comandos:", "  help", "  estado" }));
            Assert.That(_montador.Descarregar(), Is.EqualTo("  cab"));
        }

        [Test]
        public void Adicionar_LinhaVazia_EhDevolvida()
        {
            Assert.That(_montador.Adicionar("\r\n"), Is.EqualTo(new[] { "" }));
            Assert.That(_montador.Adicionar("a\n\nb\n"), Is.EqualTo(new[] { "a", "", "b" }));
        }

        [Test]
        public void Adicionar_TextoVazio_NaoDevolveNada()
        {
            Assert.That(_montador.Adicionar(""), Is.Empty);
            Assert.That(_montador.Descarregar(), Is.Null);
        }

        [Test]
        public void ColetarPendenteSeOcioso_SemPendente_DevolveNull()
        {
            _relogio.Advance(TimeSpan.FromSeconds(1));
            Assert.That(_montador.ColetarPendenteSeOcioso(), Is.Null);
        }

        [Test]
        public void ColetarPendenteSeOcioso_SoEntregaApos100Ms()
        {
            _montador.Adicionar("> ");

            _relogio.Advance(TimeSpan.FromMilliseconds(99));
            Assert.That(_montador.ColetarPendenteSeOcioso(), Is.Null);

            _relogio.Advance(TimeSpan.FromMilliseconds(1));
            Assert.That(_montador.ColetarPendenteSeOcioso(), Is.EqualTo("> "));

            // Já foi limpo
            _relogio.Advance(TimeSpan.FromMilliseconds(200));
            Assert.That(_montador.ColetarPendenteSeOcioso(), Is.Null);
        }

        [Test]
        public void ColetarPendenteSeOcioso_ByteNovoReiniciaContagem()
        {
            _montador.Adicionar("ab");
            _relogio.Advance(TimeSpan.FromMilliseconds(80));
            _montador.Adicionar("c");

            _relogio.Advance(TimeSpan.FromMilliseconds(80));
            Assert.That(_montador.ColetarPendenteSeOcioso(), Is.Null, "160 ms do 1º byte, mas só 80 ms do último");

            _relogio.Advance(TimeSpan.FromMilliseconds(20));
            Assert.That(_montador.ColetarPendenteSeOcioso(), Is.EqualTo("abc"));
        }

        [Test]
        public void ColetaOciosaTerminadaEmCr_LfAtrasadoNaoGeraLinhaVazia()
        {
            _montador.Adicionar("abc\r");
            _relogio.Advance(MontadorLinhasSerial.TempoOciosoParaEntrega);
            Assert.That(_montador.ColetarPendenteSeOcioso(), Is.EqualTo("abc"));

            Assert.That(_montador.Adicionar("\n"), Is.Empty);
            Assert.That(_montador.Descarregar(), Is.Null);

            // Depois do \n engolido, tudo volta ao normal
            Assert.That(_montador.Adicionar("\n"), Is.EqualTo(new[] { "" }));
        }

        [Test]
        public void ColetaOciosaSemCr_LfAtrasadoContinuaGerandoLinhaVazia()
        {
            _montador.Adicionar("abc");
            _relogio.Advance(MontadorLinhasSerial.TempoOciosoParaEntrega);
            Assert.That(_montador.ColetarPendenteSeOcioso(), Is.EqualTo("abc"));

            Assert.That(_montador.Adicionar("\n"), Is.EqualTo(new[] { "" }));
        }

        [Test]
        public void ColetaOciosaTerminadaEmCr_OutroCaractereZeraFlag()
        {
            _montador.Adicionar("abc\r");
            _relogio.Advance(MontadorLinhasSerial.TempoOciosoParaEntrega);
            Assert.That(_montador.ColetarPendenteSeOcioso(), Is.EqualTo("abc"));

            Assert.That(_montador.Adicionar("x\n\n"), Is.EqualTo(new[] { "x", "" }));
        }

        [Test]
        public void DescargaTerminadaEmCr_LfAtrasadoNaoGeraLinhaVazia()
        {
            _montador.Adicionar("abc\r");
            Assert.That(_montador.Descarregar(), Is.EqualTo("abc"));

            Assert.That(_montador.Adicionar("\nOK\r\n"), Is.EqualTo(new[] { "OK" }));
        }

        [Test]
        public void Limpar_ZeraFlagDoCr()
        {
            _montador.Adicionar("abc\r");
            Assert.That(_montador.Descarregar(), Is.EqualTo("abc"));
            _montador.Limpar();

            Assert.That(_montador.Adicionar("\n"), Is.EqualTo(new[] { "" }));
        }

        [Test]
        public void Descarregar_DevolvePendenteNaHoraELimpa()
        {
            _montador.Adicionar("parcial");

            Assert.That(_montador.Descarregar(), Is.EqualTo("parcial"));
            Assert.That(_montador.Descarregar(), Is.Null);
        }

        [Test]
        public void Descarregar_SemPendente_DevolveNull()
        {
            Assert.That(_montador.Descarregar(), Is.Null);
        }

        [Test]
        public void Limpar_DescartaPendente()
        {
            _montador.Adicionar("lixo");
            _montador.Limpar();

            Assert.That(_montador.Descarregar(), Is.Null);
            Assert.That(_montador.Adicionar("OK\r\n"), Is.EqualTo(new[] { "OK" }));
        }

        [Test]
        public void Adicionar_LinhaAteOLimite_NaoEhCortada()
        {
            string limite = new('x', MontadorLinhasSerial.TamanhoMaximoLinha);

            Assert.That(_montador.Adicionar(limite), Is.Empty);
            Assert.That(_montador.Adicionar("\n"), Is.EqualTo(new[] { limite }));
        }

        [Test]
        public void Adicionar_LinhaAcimaDoLimite_EhEntregueSemLf()
        {
            string limite = new('x', MontadorLinhasSerial.TamanhoMaximoLinha);

            var linhas = _montador.Adicionar(limite + "yz");

            Assert.That(linhas, Is.EqualTo(new[] { limite }));
            Assert.That(_montador.Descarregar(), Is.EqualTo("yz"));
        }

        [Test]
        public void Adicionar_LimiteSeguidoDeCrLf_DevolveUmaLinhaSemLinhaVazia()
        {
            string limite = new('x', MontadorLinhasSerial.TamanhoMaximoLinha);

            Assert.That(_montador.Adicionar(limite + "\r\n"), Is.EqualTo(new[] { limite }));
            Assert.That(_montador.Descarregar(), Is.Null);
        }

        [Test]
        public void Adicionar_LimiteSeguidoDeCrLfEmPedacos_DevolveUmaLinhaSemLinhaVazia()
        {
            string limite = new('x', MontadorLinhasSerial.TamanhoMaximoLinha);

            Assert.That(_montador.Adicionar(limite), Is.Empty);
            Assert.That(_montador.Adicionar("\r"), Is.Empty);
            Assert.That(_montador.Adicionar("\n"), Is.EqualTo(new[] { limite }));
            Assert.That(_montador.Descarregar(), Is.Null);
        }

        [Test]
        public void Adicionar_Nulo_NaoDevolveNada()
        {
            Assert.That(_montador.Adicionar(null!), Is.Empty);
            Assert.That(_montador.Descarregar(), Is.Null);
        }

        [Test]
        public void Adicionar_LinhaAcimaDoLimiteEmPedacos_EhEntregue()
        {
            string metade = new('x', MontadorLinhasSerial.TamanhoMaximoLinha / 2);

            Assert.That(_montador.Adicionar(metade), Is.Empty);
            Assert.That(_montador.Adicionar(metade), Is.Empty);

            var linhas = _montador.Adicionar("z\n");

            Assert.That(linhas, Is.EqualTo(new[] { metade + metade, "z" }));
        }

        [Test]
        public void Construtor_RelogioNulo_LancaExcecao()
        {
            Assert.Throws<ArgumentNullException>(() => _ = new MontadorLinhasSerial(null!));
        }

        [Test]
        public void ConstrutorPadrao_UsaRelogioDoSistema()
        {
            var montador = new MontadorLinhasSerial();
            Assert.That(montador.Adicionar("a\n"), Is.EqualTo(new[] { "a" }));
        }
    }
}
