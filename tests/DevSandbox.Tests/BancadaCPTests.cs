using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DevSandbox.Core.Bancada;
using DevSandbox.Tests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace DevSandbox.Tests
{
    /// <summary>
    /// Os 11 estados do CP (Tabela II da IEC 61851-1) alinhados ao firmware da Bancada (sem Estado F).
    /// Literais copiados de firmware/Bancada/src/main.cpp (menu de ajuda) e EmuladorVeiculoCP.cpp ([STATUS CP]).
    /// </summary>
    [TestFixture]
    public class EstadoVeiculoCPTests
    {
        //        estado,                        comando,       código, tensão, grupo,           variação
        private static readonly object[] Tabela =
        [
            new object[] { EstadoVeiculoCP.AX,         "estado ax",  "AX",  12.0, GrupoEstadoCP.A, VariacaoEstadoCP.Nominal },
            new object[] { EstadoVeiculoCP.BXSuperior, "estado bx+", "BX+", 10.0, GrupoEstadoCP.B, VariacaoEstadoCP.Superior },
            new object[] { EstadoVeiculoCP.BXNominal,  "estado bx",  "BX",   9.0, GrupoEstadoCP.B, VariacaoEstadoCP.Nominal },
            new object[] { EstadoVeiculoCP.BXInferior, "estado bx-", "BX-",  8.0, GrupoEstadoCP.B, VariacaoEstadoCP.Inferior },
            new object[] { EstadoVeiculoCP.CXSuperior, "estado cx+", "CX+",  7.0, GrupoEstadoCP.C, VariacaoEstadoCP.Superior },
            new object[] { EstadoVeiculoCP.CXNominal,  "estado cx",  "CX",   6.0, GrupoEstadoCP.C, VariacaoEstadoCP.Nominal },
            new object[] { EstadoVeiculoCP.CXInferior, "estado cx-", "CX-",  5.0, GrupoEstadoCP.C, VariacaoEstadoCP.Inferior },
            new object[] { EstadoVeiculoCP.DXSuperior, "estado dx+", "DX+",  4.0, GrupoEstadoCP.D, VariacaoEstadoCP.Superior },
            new object[] { EstadoVeiculoCP.DXNominal,  "estado dx",  "DX",   3.0, GrupoEstadoCP.D, VariacaoEstadoCP.Nominal },
            new object[] { EstadoVeiculoCP.DXInferior, "estado dx-", "DX-",  2.0, GrupoEstadoCP.D, VariacaoEstadoCP.Inferior },
            new object[] { EstadoVeiculoCP.E,          "estado e",   "E",    1.0, GrupoEstadoCP.E, VariacaoEstadoCP.Nominal },
        ];

        [TestCaseSource(nameof(Tabela))]
        public void Estado_DeveBaterComATabelaDoFirmware(EstadoVeiculoCP estado, string comando, string codigo,
                                                          double tensao, GrupoEstadoCP grupo, VariacaoEstadoCP variacao)
        {
            Assert.Multiple(() =>
            {
                Assert.That(estado.ObterComandoSerial(), Is.EqualTo(comando));
                Assert.That(estado.ObterCodigo(), Is.EqualTo(codigo));
                Assert.That(estado.ObterTensaoNominal(), Is.EqualTo(tensao));
                Assert.That(estado.ObterGrupo(), Is.EqualTo(grupo));
                Assert.That(estado.ObterVariacao(), Is.EqualTo(variacao));
                Assert.That(estado.ObterDescricao(), Does.StartWith(codigo + " - "));
                Assert.That(EstadoVeiculoCPExtensions.ObterEstado(grupo, variacao), Is.EqualTo(estado));
                Assert.That(EstadoVeiculoCPExtensions.TentarInterpretarCodigo(codigo.ToLowerInvariant(), out var lido), Is.True);
                Assert.That(lido, Is.EqualTo(estado));
            });
        }

        [Test]
        public void Todos_DeveTerOs11EstadosNaOrdemDoFirmware()
        {
            IReadOnlyList<EstadoVeiculoCP> todos = EstadoVeiculoCPExtensions.Todos;

            Assert.That(todos, Has.Count.EqualTo(11));
            Assert.That(todos.Select(e => (int)e), Is.EqualTo(Enumerable.Range(0, 11)), "Mesma numeração do CodigoEstadoCP do firmware.");
            Assert.That(Enum.GetValues<EstadoVeiculoCP>(), Is.EquivalentTo(todos));
            Assert.That(todos.Select(e => e.ObterComandoSerial()).Distinct().Count(), Is.EqualTo(11));
        }

        [Test]
        public void EstadoF_NaoDeveExistir()
        {
            Assert.That(Enum.GetNames<EstadoVeiculoCP>(), Has.None.StartsWith("F"));
            Assert.That(Enum.GetNames<GrupoEstadoCP>(), Has.None.EqualTo("F"));
            Assert.That(EstadoVeiculoCPExtensions.TentarInterpretarCodigo("F", out _), Is.False);
            Assert.That(EstadoVeiculoCPExtensions.Todos.Select(e => e.ObterComandoSerial()), Has.None.EqualTo("estado f"));
        }

        [TestCase(GrupoEstadoCP.A, VariacaoEstadoCP.Superior, EstadoVeiculoCP.AX)]
        [TestCase(GrupoEstadoCP.A, VariacaoEstadoCP.Inferior, EstadoVeiculoCP.AX)]
        [TestCase(GrupoEstadoCP.E, VariacaoEstadoCP.Superior, EstadoVeiculoCP.E)]
        [TestCase(GrupoEstadoCP.E, VariacaoEstadoCP.Inferior, EstadoVeiculoCP.E)]
        public void ObterEstado_AeE_DevemIgnorarVariacao(GrupoEstadoCP grupo, VariacaoEstadoCP variacao, EstadoVeiculoCP esperado)
        {
            Assert.That(EstadoVeiculoCPExtensions.ObterEstado(grupo, variacao), Is.EqualTo(esperado));
            Assert.That(grupo.TemVariacao(), Is.False);
        }

        [Test]
        public void TemVariacao_SoParaBCD()
        {
            Assert.That(GrupoEstadoCP.B.TemVariacao(), Is.True);
            Assert.That(GrupoEstadoCP.C.TemVariacao(), Is.True);
            Assert.That(GrupoEstadoCP.D.TemVariacao(), Is.True);
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("B")]   // atalho do firmware, não é código de status
        [TestCase("BX++")]
        [TestCase("A")]
        public void TentarInterpretarCodigo_Invalido_DeveRetornarFalse(string? codigo)
        {
            Assert.That(EstadoVeiculoCPExtensions.TentarInterpretarCodigo(codigo, out _), Is.False);
        }

        [Test]
        public void ValoresForaDoEnum_DevemLancarArgumentOutOfRange()
        {
            var invalido = (EstadoVeiculoCP)11;
            Assert.Throws<ArgumentOutOfRangeException>(() => invalido.ObterComandoSerial());
            Assert.Throws<ArgumentOutOfRangeException>(() => invalido.ObterTensaoNominal());
            Assert.Throws<ArgumentOutOfRangeException>(() => EstadoVeiculoCPExtensions.ObterEstado(GrupoEstadoCP.B, (VariacaoEstadoCP)3));
        }

        /// <summary>
        /// Confere os literais contra os fontes do firmware no próprio repositório (se o firmware estiver presente).
        /// </summary>
        [TestCaseSource(nameof(Tabela))]
        public void Estado_DeveAparecerNosFontesDoFirmware(EstadoVeiculoCP estado, string comando, string codigo,
                                                           double tensao, GrupoEstadoCP grupo, VariacaoEstadoCP variacao)
        {
            string? raiz = LocalizarRaizDoRepositorio();
            if (raiz == null) Assert.Ignore("Firmware não encontrado a partir da pasta de testes.");

            string mainCpp = File.ReadAllText(Path.Combine(raiz!, "firmware", "Bancada", "src", "main.cpp"));
            string emuladorCpp = File.ReadAllText(Path.Combine(raiz!, "firmware", "Bancada", "lib", "EmuladorVeiculoCP", "EmuladorVeiculoCP.cpp"));

            // Menu de ajuda: F(" estado bx+   -> ...")
            Assert.That(mainCpp, Does.Contain($"F(\" {comando} "), "Comando ausente do menu de ajuda do firmware.");
            // Status: F("BX+ | Vcp: 10 V | ...")
            Assert.That(emuladorCpp, Does.Contain($"F(\"{codigo} | Vcp: {tensao:0} V |"), "Código/tensão divergente do [STATUS CP] do firmware.");
        }

        internal static string? LocalizarRaizDoRepositorio()
        {
            DirectoryInfo? dir = new(TestContext.CurrentContext.TestDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "firmware", "Bancada", "src", "main.cpp"))) return dir.FullName;
                dir = dir.Parent;
            }
            return null;
        }
    }

    [TestFixture]
    public class EstadoCaboPPNotaTests
    {
        [Test]
        public void Canal1_DeveTrazerNotaDeValorDaBancadaSemMudarValorNemComando()
        {
            Assert.That(EstadoCaboPP.Desconectado.ObterDescricao(), Does.Contain("4700 Ω"));
            Assert.That(EstadoCaboPP.Desconectado.ObterDescricao(), Does.Contain("valor da bancada, fora da tabela IEC 62196-2"));
            Assert.That(EstadoCaboPP.Desconectado.ObterResistenciaNominal(), Is.EqualTo(4700));
            Assert.That(EstadoCaboPP.Desconectado.ObterComandoSerial(), Is.EqualTo("cabo 1"));
            Assert.That(EstadoCaboPP.Desconectado.ForaDaNorma(), Is.True);
        }

        [TestCase(EstadoCaboPP.CircuitoAberto)]
        [TestCase(EstadoCaboPP.Cabo13A)]
        [TestCase(EstadoCaboPP.Cabo20A)]
        [TestCase(EstadoCaboPP.Cabo32A)]
        [TestCase(EstadoCaboPP.Cabo63A)]
        [TestCase(EstadoCaboPP.FalhaCurto)]
        public void DemaisCanais_NaoTemNota(EstadoCaboPP cabo)
        {
            Assert.That(cabo.ForaDaNorma(), Is.False);
            Assert.That(cabo.ObterDescricao(), Does.Not.Contain("fora da tabela"));
        }
    }

    [TestFixture]
    public class RespostaFirmwareTests
    {
        [TestCase("OK", TipoRespostaFirmware.Ok, MotivoErroFirmware.Nenhum)]
        [TestCase("  OK \r", TipoRespostaFirmware.Ok, MotivoErroFirmware.Nenhum)]
        [TestCase("ERRO", TipoRespostaFirmware.Erro, MotivoErroFirmware.ComandoDesconhecido)]
        [TestCase("ERRO: Estado", TipoRespostaFirmware.Erro, MotivoErroFirmware.Estado)]
        [TestCase("ERRO: Cabo", TipoRespostaFirmware.Erro, MotivoErroFirmware.Cabo)]
        [TestCase("ERRO: CP indisponivel no Nano", TipoRespostaFirmware.Erro, MotivoErroFirmware.CPIndisponivel)]
        [TestCase("ERRO: Comando longo", TipoRespostaFirmware.Erro, MotivoErroFirmware.ComandoLongo)]
        [TestCase("ERRO: Algo novo", TipoRespostaFirmware.Erro, MotivoErroFirmware.Outro)]
        [TestCase("OKAY", TipoRespostaFirmware.Outra, MotivoErroFirmware.Nenhum)]
        [TestCase("ERROR", TipoRespostaFirmware.Outra, MotivoErroFirmware.Nenhum)]
        [TestCase(" Digite 'help' para ver os comandos disponiveis.  ", TipoRespostaFirmware.Outra, MotivoErroFirmware.Nenhum)]
        [TestCase(" estado bx+   -> BX limite superior (10 V)", TipoRespostaFirmware.Outra, MotivoErroFirmware.Nenhum)]
        [TestCase("", TipoRespostaFirmware.Outra, MotivoErroFirmware.Nenhum)]
        public void Interpretar_DeveClassificarLinhas(string linha, TipoRespostaFirmware tipo, MotivoErroFirmware motivo)
        {
            RespostaFirmware r = RespostaFirmware.Interpretar(linha);

            Assert.That(r.Tipo, Is.EqualTo(tipo));
            Assert.That(r.Motivo, Is.EqualTo(motivo));
            Assert.That(r.EhErro, Is.EqualTo(tipo == TipoRespostaFirmware.Erro));
            Assert.That(r.Linha, Is.EqualTo(linha.Trim()));
        }

        [Test]
        public void Interpretar_Nulo_DeveSerOutra()
        {
            Assert.That(RespostaFirmware.Interpretar(null).Tipo, Is.EqualTo(TipoRespostaFirmware.Outra));
        }

        [TestCase("[STATUS CP] Estado: AX | Vcp: 12 V | Veiculo nao conectado | Reles: nenhum", EstadoVeiculoCP.AX)]
        [TestCase("[STATUS CP] Estado: BX+ | Vcp: 10 V | Conectado, limite superior | Reles: S7", EstadoVeiculoCP.BXSuperior)]
        [TestCase("[STATUS CP] Estado: CX- | Vcp: 5 V | Carga sem ventilacao, limite inferior | Reles: S7 S9.1 S10.2 S11.2", EstadoVeiculoCP.CXInferior)]
        [TestCase("[STATUS CP] Estado: DX | Vcp: 3 V | Carga com ventilacao, nominal | Reles: S7 S9.1 S10.1 S12.1", EstadoVeiculoCP.DXNominal)]
        [TestCase("[STATUS CP] Estado: E | Vcp: 1 V | Falha / curto CP-PE | Reles: S7 S8", EstadoVeiculoCP.E)]
        public void Interpretar_StatusCP_DeveExtrairEstado(string linha, EstadoVeiculoCP esperado)
        {
            RespostaFirmware r = RespostaFirmware.Interpretar(linha);

            Assert.That(r.Tipo, Is.EqualTo(TipoRespostaFirmware.StatusCP));
            Assert.That(r.EstadoCP, Is.EqualTo(esperado));
        }

        [Test]
        public void Interpretar_StatusCPDesconhecido_DeveTerEstadoNulo()
        {
            RespostaFirmware r = RespostaFirmware.Interpretar("[STATUS CP] Estado: Desconhecido | Reles: nenhum");

            Assert.That(r.Tipo, Is.EqualTo(TipoRespostaFirmware.StatusCP));
            Assert.That(r.EstadoCP, Is.Null);
        }

        [TestCase("[STATUS CABO] Canal: 4 | R: 220 Ohms | Imax: 32 A | Descricao: Cabo 32 A", EstadoCaboPP.Cabo32A)]
        [TestCase("[STATUS CABO] Canal: 0 | R: 0 Ohms | Imax: 0 A | Descricao: Aberto", EstadoCaboPP.CircuitoAberto)]
        public void Interpretar_StatusCabo_DeveExtrairCanal(string linha, EstadoCaboPP esperado)
        {
            RespostaFirmware r = RespostaFirmware.Interpretar(linha);

            Assert.That(r.Tipo, Is.EqualTo(TipoRespostaFirmware.StatusCabo));
            Assert.That(r.CaboPP, Is.EqualTo(esperado));
        }

        [Test]
        public void Interpretar_StatusCaboComCanalInvalido_DeveTerCaboNulo()
        {
            Assert.That(RespostaFirmware.Interpretar("[STATUS CABO] Canal: 9 | R: 0 Ohms").CaboPP, Is.Null);
        }

        /// <summary>Os textos de erro do parser devem existir literalmente no main.cpp do firmware.</summary>
        [TestCase(RespostaFirmware.TextoErroEstado)]
        [TestCase(RespostaFirmware.TextoErroCabo)]
        [TestCase(RespostaFirmware.TextoErroCPIndisponivel)]
        [TestCase(RespostaFirmware.TextoErroComandoLongo)]
        [TestCase(RespostaFirmware.TextoOk)]
        [TestCase(RespostaFirmware.TextoErro)]
        public void TextosDoParser_DevemExistirNoFirmware(string texto)
        {
            string? raiz = EstadoVeiculoCPTests.LocalizarRaizDoRepositorio();
            if (raiz == null) Assert.Ignore("Firmware não encontrado a partir da pasta de testes.");

            string mainCpp = File.ReadAllText(Path.Combine(raiz!, "firmware", "Bancada", "src", "main.cpp"));
            // Desde o commit 7143b04 o firmware imprime pela SaidaSerial (Saida.println); aceita os dois
            Assert.That(mainCpp, Does.Match($@"(Serial|Saida)\.print(?:ln)?\(F\(""{Regex.Escape(texto)}""\)\)"));
        }
    }

    /// <summary>Desfecho dos comandos no BancadaEngine (estados visuais da UI).</summary>
    [TestFixture]
    public class BancadaEngineRespostasTests
    {
        private FakeSerialEngine _serial;
        private FakeTimeProvider _relogio;
        private BancadaEngine _engine;
        private List<ConclusaoComando> _concluidos;
        private List<RespostaFirmware> _respostas;

        [SetUp]
        public void Setup()
        {
            _serial = new FakeSerialEngine { EstaConectado = true };
            _relogio = new FakeTimeProvider();
            _engine = new BancadaEngine(_serial, relogio: _relogio);
            _concluidos = [];
            _respostas = [];
            _engine.ComandoConcluido += (s, c) => _concluidos.Add(c);
            _engine.RespostaRecebida += (s, r) => _respostas.Add(r);
        }

        [TearDown]
        public void TearDown()
        {
            _engine.Dispose();
            _serial.Dispose();
        }

        [Test]
        public void DefinirEstado_ComOK_DeveConcluirConfirmadoEAtualizarEstadoConfirmado()
        {
            Assert.That(_engine.DefinirEstado(EstadoVeiculoCP.BXSuperior), Is.True);
            Assert.That(_serial.ComandosTransmitidos, Is.EqualTo(new[] { "estado bx+" }));
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(1));
            Assert.That(_concluidos, Is.Empty);

            _serial.SimularRecepcao("OK\r\n");

            Assert.That(_concluidos, Has.Count.EqualTo(1));
            Assert.That(_concluidos[0].Comando, Is.EqualTo("estado bx+"));
            Assert.That(_concluidos[0].Resultado, Is.EqualTo(ResultadoComando.Confirmado));
            Assert.That(_concluidos[0].Estado, Is.EqualTo(EstadoVeiculoCP.BXSuperior));
            Assert.That(_concluidos[0].Resposta!.Tipo, Is.EqualTo(TipoRespostaFirmware.Ok));
            Assert.That(_engine.EstadoConfirmado, Is.EqualTo(EstadoVeiculoCP.BXSuperior));
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(0));
        }

        [Test]
        public void Resposta_EmFragmentos_DeveSerMontadaPorLinha()
        {
            _engine.DefinirCabo(EstadoCaboPP.Cabo32A);

            _serial.SimularRecepcao("O");
            Assert.That(_concluidos, Is.Empty);
            _serial.SimularRecepcao("K\r");
            Assert.That(_concluidos, Is.Empty);
            _serial.SimularRecepcao("\n");

            Assert.That(_concluidos.Single().Resultado, Is.EqualTo(ResultadoComando.Confirmado));
            Assert.That(_concluidos.Single().Cabo, Is.EqualTo(EstadoCaboPP.Cabo32A));
            Assert.That(_engine.CaboConfirmado, Is.EqualTo(EstadoCaboPP.Cabo32A));
        }

        [TestCase("ERRO: CP indisponivel no Nano", MotivoErroFirmware.CPIndisponivel)]
        [TestCase("ERRO: Estado", MotivoErroFirmware.Estado)]
        [TestCase("ERRO: Comando longo", MotivoErroFirmware.ComandoLongo)]
        public void DefinirEstado_ComErroDoFirmware_DeveConcluirErroSemConfirmarEstado(string linha, MotivoErroFirmware motivo)
        {
            _engine.DefinirEstado(EstadoVeiculoCP.CXNominal);

            _serial.SimularRecepcao(linha + "\r\n");

            ConclusaoComando c = _concluidos.Single();
            Assert.That(c.Resultado, Is.EqualTo(ResultadoComando.ErroFirmware));
            Assert.That(c.Resposta!.Motivo, Is.EqualTo(motivo));
            Assert.That(c.Estado, Is.EqualTo(EstadoVeiculoCP.CXNominal));
            Assert.That(_engine.EstadoConfirmado, Is.Null);
            // O estado selecionado localmente continua (a UI mostra o erro sobre a seleção)
            Assert.That(_engine.EstadoAtual, Is.EqualTo(EstadoVeiculoCP.CXNominal));
        }

        [Test]
        public void SemConexao_DeveConcluirNaoEnviadoEManterComandoEnviadoSemDisparar()
        {
            _serial.EstaConectado = false;
            int enviados = 0;
            _engine.ComandoEnviado += (s, c) => enviados++;

            Assert.That(_engine.DefinirEstado(EstadoVeiculoCP.DXInferior), Is.False);

            Assert.That(enviados, Is.EqualTo(0));
            Assert.That(_concluidos.Single().Resultado, Is.EqualTo(ResultadoComando.NaoEnviado));
            Assert.That(_concluidos.Single().Estado, Is.EqualTo(EstadoVeiculoCP.DXInferior));
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(0));
        }

        [Test]
        public void FalhaDeEscrita_DeveConcluirNaoEnviadoSemDeixarPendente()
        {
            _serial.FalharEnvio = true;

            Assert.That(_engine.DefinirCabo(EstadoCaboPP.Cabo13A), Is.False);

            Assert.That(_concluidos.Single().Resultado, Is.EqualTo(ResultadoComando.NaoEnviado));
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(0));
        }

        [Test]
        public void SemResposta_NoPrazo_DeveConcluirSemResposta()
        {
            _engine.DefinirEstado(EstadoVeiculoCP.E);

            _relogio.Advance(BancadaEngine.TimeoutRespostaPadrao - TimeSpan.FromMilliseconds(1));
            Assert.That(_concluidos, Is.Empty);

            _relogio.Advance(TimeSpan.FromMilliseconds(1));

            Assert.That(_concluidos.Single().Resultado, Is.EqualTo(ResultadoComando.SemResposta));
            Assert.That(_concluidos.Single().Estado, Is.EqualTo(EstadoVeiculoCP.E));
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(0));
        }

        [Test]
        public void OKAtrasado_DepoisDoPrazo_NaoDeveConcluirComandoSeguinte()
        {
            _engine.DefinirEstado(EstadoVeiculoCP.AX);
            _relogio.Advance(BancadaEngine.TimeoutRespostaPadrao);
            _concluidos.Clear();

            _serial.SimularRecepcao("OK\r\n"); // OK do comando que já expirou

            Assert.That(_concluidos, Is.Empty);
        }

        [Test]
        public void VariosComandos_DevemSerConcluidosEmOrdem()
        {
            _engine.DefinirCabo(EstadoCaboPP.Cabo20A);
            _engine.DefinirEstado(EstadoVeiculoCP.BXInferior);
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(2));

            _serial.SimularRecepcao("OK\r\nERRO: Estado\r\n");

            Assert.That(_concluidos.Select(c => c.Comando), Is.EqualTo(new[] { "cabo 3", "estado bx-" }));
            Assert.That(_concluidos.Select(c => c.Resultado),
                        Is.EqualTo(new[] { ResultadoComando.Confirmado, ResultadoComando.ErroFirmware }));
            Assert.That(_engine.CaboConfirmado, Is.EqualTo(EstadoCaboPP.Cabo20A));
            Assert.That(_engine.EstadoConfirmado, Is.Null);
        }

        [Test]
        public void SolicitarStatusCP_DeveConcluirComStatusEDispararStatusCPRecebido()
        {
            EstadoVeiculoCP? status = null;
            _engine.StatusCPRecebido += (s, e) => status = e;

            Assert.That(_engine.SolicitarStatusCP(), Is.True);
            Assert.That(_serial.ComandosTransmitidos.Single(), Is.EqualTo("estado status"));

            _serial.SimularRecepcao("[STATUS CP] Estado: DX- | Vcp: 2 V | Carga com ventilacao, limite inferior | Reles: S7 S9.1 S10.1 S12.2\r\n");

            Assert.That(_concluidos.Single().Resultado, Is.EqualTo(ResultadoComando.Confirmado));
            Assert.That(_concluidos.Single().Resposta!.Tipo, Is.EqualTo(TipoRespostaFirmware.StatusCP));
            Assert.That(status, Is.EqualTo(EstadoVeiculoCP.DXInferior));
            Assert.That(_engine.EstadoConfirmado, Is.EqualTo(EstadoVeiculoCP.DXInferior));
        }

        [Test]
        public void SolicitarStatusCP_NoNano_DeveConcluirErroFirmware()
        {
            _engine.SolicitarStatusCP();

            _serial.SimularRecepcao("ERRO: CP indisponivel no Nano\r\n");

            Assert.That(_concluidos.Single().Resultado, Is.EqualTo(ResultadoComando.ErroFirmware));
            Assert.That(_concluidos.Single().Resposta!.Motivo, Is.EqualTo(MotivoErroFirmware.CPIndisponivel));
        }

        [Test]
        public void LinhasDoMenuDeAjuda_NaoDevemConcluirComandoDeEstado()
        {
            _engine.DefinirEstado(EstadoVeiculoCP.CXSuperior);

            _serial.SimularRecepcao(" estado cx+   -> CX limite superior (7 V)\r\n==========\r\n");
            Assert.That(_concluidos, Is.Empty);
            Assert.That(_respostas.Select(r => r.Tipo), Is.All.EqualTo(TipoRespostaFirmware.Outra));

            _serial.SimularRecepcao("OK\r\n");
            Assert.That(_concluidos.Single().Resultado, Is.EqualTo(ResultadoComando.Confirmado));
        }

        [TestCase("help")]
        [TestCase("?")]
        [TestCase("  HELP ")]
        [TestCase("debug stack")]
        public void ComandosSemConfirmacao_NaoDevemFicarPendentes(string comando)
        {
            Assert.That(_engine.EnviarComandoManual(comando), Is.True);
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(0));
        }

        [TestCase("estado b", EstadoVeiculoCP.BXNominal)]
        [TestCase("ESTADO   BX+", EstadoVeiculoCP.BXSuperior)]
        [TestCase("estado dx-", EstadoVeiculoCP.DXInferior)]
        [TestCase("estado a", EstadoVeiculoCP.AX)]
        public void ComandoManualDeEstado_ConfirmadoDeveAtualizarEstadoConfirmado(string comando, EstadoVeiculoCP esperado)
        {
            _engine.EnviarComandoManual(comando);
            _serial.SimularRecepcao("OK\n");

            Assert.That(_concluidos.Single().Estado, Is.EqualTo(esperado));
            Assert.That(_engine.EstadoConfirmado, Is.EqualTo(esperado));
        }

        [Test]
        public void ComandoManualDesconhecido_ComERRO_DeveConcluirErroFirmware()
        {
            _engine.EnviarComandoManual("xyz");
            _serial.SimularRecepcao("ERRO\r\n");

            Assert.That(_concluidos.Single().Resultado, Is.EqualTo(ResultadoComando.ErroFirmware));
            Assert.That(_concluidos.Single().Resposta!.Motivo, Is.EqualTo(MotivoErroFirmware.ComandoDesconhecido));
            Assert.That(_concluidos.Single().Estado, Is.Null);
        }

        [Test]
        public void RespostaSemComandoPendente_DeveSerRepassadaSemConcluirNada()
        {
            _serial.SimularRecepcao("OK\r\nERRO\r\n");

            Assert.That(_concluidos, Is.Empty);
            Assert.That(_respostas.Select(r => r.Tipo), Is.EqualTo(new[] { TipoRespostaFirmware.Ok, TipoRespostaFirmware.Erro }));
        }

        [Test]
        public void PerdaDeConexao_DeveConcluirPendentesComoSemResposta()
        {
            _engine.DefinirCabo(EstadoCaboPP.Cabo63A);
            _engine.DefinirEstado(EstadoVeiculoCP.DXNominal);

            _serial.SimularDesconexao();

            Assert.That(_concluidos.Select(c => c.Resultado), Is.All.EqualTo(ResultadoComando.SemResposta));
            Assert.That(_concluidos, Has.Count.EqualTo(2));
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(0));
        }

        [Test]
        public void Desconectar_DeveDescartarLinhaParcialEPendentes()
        {
            _engine.DefinirEstado(EstadoVeiculoCP.AX);
            _serial.SimularRecepcao("O");

            _engine.Desconectar();
            _serial.EstaConectado = true;
            _engine.DefinirEstado(EstadoVeiculoCP.BXNominal);
            _serial.SimularRecepcao("K\r\n"); // sobra da linha antiga não forma "OK"

            Assert.That(_concluidos.Single().Resultado, Is.EqualTo(ResultadoComando.SemResposta));
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(1));
        }

        [Test]
        public void DadosRecebidos_DeveContinuarRepassandoTextoBruto()
        {
            var brutos = new List<string>();
            _engine.DadosRecebidos += (s, d) => brutos.Add(d);

            _serial.SimularRecepcao("O");
            _serial.SimularRecepcao("K\r\n");

            Assert.That(brutos, Is.EqualTo(new[] { "O", "K\r\n" }));
        }

        [Test]
        public void Construtor_TimeoutInvalido_DeveLancar()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BancadaEngine(new FakeSerialEngine(), timeoutResposta: TimeSpan.Zero));
        }

        [Test]
        public void DefinirEstado_ForaDoEnum_DeveLancar()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _engine.DefinirEstado((EstadoVeiculoCP)42));
        }
    }
}
