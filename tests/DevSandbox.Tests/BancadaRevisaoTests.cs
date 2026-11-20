using System;
using System.Collections.Generic;
using System.Linq;
using DevSandbox.Core.Bancada;
using DevSandbox.Core.Serial;
using DevSandbox.Tests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace DevSandbox.Tests
{
    /// <summary>
    /// Correções da revisão da etapa 3c (Coruja): prazo por atividade e respostas órfãs, conclusão única na
    /// falha de escrita, ordem dos eventos e Id, Dispose, comando manual não ASCII.
    /// </summary>
    [TestFixture]
    public class BancadaRevisaoTests
    {
        private static readonly TimeSpan Prazo = BancadaEngine.TimeoutRespostaPadrao;

        private FakeSerialEngine _serial;
        private FakeTimeProvider _relogio;
        private BancadaEngine _engine;
        private List<ConclusaoComando> _concluidos;

        [SetUp]
        public void Setup()
        {
            _serial = new FakeSerialEngine { EstaConectado = true };
            _relogio = new FakeTimeProvider();
            _engine = new BancadaEngine(_serial, relogio: _relogio);
            _concluidos = [];
            _engine.ComandoConcluido += (s, c) => _concluidos.Add(c);
        }

        [TearDown]
        public void TearDown()
        {
            _engine.Dispose();
            _serial.Dispose();
        }

        // ---------- Item 1: prazo por atividade, padrão 4 s, respostas órfãs ----------

        [Test]
        public void PrazoPadrao_DeveSer4Segundos()
        {
            Assert.That(BancadaEngine.TimeoutRespostaPadrao, Is.EqualTo(TimeSpan.FromSeconds(4)));
        }

        [Test]
        public void QualquerLinhaRecebida_DeveRenovarOPrazoDoComandoDaVez()
        {
            _engine.DefinirEstado(EstadoVeiculoCP.BXNominal);

            _relogio.Advance(TimeSpan.FromSeconds(3));
            _serial.SimularRecepcao(" estado bx    -> BX nominal (9 V)\r\n"); // linha 'Outra' = atividade
            _relogio.Advance(TimeSpan.FromSeconds(3));
            Assert.That(_concluidos, Is.Empty, "A atividade da serial deveria ter renovado o prazo.");

            _relogio.Advance(TimeSpan.FromSeconds(1));
            Assert.That(_concluidos.Single().Resultado, Is.EqualTo(ResultadoComando.SemResposta));
        }

        [Test]
        public void ProximoDaFila_DeveContarOPrazoAPartirDaConclusaoDoAnterior()
        {
            _engine.DefinirCabo(EstadoCaboPP.Cabo13A);
            _engine.DefinirEstado(EstadoVeiculoCP.CXNominal);

            _relogio.Advance(TimeSpan.FromSeconds(3.5));
            _serial.SimularRecepcao("OK\r\n"); // conclui o cabo
            _relogio.Advance(TimeSpan.FromSeconds(3.5));
            Assert.That(_concluidos.Select(c => c.Resultado), Is.EqualTo(new[] { ResultadoComando.Confirmado }));

            _relogio.Advance(TimeSpan.FromSeconds(0.5));
            Assert.That(_concluidos.Select(c => c.Resultado),
                        Is.EqualTo(new[] { ResultadoComando.Confirmado, ResultadoComando.SemResposta }));
        }

        [Test]
        public void RespostaOrfa_DepoisDeSemResposta_NaoDeveDeslocarAFila()
        {
            _engine.DefinirEstado(EstadoVeiculoCP.AX);
            _relogio.Advance(Prazo);
            Assert.That(_concluidos.Single().Resultado, Is.EqualTo(ResultadoComando.SemResposta));

            _engine.DefinirEstado(EstadoVeiculoCP.E);
            _serial.SimularRecepcao("OK\r\n"); // atrasado: era do 'estado ax'
            Assert.That(_concluidos, Has.Count.EqualTo(1), "O OK órfão não pode concluir o 'estado e'.");

            _serial.SimularRecepcao("ERRO: Estado\r\n"); // este é do 'estado e'
            Assert.That(_concluidos[1].Comando, Is.EqualTo("estado e"));
            Assert.That(_concluidos[1].Resultado, Is.EqualTo(ResultadoComando.ErroFirmware));
        }

        [Test]
        public void RespostaOrfa_SoValeDentroDeMaisUmPrazo()
        {
            _engine.DefinirEstado(EstadoVeiculoCP.AX);
            _relogio.Advance(Prazo); // SemResposta
            _relogio.Advance(Prazo); // a órfã vence

            _engine.DefinirEstado(EstadoVeiculoCP.BXSuperior);
            _serial.SimularRecepcao("OK\r\n");

            Assert.That(_concluidos.Last().Comando, Is.EqualTo("estado bx+"));
            Assert.That(_concluidos.Last().Resultado, Is.EqualTo(ResultadoComando.Confirmado));
        }

        [Test]
        public void OrfaDeStatus_SoConsomeStatusOuErro()
        {
            _engine.SolicitarStatusCP();
            _relogio.Advance(Prazo); // 'estado status' sem resposta

            _engine.DefinirCabo(EstadoCaboPP.Cabo20A);
            _serial.SimularRecepcao("OK\r\n"); // OK não é o que o status devia: conclui o cabo

            Assert.That(_concluidos.Last().Comando, Is.EqualTo("cabo 3"));
            Assert.That(_concluidos.Last().Resultado, Is.EqualTo(ResultadoComando.Confirmado));
        }

        [Test]
        public void Desconectar_DeveLimparRespostasOrfas()
        {
            _engine.DefinirEstado(EstadoVeiculoCP.AX);
            _relogio.Advance(Prazo); // deixa uma órfã

            _engine.Desconectar();
            _serial.EstaConectado = true;
            _engine.DefinirEstado(EstadoVeiculoCP.DXNominal);
            _serial.SimularRecepcao("OK\r\n");

            Assert.That(_concluidos.Last().Comando, Is.EqualTo("estado dx"));
            Assert.That(_concluidos.Last().Resultado, Is.EqualTo(ResultadoComando.Confirmado));
        }

        // ---------- Item 2: conclusão única na falha de escrita com ConexaoPerdida síncrona ----------

        [Test]
        public void FalhaDeEscritaComPerdaSincrona_DeveConcluirSoNaoEnviado()
        {
            var serial = new SerialQuePerdeNoEnvio();
            using var engine = new BancadaEngine(serial, relogio: _relogio);
            var concluidos = new List<ConclusaoComando>();
            engine.ComandoConcluido += (s, c) => concluidos.Add(c);

            engine.DefinirCabo(EstadoCaboPP.Cabo32A); // transmitido, fica pendente
            serial.FalharProximoEnvio = true;
            engine.DefinirEstado(EstadoVeiculoCP.CXSuperior); // falha e dispara ConexaoPerdida dentro do EnviarDados

            ConclusaoComando[] doEstado = concluidos.Where(c => c.Comando == "estado cx+").ToArray();
            Assert.That(doEstado, Has.Length.EqualTo(1), "O comando que falhou deve ter um único desfecho.");
            Assert.That(doEstado[0].Resultado, Is.EqualTo(ResultadoComando.NaoEnviado));
            Assert.That(doEstado[0].Detalhe, Is.EqualTo(BancadaEngine.DetalheSemConexao));

            // O pendente anterior cai como SemResposta pela perda de conexão
            Assert.That(concluidos.Single(c => c.Comando == "cabo 4").Resultado, Is.EqualTo(ResultadoComando.SemResposta));
            Assert.That(engine.ComandosPendentes, Is.EqualTo(0));
            Assert.That(concluidos.Select(c => c.Id).Distinct().Count(), Is.EqualTo(concluidos.Count));
        }

        // ---------- Item 3: ordem dos eventos e Id ----------

        [Test]
        public void CaboAlteradoEEstadoVeiculoAlterado_DevemVirAntesDaTransmissao()
        {
            int transmitidosNoCabo = -1, transmitidosNoEstado = -1;
            _engine.CaboAlterado += (s, c) => transmitidosNoCabo = _serial.ComandosTransmitidos.Count;
            _engine.EstadoVeiculoAlterado += (s, e) => transmitidosNoEstado = _serial.ComandosTransmitidos.Count;

            _engine.DefinirCabo(EstadoCaboPP.Cabo63A);
            _engine.DefinirEstado(EstadoVeiculoCP.DXSuperior);

            Assert.That(transmitidosNoCabo, Is.EqualTo(0));
            Assert.That(transmitidosNoEstado, Is.EqualTo(1));
        }

        [Test]
        public void Id_DeveSerSequencialEBaterComUltimoIdComando()
        {
            Assert.That(_engine.UltimoIdComando, Is.EqualTo(0));

            _engine.DefinirCabo(EstadoCaboPP.Cabo13A);
            long idCabo = _engine.UltimoIdComando;
            _serial.EstaConectado = false;
            _engine.DefinirEstado(EstadoVeiculoCP.BXInferior); // NaoEnviado
            long idEstado = _engine.UltimoIdComando;
            _serial.EstaConectado = true;
            _serial.SimularRecepcao("OK\r\n");

            Assert.That(idCabo, Is.EqualTo(1));
            Assert.That(idEstado, Is.EqualTo(2));
            Assert.That(_concluidos.Single(c => c.Comando == "cabo 2").Id, Is.EqualTo(idCabo));
            Assert.That(_concluidos.Single(c => c.Comando == "estado bx-").Id, Is.EqualTo(idEstado));
        }

        [Test]
        public void ComandoConcluido_PodeChegarAntesDoComandoEnviado()
        {
            var serial = new SerialQueRespondeNoEnvio();
            using var engine = new BancadaEngine(serial, relogio: _relogio);
            var ordem = new List<string>();
            engine.ComandoConcluido += (s, c) => ordem.Add("concluido:" + c.Resultado);
            engine.ComandoEnviado += (s, c) => ordem.Add("enviado");

            Assert.That(engine.DefinirEstado(EstadoVeiculoCP.CXInferior), Is.True);

            Assert.That(ordem, Is.EqualTo(new[] { "concluido:Confirmado", "enviado" }));
            Assert.That(engine.EstadoConfirmado, Is.EqualTo(EstadoVeiculoCP.CXInferior));
        }

        // ---------- Item 4: Dispose ----------

        [Test]
        public void Dispose_ComPendentes_NaoDeveConcluirNemLancarDepois()
        {
            _engine.DefinirEstado(EstadoVeiculoCP.BXNominal);

            _engine.Dispose();
            Assert.DoesNotThrow(() => _relogio.Advance(Prazo * 3));
            Assert.DoesNotThrow(() => _engine.ProcessarDados("OK\r\n"));
            Assert.DoesNotThrow(() => _engine.Dispose());

            Assert.That(_concluidos, Is.Empty);
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(0));
            Assert.That(_engine.DefinirEstado(EstadoVeiculoCP.AX), Is.False);
        }

        // ---------- Item 5: comando manual com caractere fora do ASCII ----------

        [TestCase("estado ç")]
        [TestCase("cabo ①")]
        [TestCase("estado bx–")] // travessão (U+2013), não hífen
        [TestCase("help\u0001")]
        public void ComandoManual_ComCaractereInvalido_DeveSerRecusadoSemTransmitir(string comando)
        {
            int enviados = 0;
            _engine.ComandoEnviado += (s, c) => enviados++;

            Assert.That(BancadaEngine.ComandoManualValido(comando), Is.False);
            Assert.That(_engine.EnviarComandoManual(comando), Is.False);

            Assert.That(enviados, Is.EqualTo(0));
            Assert.That(_serial.ComandosTransmitidos, Is.Empty);
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(0));
            ConclusaoComando c = _concluidos.Single();
            Assert.That(c.Resultado, Is.EqualTo(ResultadoComando.NaoEnviado));
            Assert.That(c.Detalhe, Is.EqualTo(BancadaEngine.DetalheCaractereInvalido));
            Assert.That(c.Id, Is.EqualTo(_engine.UltimoIdComando));
        }

        [TestCase("cabo 4")]
        [TestCase("estado bx+")]
        [TestCase("ESTADO\tDX-")]
        [TestCase("help")]
        public void ComandoManualValido_AsciiImprimivel_DeveSerAceito(string comando)
        {
            Assert.That(BancadaEngine.ComandoManualValido(comando), Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ComandoManualValido_Vazio_DeveSerFalso(string? comando)
        {
            Assert.That(BancadaEngine.ComandoManualValido(comando), Is.False);
        }

        // ---------- Dublês específicos ----------

        /// <summary>Simula a perda de conexão síncrona: EnviarDados falha e dispara ConexaoPerdida antes de retornar.</summary>
        private sealed class SerialQuePerdeNoEnvio : ISerialEngine
        {
            public bool EstaConectado { get; private set; } = true;
            public bool TentandoReconectar { get; private set; }
            public bool FalharProximoEnvio { get; set; }

            public event EventHandler<string>? DadosRecebidos;
            public event EventHandler? ConexaoPerdida;
            public event EventHandler? Reconectado;

            public string[] ObterPortasDisponiveis() => [];
            public void Conectar(string nomePorta, int baudRate) => EstaConectado = true;
            public void Desconectar() => EstaConectado = false;

            public bool EnviarDados(string texto, FimDeLinha fimDeLinha)
            {
                if (!FalharProximoEnvio) return true;

                EstaConectado = false;
                TentandoReconectar = true;
                ConexaoPerdida?.Invoke(this, EventArgs.Empty); // como o SupervisorioSerialEngine na IOException
                return false;
            }

            public void Dispose()
            {
                _ = DadosRecebidos;
                _ = Reconectado;
            }
        }

        /// <summary>Simula o firmware respondendo "OK" antes de EnviarDados retornar (outra thread, mais rápida).</summary>
        private sealed class SerialQueRespondeNoEnvio : ISerialEngine
        {
            public bool EstaConectado => true;
            public bool TentandoReconectar => false;

            public event EventHandler<string>? DadosRecebidos;
            public event EventHandler? ConexaoPerdida;
            public event EventHandler? Reconectado;

            public string[] ObterPortasDisponiveis() => [];
            public void Conectar(string nomePorta, int baudRate) { }
            public void Desconectar() { }

            public bool EnviarDados(string texto, FimDeLinha fimDeLinha)
            {
                DadosRecebidos?.Invoke(this, "OK\r\n");
                return true;
            }

            public void Dispose()
            {
                _ = ConexaoPerdida;
                _ = Reconectado;
            }
        }
    }
}
