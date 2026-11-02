using System;
using System.Collections.Generic;
using System.Linq;
using DevSandbox.Core.Bancada;
using DevSandbox.Tests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace DevSandbox.Tests
{
    /// <summary>
    /// Bancada v2 (Core): relés do [STATUS CP], cabo status, respostas multilinha (help / debug stack) e reenvio.
    /// Literais copiados de firmware/Bancada/src/main.cpp e lib/EmuladorVeiculoCP/EmuladorVeiculoCP.cpp.
    /// </summary>
    [TestFixture]
    public class RelesCPTests
    {
        [TestCase(30, "PE")]
        [TestCase(31, "falha")]
        [TestCase(29, "B-hab")]
        [TestCase(28, "B-sel")]
        [TestCase(27, "CD-hab")]
        [TestCase(26, "CD-sel")]
        [TestCase(25, "C-hab")]
        [TestCase(24, "C-sel")]
        [TestCase(23, "D-hab")]
        [TestCase(22, "D-sel")]
        public void ObterNome_DeveSeguirOConfigHardware(int pino, string nome)
        {
            Assert.That(RelesCP.ObterNome(pino), Is.EqualTo(nome));
            Assert.That(RelesCP.EhPinoDeRele(pino), Is.True);
        }

        [Test]
        public void NomesPorPino_DeveTerOs10PinosDe22a31()
        {
            Assert.That(RelesCP.NomesPorPino.Keys, Is.EquivalentTo(Enumerable.Range(22, 10)));
            Assert.That(RelesCP.ObterNome(21), Is.Null);
            Assert.That(RelesCP.ObterNome(32), Is.Null);
        }

        [Test]
        public void TentarInterpretar_ListaDePinos_DeveManterAOrdemDoFirmware()
        {
            Assert.That(RelesCP.TentarInterpretar("D30 D29 D28", out var pinos), Is.True);
            Assert.That(pinos, Is.EqualTo(new[] { 30, 29, 28 }));
        }

        [Test]
        public void TentarInterpretar_Nenhum_DeveDarListaVazia()
        {
            Assert.That(RelesCP.TentarInterpretar("nenhum", out var pinos), Is.True);
            Assert.That(pinos, Is.Empty);
        }

        [TestCase("")]
        [TestCase("S7")]
        [TestCase("D21")]
        [TestCase("D32")]
        [TestCase("D30 D30")]
        [TestCase("D30 X")]
        [TestCase("D-1")]
        [TestCase("D")]
        public void TentarInterpretar_Invalido_DeveFalhar(string campo)
        {
            Assert.That(RelesCP.TentarInterpretar(campo, out _), Is.False);
        }
    }

    [TestFixture]
    public class RespostaFirmwareV2Tests
    {
        [Test]
        public void StatusCP_ComReles_DeveLerOsPinos()
        {
            var r = RespostaFirmware.Interpretar("[STATUS CP] Estado: BX | Vcp: 9 V | Conectado, nominal | Reles: D30 D29");

            Assert.That(r.Tipo, Is.EqualTo(TipoRespostaFirmware.StatusCP));
            Assert.That(r.EstadoCP, Is.EqualTo(EstadoVeiculoCP.BXNominal));
            Assert.That(r.RelesAtivos, Is.EqualTo(new[] { 30, 29 }));
        }

        [Test]
        public void StatusCP_ComRelesNenhum_DeveDarListaVazia()
        {
            var r = RespostaFirmware.Interpretar("[STATUS CP] Estado: AX | Vcp: 12 V | Veiculo nao conectado | Reles: nenhum");

            Assert.That(r.EstadoCP, Is.EqualTo(EstadoVeiculoCP.AX));
            Assert.That(r.RelesAtivos, Is.Not.Null.And.Empty);
        }

        [Test]
        public void StatusCP_SemCampoReles_DeveDarNulo()
        {
            var r = RespostaFirmware.Interpretar("[STATUS CP] Estado: BX | Vcp: 9 V | Conectado, nominal");

            Assert.That(r.EstadoCP, Is.EqualTo(EstadoVeiculoCP.BXNominal));
            Assert.That(r.RelesAtivos, Is.Null);
        }

        [Test]
        public void StatusCP_ComRelesInvalido_DeveDarNuloSemPerderOEstado()
        {
            var r = RespostaFirmware.Interpretar("[STATUS CP] Estado: E | Vcp: 1 V | Falha / curto CP-PE | Reles: S7");

            Assert.That(r.EstadoCP, Is.EqualTo(EstadoVeiculoCP.E));
            Assert.That(r.RelesAtivos, Is.Null);
        }

        [Test]
        public void OutrasLinhas_NaoDevemTerReles()
        {
            Assert.That(RespostaFirmware.Interpretar("OK").RelesAtivos, Is.Null);
            Assert.That(RespostaFirmware.Interpretar(" Reles (placa EasyEDA): D30 = PE (S7); D31 = falha (S8)").RelesAtivos, Is.Null);
        }

        [Test]
        public void Stack_DeveSerInterpretado()
        {
            var r = RespostaFirmware.Interpretar("[STACK] SerialCLI livre: 87 de 256 | Blink livre: 41 de 128");

            Assert.That(r.Tipo, Is.EqualTo(TipoRespostaFirmware.Outra), "Continua 'Outra' (sem OK, não conclui comando).");
            Assert.That(r.Stack, Is.EqualTo(new StackTasks(87, 256, 41, 128)));
        }

        [TestCase("[STACK] SerialCLI livre: x de 256 | Blink livre: 41 de 128")]
        [TestCase("[STACK] SerialCLI livre: 87 de 256")]
        [TestCase("STACK SerialCLI livre: 87 de 256 | Blink livre: 41 de 128")]
        public void Stack_ForaDoFormato_DeveDarNulo(string linha)
        {
            Assert.That(RespostaFirmware.Interpretar(linha).Stack, Is.Null);
            Assert.That(StackTasks.TentarInterpretar(linha), Is.Null);
        }
    }

    [TestFixture]
    public class BancadaEngineV2Tests
    {
        private static readonly TimeSpan Janela = BancadaEngine.JanelaSilencioMultilinhaPadrao;

        private FakeSerialEngine _serial;
        private FakeTimeProvider _relogio;
        private BancadaEngine _engine;
        private List<ConclusaoComando> _concluidos;
        private List<RespostaMultilinha> _multilinhas;
        private List<IReadOnlyList<int>> _reles;

        [SetUp]
        public void Setup()
        {
            _serial = new FakeSerialEngine { EstaConectado = true };
            _relogio = new FakeTimeProvider();
            _engine = new BancadaEngine(_serial, relogio: _relogio);
            _concluidos = [];
            _multilinhas = [];
            _reles = [];
            _engine.ComandoConcluido += (s, c) => _concluidos.Add(c);
            _engine.RespostaMultilinhaRecebida += (s, r) => _multilinhas.Add(r);
            _engine.RelesCPAtualizados += (s, r) => _reles.Add(r);
        }

        [TearDown]
        public void TearDown()
        {
            _engine.Dispose();
            _serial.Dispose();
        }

        // ---------- 1. Relés do [STATUS CP] ----------

        [Test]
        public void StatusCP_ComReles_DeveAtualizarRelesConfirmadosEDispararEvento()
        {
            Assert.That(_engine.RelesConfirmados, Is.Null);
            _engine.SolicitarStatusCP();

            _serial.SimularRecepcao("[STATUS CP] Estado: CX | Vcp: 6 V | Carga sem ventilacao, nominal | Reles: D30 D29 D27 D25\r\n");

            Assert.That(_concluidos.Single().Resultado, Is.EqualTo(ResultadoComando.Confirmado));
            Assert.That(_engine.EstadoConfirmado, Is.EqualTo(EstadoVeiculoCP.CXNominal));
            Assert.That(_engine.RelesConfirmados, Is.EqualTo(new[] { 30, 29, 27, 25 }));
            Assert.That(_reles.Single(), Is.EqualTo(new[] { 30, 29, 27, 25 }));
            Assert.That(_engine.RelesConfirmados!.Select(p => RelesCP.ObterNome(p)),
                        Is.EqualTo(new[] { "PE", "B-hab", "CD-hab", "C-hab" }));
        }

        [Test]
        public void StatusCP_RelesNenhum_DeveZerarRelesConfirmados()
        {
            _engine.SolicitarStatusCP();
            _serial.SimularRecepcao("[STATUS CP] Estado: BX | Vcp: 9 V | Conectado, nominal | Reles: D30 D29\r\n");
            _engine.SolicitarStatusCP();
            _serial.SimularRecepcao("[STATUS CP] Estado: AX | Vcp: 12 V | Veiculo nao conectado | Reles: nenhum\r\n");

            Assert.That(_engine.RelesConfirmados, Is.Not.Null.And.Empty);
            Assert.That(_reles, Has.Count.EqualTo(2));
            Assert.That(_reles[1], Is.Empty);
        }

        [Test]
        public void StatusCP_SemCampoReles_NaoDeveDispararEventoDeReles()
        {
            _engine.SolicitarStatusCP();
            _serial.SimularRecepcao("[STATUS CP] Estado: BX | Vcp: 9 V | Conectado, nominal\r\n");

            Assert.That(_engine.EstadoConfirmado, Is.EqualTo(EstadoVeiculoCP.BXNominal));
            Assert.That(_engine.RelesConfirmados, Is.Null);
            Assert.That(_reles, Is.Empty);
        }

        [Test]
        public void OkDeEstado_NaoDeveMudarRelesConfirmados()
        {
            _engine.SolicitarStatusCP();
            _serial.SimularRecepcao("[STATUS CP] Estado: BX | Vcp: 9 V | Conectado, nominal | Reles: D30 D29\r\n");

            _engine.DefinirEstado(EstadoVeiculoCP.AX);
            _serial.SimularRecepcao("OK\r\n");

            Assert.That(_engine.RelesConfirmados, Is.EqualTo(new[] { 30, 29 }));
            Assert.That(_reles, Has.Count.EqualTo(1));
        }

        // ---------- 2. cabo status ----------

        [Test]
        public void SolicitarStatusCabo_DeveEnviarEConcluirComStatusCabo()
        {
            Assert.That(_engine.SolicitarStatusCabo(), Is.True);
            Assert.That(_serial.ComandosTransmitidos, Is.EqualTo(new[] { "cabo status" }));
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(1));

            _serial.SimularRecepcao("[STATUS CABO] Canal: 4 | R: 220 Ohms | Imax: 32 A | Descricao: Cabo 32 A\r\n");

            ConclusaoComando c = _concluidos.Single();
            Assert.That(c.Comando, Is.EqualTo("cabo status"));
            Assert.That(c.Resultado, Is.EqualTo(ResultadoComando.Confirmado));
            Assert.That(c.Resposta!.CaboPP, Is.EqualTo(EstadoCaboPP.Cabo32A));
            Assert.That(_engine.CaboConfirmado, Is.EqualTo(EstadoCaboPP.Cabo32A));
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(0));
        }

        [Test]
        public void SolicitarStatusCabo_OkNaoDeveConcluir_SoOStatusCabo()
        {
            _engine.SolicitarStatusCabo();
            _serial.SimularRecepcao("OK\r\n");
            Assert.That(_concluidos, Is.Empty);

            _serial.SimularRecepcao("[STATUS CABO] Canal: 0 | R: -1 Ohms | Imax: 0 A | Descricao: Circuito Aberto\r\n");
            Assert.That(_concluidos.Single().Resultado, Is.EqualTo(ResultadoComando.Confirmado));
        }

        [Test]
        public void SolicitarStatusCabo_SemResposta_DeveExpirar()
        {
            _engine.SolicitarStatusCabo();
            _relogio.Advance(BancadaEngine.TimeoutRespostaPadrao);

            Assert.That(_concluidos.Single().Resultado, Is.EqualTo(ResultadoComando.SemResposta));
        }

        [Test]
        public void SolicitarStatusCabo_Desconectado_DeveConcluirNaoEnviado()
        {
            _serial.EstaConectado = false;

            Assert.That(_engine.SolicitarStatusCabo(), Is.False);
            Assert.That(_concluidos.Single().Resultado, Is.EqualTo(ResultadoComando.NaoEnviado));
        }

        // ---------- 3. Respostas multilinha ----------

        [Test]
        public void SolicitarDebugStack_DeveEntregarAposAJanelaDeSilencio()
        {
            Assert.That(_engine.SolicitarDebugStack(), Is.True);
            Assert.That(_serial.ComandosTransmitidos, Is.EqualTo(new[] { "debug stack" }));
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(0), "Não entra na FIFO.");

            _serial.SimularRecepcao("[STACK] SerialCLI livre: 87 de 256 | Blink livre: 41 de 128\r\n");
            _relogio.Advance(Janela - TimeSpan.FromMilliseconds(1));
            Assert.That(_multilinhas, Is.Empty);

            _relogio.Advance(TimeSpan.FromMilliseconds(1));

            RespostaMultilinha r = _multilinhas.Single();
            Assert.That(r.Comando, Is.EqualTo("debug stack"));
            Assert.That(r.Linhas, Is.EqualTo(new[] { "[STACK] SerialCLI livre: 87 de 256 | Blink livre: 41 de 128" }));
            Assert.That(r.Stack, Is.EqualTo(new StackTasks(87, 256, 41, 128)));
            Assert.That(_concluidos, Is.Empty, "Comando sem OK não gera ComandoConcluido.");
        }

        [Test]
        public void SolicitarHelp_LinhasEspacadas_DevemRenovarAJanelaEVirJuntas()
        {
            _engine.SolicitarHelp();
            Assert.That(_serial.ComandosTransmitidos, Is.EqualTo(new[] { "help" }));

            _serial.SimularRecepcao("\r\n==================================================\r\n");
            _relogio.Advance(TimeSpan.FromMilliseconds(200));
            _serial.SimularRecepcao(" cabo 0      -> Circuito Aberto (Desliga todas as chaves)\r\n");
            _relogio.Advance(TimeSpan.FromMilliseconds(200));
            _serial.SimularRecepcao(" help        -> Exibe este menu explicativo\r\n=====");
            _relogio.Advance(TimeSpan.FromMilliseconds(200));
            _serial.SimularRecepcao("=============================================\r\n");
            Assert.That(_multilinhas, Is.Empty, "Ainda dentro da janela.");

            _relogio.Advance(Janela);

            RespostaMultilinha r = _multilinhas.Single();
            Assert.That(r.Comando, Is.EqualTo("help"));
            Assert.That(r.Linhas, Is.EqualTo(new[]
            {
                "==================================================",
                "cabo 0      -> Circuito Aberto (Desliga todas as chaves)",
                "help        -> Exibe este menu explicativo",
                "=================================================="
            }));
            Assert.That(r.Stack, Is.Null);
        }

        [Test]
        public void JanelaConfiguravel_DeveSerRespeitada()
        {
            using var engine = new BancadaEngine(_serial, descartarSerial: false, relogio: _relogio,
                                                 janelaSilencioMultilinha: TimeSpan.FromMilliseconds(50));
            var recebidas = new List<RespostaMultilinha>();
            engine.RespostaMultilinhaRecebida += (s, r) => recebidas.Add(r);

            engine.SolicitarHelp();
            _serial.SimularRecepcao("linha\r\n");
            _relogio.Advance(TimeSpan.FromMilliseconds(50));

            Assert.That(recebidas.Single().Linhas, Is.EqualTo(new[] { "linha" }));
        }

        [Test]
        public void JanelaInvalida_DeveLancar()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _ = new BancadaEngine(_serial, descartarSerial: false, relogio: _relogio, janelaSilencioMultilinha: TimeSpan.Zero));
        }

        [Test]
        public void Multilinha_SemNenhumaLinha_DeveEntregarVaziaNoPrazoDeResposta()
        {
            _engine.SolicitarDebugStack();

            _relogio.Advance(Janela);
            Assert.That(_multilinhas, Is.Empty, "Sem linhas, a janela de silêncio não encerra (o firmware pode estar ocupado).");

            _relogio.Advance(BancadaEngine.TimeoutRespostaPadrao - Janela);

            RespostaMultilinha r = _multilinhas.Single();
            Assert.That(r.Comando, Is.EqualTo("debug stack"));
            Assert.That(r.Linhas, Is.Empty);
            Assert.That(r.Stack, Is.Null);
        }

        [TestCase("?")]
        [TestCase("HELP")]
        [TestCase("debug stack")]
        public void ComandoManualMultilinha_DeveSerAgrupadoTambem(string comando)
        {
            _engine.EnviarComandoManual(comando);
            _serial.SimularRecepcao("qualquer linha\r\n");
            _relogio.Advance(Janela);

            Assert.That(_multilinhas.Single().Comando, Is.EqualTo(comando));
            Assert.That(_multilinhas.Single().Linhas, Is.EqualTo(new[] { "qualquer linha" }));
        }

        [Test]
        public void Multilinha_NaoDeveAtrapalharComandoAnteriorNaFifo()
        {
            // O firmware responde em ordem: o OK do "cabo 4" vem antes do menu
            _engine.DefinirCabo(EstadoCaboPP.Cabo32A);
            _engine.SolicitarHelp();

            _serial.SimularRecepcao("OK\r\n==================================================\r\n help        -> Exibe este menu explicativo\r\n");
            _relogio.Advance(Janela);

            ConclusaoComando c = _concluidos.Single();
            Assert.That(c.Comando, Is.EqualTo("cabo 4"));
            Assert.That(c.Resultado, Is.EqualTo(ResultadoComando.Confirmado));
            Assert.That(_multilinhas.Single().Linhas, Is.EqualTo(new[]
            {
                "==================================================",
                "help        -> Exibe este menu explicativo"
            }), "O OK não entra na resposta multilinha.");
        }

        [Test]
        public void Multilinha_NaoDeveAtrapalharComandoPosteriorNaFifo()
        {
            _engine.SolicitarDebugStack();
            _engine.DefinirEstado(EstadoVeiculoCP.CXNominal);

            _serial.SimularRecepcao("[STACK] SerialCLI livre: 87 de 256 | Blink livre: 41 de 128\r\nOK\r\n");

            Assert.That(_concluidos.Single().Comando, Is.EqualTo("estado cx"));
            Assert.That(_concluidos.Single().Resultado, Is.EqualTo(ResultadoComando.Confirmado));

            _relogio.Advance(Janela);
            Assert.That(_multilinhas.Single().Linhas, Has.Count.EqualTo(1));
            Assert.That(_multilinhas.Single().Stack, Is.Not.Null);
        }

        [Test]
        public void Multilinha_EmSequencia_DevemSerEntreguesEmOrdem()
        {
            _engine.SolicitarDebugStack();
            _engine.SolicitarHelp();

            _serial.SimularRecepcao("[STACK] SerialCLI livre: 87 de 256 | Blink livre: 41 de 128\r\n");
            _relogio.Advance(Janela);
            Assert.That(_multilinhas.Single().Comando, Is.EqualTo("debug stack"));

            _serial.SimularRecepcao("menu\r\n");
            _relogio.Advance(Janela);

            Assert.That(_multilinhas.Select(m => m.Comando), Is.EqualTo(new[] { "debug stack", "help" }));
            Assert.That(_multilinhas[1].Linhas, Is.EqualTo(new[] { "menu" }));
        }

        [Test]
        public void Multilinha_NaoEnviada_DeveConcluirNaoEnviadoSemEventoMultilinha()
        {
            _serial.EstaConectado = false;

            Assert.That(_engine.SolicitarHelp(), Is.False);
            _relogio.Advance(BancadaEngine.TimeoutRespostaPadrao);

            Assert.That(_concluidos.Single().Resultado, Is.EqualTo(ResultadoComando.NaoEnviado));
            Assert.That(_multilinhas, Is.Empty);
        }

        [Test]
        public void Multilinha_ConexaoPerdida_DeveEntregarOQueChegou()
        {
            _engine.SolicitarHelp();
            _serial.SimularRecepcao("linha 1\r\n");

            _serial.SimularDesconexao();

            Assert.That(_multilinhas.Single().Linhas, Is.EqualTo(new[] { "linha 1" }));
            _relogio.Advance(BancadaEngine.TimeoutRespostaPadrao);
            Assert.That(_multilinhas, Has.Count.EqualTo(1), "Não entrega de novo.");
        }

        [Test]
        public void Multilinha_ConexaoPerdidaSemLinhas_DeveDescartarSemEvento()
        {
            _engine.SolicitarHelp();

            _serial.SimularDesconexao();
            _relogio.Advance(BancadaEngine.TimeoutRespostaPadrao);

            Assert.That(_multilinhas, Is.Empty);
        }

        [Test]
        public void Multilinha_AposDispose_NaoDeveDispararEvento()
        {
            _engine.SolicitarHelp();
            _serial.SimularRecepcao("linha\r\n");

            _engine.Dispose();
            _relogio.Advance(BancadaEngine.TimeoutRespostaPadrao);

            Assert.That(_multilinhas, Is.Empty);
        }

        // ---------- 4. Reenvio do mesmo estado/cabo ----------

        [Test]
        public void DefinirEstado_MesmoEstadoDuasVezes_DeveTransmitirEConfirmarAsDuas()
        {
            Assert.That(_engine.DefinirEstado(EstadoVeiculoCP.BXNominal), Is.True);
            _serial.SimularRecepcao("OK\r\n");
            Assert.That(_engine.DefinirEstado(EstadoVeiculoCP.BXNominal), Is.True);
            _serial.SimularRecepcao("OK\r\n");

            Assert.That(_serial.ComandosTransmitidos, Is.EqualTo(new[] { "estado bx", "estado bx" }));
            Assert.That(_concluidos.Select(c => c.Resultado), Is.EqualTo(new[] { ResultadoComando.Confirmado, ResultadoComando.Confirmado }));
            Assert.That(_concluidos[1].Id, Is.GreaterThan(_concluidos[0].Id));
            Assert.That(_engine.EstadoConfirmado, Is.EqualTo(EstadoVeiculoCP.BXNominal));
        }

        [Test]
        public void DefinirCabo_MesmoCaboDuasVezes_DeveTransmitirEConfirmarAsDuas()
        {
            Assert.That(_engine.DefinirCabo(EstadoCaboPP.Cabo20A), Is.True);
            Assert.That(_engine.DefinirCabo(EstadoCaboPP.Cabo20A), Is.True);
            _serial.SimularRecepcao("OK\r\nOK\r\n");

            Assert.That(_serial.ComandosTransmitidos, Is.EqualTo(new[] { "cabo 3", "cabo 3" }));
            Assert.That(_concluidos, Has.Count.EqualTo(2));
            Assert.That(_concluidos.All(c => c.Resultado == ResultadoComando.Confirmado && c.Cabo == EstadoCaboPP.Cabo20A));
            Assert.That(_engine.CaboConfirmado, Is.EqualTo(EstadoCaboPP.Cabo20A));
        }
    }
}
