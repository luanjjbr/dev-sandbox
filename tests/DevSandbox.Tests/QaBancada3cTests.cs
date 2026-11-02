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
    /// Matriz de desfechos (Confirmado / ErroFirmware / NaoEnviado / SemResposta) x tipo de comando
    /// (estado, cabo, manual, status CP, status cabo), incluindo Id, órfã e acento.
    /// </summary>
    [TestFixture]
    public class QaBancadaMatrizDesfechosTests
    {
        private const string StatusCabo = "[STATUS CABO] Canal: 4 | R: 220 Ohms | Imax: 32 A | Descricao: Cabo 32 A";
        private const string StatusCP = "[STATUS CP] Estado: CX | Vcp: 6 V | Carga sem ventilacao, nominal | Reles: S7 S9.1 S10.2 S11.1";

        private FakeSerialEngine _serial;
        private FakeTimeProvider _relogio;
        private BancadaEngine _engine;
        private List<ConclusaoComando> _concluidos;
        private int _enviados;

        [SetUp]
        public void Setup()
        {
            _serial = new FakeSerialEngine { EstaConectado = true };
            _relogio = new FakeTimeProvider();
            _engine = new BancadaEngine(_serial, relogio: _relogio);
            _concluidos = [];
            _enviados = 0;
            _engine.ComandoConcluido += (s, c) => _concluidos.Add(c);
            _engine.ComandoEnviado += (s, c) => _enviados++;
        }

        [TearDown]
        public void TearDown()
        {
            _engine.Dispose();
            _serial.Dispose();
        }

        public sealed record Caso(string Nome, Func<BancadaEngine, bool> Enviar, string Comando,
                                  EstadoVeiculoCP? Estado, EstadoCaboPP? Cabo, string RespostaOk, string RespostaErro)
        {
            public override string ToString() => Nome;
        }

        private static readonly Caso[] Casos =
        [
            new("estado", e => e.DefinirEstado(EstadoVeiculoCP.CXNominal), "estado cx", EstadoVeiculoCP.CXNominal, null, "OK", "ERRO: Estado"),
            new("cabo", e => e.DefinirCabo(EstadoCaboPP.Cabo20A), "cabo 3", null, EstadoCaboPP.Cabo20A, "OK", "ERRO: Cabo"),
            new("manual-estado", e => e.EnviarComandoManual("estado dx-"), "estado dx-", EstadoVeiculoCP.DXInferior, null, "OK", "ERRO: Estado"),
            new("manual-cabo", e => e.EnviarComandoManual("cabo 5"), "cabo 5", null, EstadoCaboPP.Cabo63A, "OK", "ERRO: Cabo"),
            new("manual-livre", e => e.EnviarComandoManual("xyz"), "xyz", null, null, "OK", "ERRO"),
            new("status-cp", e => e.SolicitarStatusCP(), "estado status", null, null, StatusCP, "ERRO: CP indisponivel no Nano"),
            new("status-cabo", e => e.EnviarComandoManual("cabo status"), "cabo status", null, null, StatusCabo, "ERRO: Cabo"),
        ];

        private static IEnumerable<TestCaseData> TodosOsCasos() =>
            Casos.Select(c => new TestCaseData(c).SetArgDisplayNames(c.Nome));

        [TestCaseSource(nameof(TodosOsCasos))]
        public void Confirmado(Caso caso)
        {
            Assert.That(caso.Enviar(_engine), Is.True);
            long id = _engine.UltimoIdComando;
            Assert.That(_concluidos, Is.Empty, "Não conclui antes da resposta.");

            _serial.SimularRecepcao(caso.RespostaOk + "\r\n");

            ConclusaoComando c = _concluidos.Single();
            Assert.That(c.Resultado, Is.EqualTo(ResultadoComando.Confirmado));
            Assert.That(c.Comando, Is.EqualTo(caso.Comando));
            Assert.That(c.Estado, Is.EqualTo(caso.Estado));
            Assert.That(c.Cabo, Is.EqualTo(caso.Cabo));
            Assert.That(c.Resposta, Is.Not.Null);
            Assert.That(c.Id, Is.EqualTo(id));
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(0));
            if (caso.Estado is { } e) Assert.That(_engine.EstadoConfirmado, Is.EqualTo(e));
            if (caso.Cabo is { } cb) Assert.That(_engine.CaboConfirmado, Is.EqualTo(cb));
        }

        [TestCaseSource(nameof(TodosOsCasos))]
        public void ErroFirmware(Caso caso)
        {
            caso.Enviar(_engine);
            long id = _engine.UltimoIdComando;

            _serial.SimularRecepcao(caso.RespostaErro + "\r\n");

            ConclusaoComando c = _concluidos.Single();
            Assert.That(c.Resultado, Is.EqualTo(ResultadoComando.ErroFirmware));
            Assert.That(c.Resposta!.Tipo, Is.EqualTo(TipoRespostaFirmware.Erro));
            Assert.That(c.Estado, Is.EqualTo(caso.Estado));
            Assert.That(c.Cabo, Is.EqualTo(caso.Cabo));
            Assert.That(c.Id, Is.EqualTo(id));
            Assert.That(_engine.EstadoConfirmado, Is.Null, "Erro não confirma estado.");
            Assert.That(_engine.CaboConfirmado, Is.Null, "Erro não confirma cabo.");
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(0));
        }

        [TestCaseSource(nameof(TodosOsCasos))]
        public void NaoEnviado_SemConexao(Caso caso)
        {
            _serial.EstaConectado = false;

            Assert.That(caso.Enviar(_engine), Is.False);

            ConclusaoComando c = _concluidos.Single();
            Assert.That(c.Resultado, Is.EqualTo(ResultadoComando.NaoEnviado));
            Assert.That(c.Comando, Is.EqualTo(caso.Comando));
            Assert.That(c.Estado, Is.EqualTo(caso.Estado));
            Assert.That(c.Cabo, Is.EqualTo(caso.Cabo));
            Assert.That(c.Resposta, Is.Null);
            Assert.That(c.Detalhe, Is.EqualTo(BancadaEngine.DetalheSemConexao));
            Assert.That(c.Id, Is.EqualTo(_engine.UltimoIdComando));
            Assert.That(_enviados, Is.EqualTo(0));
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(0));
        }

        [TestCaseSource(nameof(TodosOsCasos))]
        public void NaoEnviado_FalhaDeEscrita(Caso caso)
        {
            _serial.FalharEnvio = true;

            Assert.That(caso.Enviar(_engine), Is.False);

            Assert.That(_concluidos.Single().Resultado, Is.EqualTo(ResultadoComando.NaoEnviado));
            Assert.That(_enviados, Is.EqualTo(0));
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(0));
        }

        [TestCaseSource(nameof(TodosOsCasos))]
        public void SemResposta_NoPrazo_ERespostaAtrasadaNaoConcluiOSeguinte(Caso caso)
        {
            caso.Enviar(_engine);
            long id = _engine.UltimoIdComando;

            _relogio.Advance(BancadaEngine.TimeoutRespostaPadrao);

            ConclusaoComando c = _concluidos.Single();
            Assert.That(c.Resultado, Is.EqualTo(ResultadoComando.SemResposta));
            Assert.That(c.Comando, Is.EqualTo(caso.Comando));
            Assert.That(c.Estado, Is.EqualTo(caso.Estado));
            Assert.That(c.Cabo, Is.EqualTo(caso.Cabo));
            Assert.That(c.Resposta, Is.Null);
            Assert.That(c.Id, Is.EqualTo(id));
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(0));
            Assert.That(_engine.EstadoConfirmado, Is.Null);
            Assert.That(_engine.CaboConfirmado, Is.Null);

            // Um comando novo e, depois, a resposta atrasada do antigo: a atrasada é órfã e não conclui o novo
            _engine.DefinirCabo(EstadoCaboPP.Cabo13A);
            _serial.SimularRecepcao(caso.RespostaOk + "\r\n");

            Assert.That(_concluidos, Has.Count.EqualTo(1));
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(1));
        }

        [TestCaseSource(nameof(TodosOsCasos))]
        public void SemResposta_PerdaDeConexao(Caso caso)
        {
            caso.Enviar(_engine);

            _serial.SimularDesconexao();

            ConclusaoComando c = _concluidos.Single();
            Assert.That(c.Resultado, Is.EqualTo(ResultadoComando.SemResposta));
            Assert.That(c.Estado, Is.EqualTo(caso.Estado));
            Assert.That(c.Cabo, Is.EqualTo(caso.Cabo));
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(0));
        }

        [Test]
        public void Id_EUnicoESequencialEntreTodosOsDesfechos()
        {
            _engine.DefinirEstado(EstadoVeiculoCP.AX);                 // 1: confirmado
            _serial.SimularRecepcao("OK\r\n");
            _engine.DefinirCabo(EstadoCaboPP.Cabo32A);                 // 2: erro
            _serial.SimularRecepcao("ERRO: Cabo\r\n");
            _serial.EstaConectado = false;
            _engine.SolicitarStatusCP();                               // 3: não enviado
            _engine.EnviarComandoManual("estado ç");                   // 4: recusado (acento)
            _serial.EstaConectado = true;
            _engine.EnviarComandoManual("cabo 1");                     // 5: sem resposta
            _relogio.Advance(BancadaEngine.TimeoutRespostaPadrao);

            Assert.That(_concluidos.Select(c => c.Id), Is.EqualTo(new long[] { 1, 2, 3, 4, 5 }));
            Assert.That(_concluidos.Select(c => c.Resultado), Is.EqualTo(new[]
            {
                ResultadoComando.Confirmado, ResultadoComando.ErroFirmware, ResultadoComando.NaoEnviado,
                ResultadoComando.NaoEnviado, ResultadoComando.SemResposta
            }));
            Assert.That(_engine.UltimoIdComando, Is.EqualTo(5));
        }

        [Test]
        public void ComandoComAcento_NaoTransmiteNemFicaPendenteEPreservaAlvo()
        {
            Assert.That(_engine.EnviarComandoManual("estado bx+ ç"), Is.False);
            Assert.That(_serial.ComandosTransmitidos, Is.Empty);
            Assert.That(_engine.ComandosPendentes, Is.EqualTo(0));
            Assert.That(_concluidos.Single().Detalhe, Is.EqualTo(BancadaEngine.DetalheCaractereInvalido));
            Assert.That(_concluidos.Single().Id, Is.EqualTo(_engine.UltimoIdComando));

            // Não deve atrapalhar um comando válido seguinte
            _engine.EnviarComandoManual("estado a");
            _serial.SimularRecepcao("OK\r\n");
            Assert.That(_concluidos, Has.Count.EqualTo(2));
            Assert.That(_concluidos[1].Resultado, Is.EqualTo(ResultadoComando.Confirmado));
        }

        [Test]
        public void StatusCabo_AtualizaCaboConfirmadoSemTerComandoDeCabo()
        {
            _engine.EnviarComandoManual("cabo status");
            _serial.SimularRecepcao(StatusCabo + "\r\n");

            Assert.That(_concluidos.Single().Resultado, Is.EqualTo(ResultadoComando.Confirmado));
            Assert.That(_engine.CaboConfirmado, Is.EqualTo(EstadoCaboPP.Cabo32A));
        }

        [Test]
        public void PrazoPorAtividade_TambemVaiParaOSegundoDaFila()
        {
            _engine.DefinirCabo(EstadoCaboPP.Cabo13A);
            _engine.DefinirEstado(EstadoVeiculoCP.BXNominal);

            _relogio.Advance(TimeSpan.FromSeconds(3));
            _serial.SimularRecepcao("OK\r\n"); // conclui o 1º; o 2º começa a contar agora
            _relogio.Advance(TimeSpan.FromSeconds(3));
            Assert.That(_concluidos, Has.Count.EqualTo(1));

            _relogio.Advance(TimeSpan.FromSeconds(1));
            Assert.That(_concluidos.Select(c => c.Resultado),
                        Is.EqualTo(new[] { ResultadoComando.Confirmado, ResultadoComando.SemResposta }));
        }
    }

    /// <summary>
    /// Contrato app x firmware lido dos fontes (firmware/Bancada): comandos, [STATUS CP], enum e tabela de relés.
    /// </summary>
    [TestFixture]
    public class QaContratoFirmwareTests
    {
        private static string Ler(params string[] partes)
        {
            string? raiz = EstadoVeiculoCPTests.LocalizarRaizDoRepositorio();
            if (raiz == null) Assert.Ignore("Firmware não encontrado a partir da pasta de testes.");
            return File.ReadAllText(Path.Combine([raiz!, "firmware", "Bancada", .. partes]));
        }

        // Nome do enum do firmware -> código do app (AX_NOMINAL -> AX, BX_SUPERIOR -> BX+, E_FALHA -> E)
        private static string CodigoDoEnumDoFirmware(string nome) => nome switch
        {
            "AX_NOMINAL" => "AX",
            "E_FALHA" => "E",
            _ => nome[..2] + (nome.EndsWith("_SUPERIOR") ? "+" : nome.EndsWith("_INFERIOR") ? "-" : "")
        };

        private static List<string> NomesDoEnumDoFirmware()
        {
            string h = Ler("lib", "EmuladorVeiculoCP", "TabelaEstadosCP.h");
            Match corpo = Regex.Match(h, @"enum class CodigoEstadoCP[^{]*\{(?<c>.*?)\};", RegexOptions.Singleline);
            Assert.That(corpo.Success, Is.True, "enum CodigoEstadoCP não encontrado.");
            return Regex.Matches(corpo.Groups["c"].Value, @"^\s*([A-Z_]+)\b", RegexOptions.Multiline)
                        .Select(m => m.Groups[1].Value).ToList();
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void CaboN_ExisteNoMenuENoComandoDoApp(int n)
        {
            string main = Ler("src", "main.cpp");
            var cabo = (EstadoCaboPP)n;

            Assert.That(cabo.ObterComandoSerial(), Is.EqualTo($"cabo {n}"));
            Assert.That(main, Does.Contain($"F(\" cabo {n} "), "Comando ausente do menu de ajuda.");
        }

        [Test]
        public void Cabos_SaoExatamenteSeteECobremOIntervaloAceitoPeloFirmware()
        {
            string main = Ler("src", "main.cpp");

            Assert.That(Enum.GetValues<EstadoCaboPP>(), Has.Length.EqualTo(7));
            Assert.That(Enum.GetValues<EstadoCaboPP>().Select(c => (int)c), Is.EqualTo(Enumerable.Range(0, 7)));
            Assert.That(main, Does.Contain("arg[0] >= '0' && arg[0] <= '6' && arg[1] == '\\0'"));
            Assert.That(main, Does.Contain("strcmp(arg, \"status\") == 0"));
        }

        [Test]
        public void EnumDoFirmware_TemOs11EstadosNaMesmaOrdemDoEnumDoApp()
        {
            List<string> nomes = NomesDoEnumDoFirmware();

            Assert.That(nomes, Has.Count.EqualTo(11));
            Assert.That(Ler("lib", "EmuladorVeiculoCP", "TabelaEstadosCP.h"), Does.Contain("CP_TOTAL_ESTADOS = 11"));
            Assert.That(nomes.Select(CodigoDoEnumDoFirmware),
                        Is.EqualTo(EstadoVeiculoCPExtensions.Todos.Select(e => e.ObterCodigo())),
                        "A ordem do CodigoEstadoCP deve ser a do enum EstadoVeiculoCP.");
        }

        // Relés esperados por estado (Tabela II / docs do firmware), na ordem do enum
        private static readonly string[][] RelesEsperados =
        [
            [],                                        // AX
            ["S7"],                                    // BX+
            ["S7", "S9.1"],                            // BX
            ["S7", "S9.2"],                            // BX-
            ["S7", "S9.1", "S10.2"],                   // CX+
            ["S7", "S9.1", "S10.2", "S11.1"],          // CX
            ["S7", "S9.1", "S10.2", "S11.2"],          // CX-
            ["S7", "S9.1", "S10.1"],                   // DX+
            ["S7", "S9.1", "S10.1", "S12.1"],          // DX
            ["S7", "S9.1", "S10.1", "S12.2"],          // DX-
            ["S7", "S8"],                              // E
        ];

        // Nome do bit (CP_RELE_S9_1) -> nome impresso (S9.1), na ordem dos bits do cabeçalho
        private static Dictionary<string, (int Bit, string Nome)> RelesDoCabecalho()
        {
            string h = Ler("lib", "EmuladorVeiculoCP", "TabelaEstadosCP.h");
            var mapa = new Dictionary<string, (int, string)>();
            foreach (Match m in Regex.Matches(h, @"CP_RELE_(S\d+(?:_[A-Z0-9]+)?)\s*=\s*\(1U\s*<<\s*(\d+)\)"))
            {
                string id = "CP_RELE_" + m.Groups[1].Value;
                string chave = m.Groups[1].Value;
                string nome = chave switch
                {
                    "S10_C" => "S10.2",
                    "S10_D" => "S10.1",
                    _ => chave.Replace('_', '.')
                };
                mapa[id] = (int.Parse(m.Groups[2].Value), nome);
            }
            return mapa;
        }

        [Test]
        public void TabelaDeReles_TemAs11MesmasEntradasNaOrdemDoEnumDoApp()
        {
            string cpp = Ler("lib", "EmuladorVeiculoCP", "TabelaEstadosCP.cpp");
            Match corpo = Regex.Match(cpp, @"TABELA_RELES_CP\[CP_TOTAL_ESTADOS\]\s*=\s*\{(?<c>.*?)\};", RegexOptions.Singleline);
            Assert.That(corpo.Success, Is.True, "TABELA_RELES_CP não encontrada.");

            var linhas = Regex.Matches(corpo.Groups["c"].Value, @"/\*\s*([A-Z_]+)\s*\*/\s*(?<v>[^,\r\n]+?)\s*(?:,|$)", RegexOptions.Multiline)
                              .Select(m => (Nome: m.Groups[1].Value, Valor: m.Groups["v"].Value)).ToList();
            Assert.That(linhas, Has.Count.EqualTo(11));

            // Mesma ordem do enum do firmware e, portanto, do app
            Assert.That(linhas.Select(l => l.Nome), Is.EqualTo(NomesDoEnumDoFirmware()));

            var reles = RelesDoCabecalho();
            Assert.That(reles, Has.Count.EqualTo(10));

            for (int i = 0; i < 11; i++)
            {
                string valor = linhas[i].Valor;
                string[] lidos = valor.Contains("CP_RELES_NENHUM")
                    ? []
                    : Regex.Matches(valor, @"CP_RELE_\w+").Select(m => m.Value)
                           .OrderBy(id => reles[id].Bit).Select(id => reles[id].Nome).ToArray();

                Assert.That(lidos, Is.EqualTo(RelesEsperados[i]),
                            $"Relés de {EstadoVeiculoCPExtensions.Todos[i].ObterCodigo()} divergem da Tabela II.");
            }
        }

        [Test]
        public void NomesImpressosNoStatus_BatemComOsBitsDoCabecalho()
        {
            var reles = RelesDoCabecalho();
            string emulador = Ler("lib", "EmuladorVeiculoCP", "EmuladorVeiculoCP.cpp");
            Match arr = Regex.Match(emulador, @"NOMES_RELES\[CP_TOTAL_RELES\]\[\d+\][^=]*=\s*\{(?<c>.*?)\};", RegexOptions.Singleline);
            Assert.That(arr.Success, Is.True, "NOMES_RELES não encontrado.");

            string[] impressos = Regex.Matches(arr.Groups["c"].Value, "\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToArray();

            Assert.That(impressos, Has.Length.EqualTo(10));
            foreach (var (bit, nome) in reles.Values)
                Assert.That(impressos[bit], Is.EqualTo(nome), $"Bit {bit} do cabeçalho x nome impresso no [STATUS CP].");
        }

        [Test]
        public void StatusCPDoFirmware_DeCadaEstado_ResolveParaOEstadoCertoDoApp()
        {
            string emulador = Ler("lib", "EmuladorVeiculoCP", "EmuladorVeiculoCP.cpp");
            List<string> nomes = NomesDoEnumDoFirmware();
            var casos = Regex.Matches(emulador, @"case CodigoEstadoCP::(\w+):\s*Serial\.print\(F\(""([^""]*)""\)\)");
            Assert.That(casos, Has.Count.EqualTo(11), "Um case por estado no imprimirStatus.");

            foreach (Match m in casos)
            {
                int indice = nomes.IndexOf(m.Groups[1].Value);
                Assert.That(indice, Is.GreaterThanOrEqualTo(0), m.Groups[1].Value);
                EstadoVeiculoCP esperado = EstadoVeiculoCPExtensions.Todos[indice];

                string reles = RelesEsperados[indice].Length == 0 ? "nenhum" : string.Join(' ', RelesEsperados[indice]);
                string linha = $"[STATUS CP] Estado: {m.Groups[2].Value} | Reles: {reles}";

                RespostaFirmware r = RespostaFirmware.Interpretar(linha);
                Assert.That(r.Tipo, Is.EqualTo(TipoRespostaFirmware.StatusCP), linha);
                Assert.That(r.EstadoCP, Is.EqualTo(esperado), linha);

                Match v = Regex.Match(m.Groups[2].Value, @"Vcp:\s*(\d+) V");
                Assert.That(double.Parse(v.Groups[1].Value), Is.EqualTo(esperado.ObterTensaoNominal()), linha);
            }
        }

        [Test]
        public void Firmware_ResponderOK_EErro_ParaEstadoStatusEDesconhecido()
        {
            string main = Ler("src", "main.cpp");

            Assert.That(main, Does.Contain("strcmp(arg, \"status\") == 0"));
            Assert.That(main, Does.Contain("comecaComPalavra(comando, \"estado\")"));
            Assert.That(main, Does.Contain("comecaComPalavra(comando, \"cabo\")"));
            Assert.That(EstadoVeiculoCPExtensions.ComandoStatus, Is.EqualTo("estado status"));
        }
    }
}
