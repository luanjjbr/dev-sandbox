using System.Linq;
using DevSandbox.Core.Serial;
using NUnit.Framework;

namespace DevSandbox.Tests
{
    [TestFixture]
    public class HistoricoComandosTests
    {
        private HistoricoComandos _historico;

        [SetUp]
        public void Setup() => _historico = new HistoricoComandos();

        private void Preencher(params string[] textos)
        {
            foreach (string t in textos) _historico.Adicionar(t);
        }

        [Test]
        public void Adicionar_DeveIgnorarVazioERepeticaoDaUltima()
        {
            Assert.That(_historico.Adicionar(""), Is.False);
            Assert.That(_historico.Adicionar("   "), Is.False);
            Assert.That(_historico.Adicionar(null), Is.False);
            Assert.That(_historico.Adicionar("help"), Is.True);
            Assert.That(_historico.Adicionar("help"), Is.False);
            Assert.That(_historico.Adicionar("cabo 1"), Is.True);
            Assert.That(_historico.Adicionar("help"), Is.True, "Repetição não consecutiva entra.");
            Assert.That(_historico.Adicionar("HELP"), Is.True, "A comparação diferencia maiúsculas.");

            Assert.That(_historico.Itens, Is.EqualTo(new[] { "help", "cabo 1", "help", "HELP" }));
        }

        [Test]
        public void Adicionar_AcimaDoLimite_DeveDescartarAMaisAntiga()
        {
            for (int i = 1; i <= HistoricoComandos.Limite + 5; i++) _historico.Adicionar($"cmd {i}");

            Assert.That(_historico.Quantidade, Is.EqualTo(HistoricoComandos.Limite));
            Assert.That(_historico.Itens[0], Is.EqualTo("cmd 6"));
            Assert.That(_historico.Itens[^1], Is.EqualTo($"cmd {HistoricoComandos.Limite + 5}"));
        }

        [Test]
        public void Anterior_DeveIrDaMaisRecenteAteAMaisAntigaEParar()
        {
            Preencher("a", "b", "c");

            Assert.That(_historico.Anterior("digitando"), Is.EqualTo("c"));
            Assert.That(_historico.Anterior("c"), Is.EqualTo("b"));
            Assert.That(_historico.Anterior("b"), Is.EqualTo("a"));
            Assert.That(_historico.Anterior("a"), Is.EqualTo("a"), "Para na mais antiga.");
            Assert.That(_historico.EstaNavegando, Is.True);
        }

        [Test]
        public void Proximo_AlemDoFim_DeveDevolverOTextoQueEstavaSendoDigitado()
        {
            Preencher("a", "b");

            _historico.Anterior("rascunho");      // b
            _historico.Anterior("b");             // a
            Assert.That(_historico.Proximo(), Is.EqualTo("b"));
            Assert.That(_historico.Proximo(), Is.EqualTo("rascunho"));
            Assert.That(_historico.EstaNavegando, Is.False);
            Assert.That(_historico.Proximo(), Is.Null, "Fora da navegação não muda o texto.");
        }

        [Test]
        public void Rascunho_SoEGuardadoNaPrimeiraSubida()
        {
            Preencher("a", "b");

            _historico.Anterior("rascunho");
            _historico.Anterior("texto editado pela seta"); // já navegando: não substitui o rascunho
            _historico.Proximo();
            Assert.That(_historico.Proximo(), Is.EqualTo("rascunho"));
        }

        [Test]
        public void HistoricoVazio_AnteriorDevolveOTextoAtualEProximoNull()
        {
            Assert.That(_historico.Anterior("x"), Is.EqualTo("x"));
            Assert.That(_historico.Anterior(null), Is.EqualTo(""));
            Assert.That(_historico.EstaNavegando, Is.False);
            Assert.That(_historico.Proximo(), Is.Null);
        }

        [Test]
        public void ResetarNavegacao_DeveVoltarAoFimEDescartarORascunho()
        {
            Preencher("a", "b");
            _historico.Anterior("rascunho");
            _historico.Anterior("b");

            _historico.ResetarNavegacao();

            Assert.That(_historico.EstaNavegando, Is.False);
            Assert.That(_historico.Proximo(), Is.Null);
            Assert.That(_historico.Anterior("novo"), Is.EqualTo("b"), "Recomeça da mais recente.");
            Assert.That(_historico.Proximo(), Is.EqualTo("novo"));
        }

        [Test]
        public void Adicionar_DuranteNavegacao_DeveEncerrarANavegacao()
        {
            Preencher("a", "b");
            _historico.Anterior("rascunho");

            _historico.Adicionar("b"); // repetido: não entra, mas encerra a navegação

            Assert.That(_historico.EstaNavegando, Is.False);
            Assert.That(_historico.Anterior(""), Is.EqualTo("b"));
            Assert.That(_historico.Itens.Count(i => i == "b"), Is.EqualTo(1));
        }
    }
}
