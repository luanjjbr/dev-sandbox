using DevSandbox.Core.Bancada;
using NUnit.Framework;

namespace DevSandbox.Tests
{
    [TestFixture]
    public class CorrenteMaximaCaboTests
    {
        [TestCase(EstadoCaboPP.Cabo13A, 13)]
        [TestCase(EstadoCaboPP.Cabo20A, 20)]
        [TestCase(EstadoCaboPP.Cabo32A, 32)]
        [TestCase(EstadoCaboPP.Cabo63A, 63)]
        public void ObterCorrenteMaxima_CaboValido_DeveSeguirATabelaIEC62196(EstadoCaboPP cabo, int amperes)
        {
            Assert.That(cabo.ObterCorrenteMaxima(), Is.EqualTo(amperes));
        }

        [TestCase(EstadoCaboPP.CircuitoAberto)]
        [TestCase(EstadoCaboPP.Desconectado)]
        [TestCase(EstadoCaboPP.FalhaCurto)]
        [TestCase((EstadoCaboPP)99)]
        public void ObterCorrenteMaxima_SemCaboValido_DeveSerNull(EstadoCaboPP cabo)
        {
            Assert.That(cabo.ObterCorrenteMaxima(), Is.Null);
        }

        [Test]
        public void ObterCorrenteMaxima_DeveSerCoerenteComAResistenciaNominal()
        {
            Assert.That(EstadoCaboPP.Cabo13A.ObterResistenciaNominal(), Is.EqualTo(1500));
            Assert.That(EstadoCaboPP.Cabo20A.ObterResistenciaNominal(), Is.EqualTo(680));
            Assert.That(EstadoCaboPP.Cabo32A.ObterResistenciaNominal(), Is.EqualTo(220));
            Assert.That(EstadoCaboPP.Cabo63A.ObterResistenciaNominal(), Is.EqualTo(100));
        }
    }
}
