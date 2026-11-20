using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using DevSandbox.Core.Bancada;
using DevSandbox.Core.Serial;
using DevSandbox.UI.Common;

namespace DevSandbox.UI.Telas
{
    /// <summary>
    /// Bancada EVSE: telemetria nominal + sincronismo, colunas CP (A–E + tolerância), PP (0–6) e matriz de relés
    /// (somente leitura, confirmada pelo firmware), e o terminal serial com conexão, comando manual e fim de linha.
    /// A verdade do hardware é <see cref="BancadaEngine.EstadoConfirmado"/>/<see cref="BancadaEngine.CaboConfirmado"/>;
    /// a seleção dos cartões é a local.
    /// </summary>
    public sealed class TelaBancada : TelaBase
    {
        private readonly BancadaEngine _engine = new();
        private bool _carregandoUI = true;

        // Seleção local da coluna CP em dois níveis
        private GrupoEstadoCP _grupoCP = GrupoEstadoCP.A;
        private VariacaoEstadoCP _variacaoCP = VariacaoEstadoCP.Nominal;

        // Id do último pedido feito pela UI (0 = nenhum): só a conclusão com esse Id altera a situação.
        // "_enviando*" cobre a conclusão síncrona (NaoEnviado) que chega DENTRO de DefinirEstado/DefinirCabo.
        private long _idPedidoCP;
        private long _idPedidoPP;
        private bool _enviandoCP;
        private bool _enviandoPP;
        private Situacao _situacaoCP = Situacao.Local;
        private Situacao _situacaoPP = Situacao.Local;
        private string _detalheCP = "";
        private string _detalhePP = "";

        // Sincronização ao conectar: a placa reinicia com o DTR, então o primeiro "estado status" pode se perder.
        private const int AtrasoRetentativaStatusMs = 2500;
        private readonly System.Windows.Forms.Timer _timerRetentativaStatus = new() { Interval = AtrasoRetentativaStatusMs };
        private long _idStatusInicial;
        private bool _retentativaStatusFeita;

        // Fim de linha do último comando manual (para a etiqueta do [TX] que o motor ecoa)
        private (string Comando, FimDeLinha Fim)? _manualPendente;

        // Histórico de comandos manuais (↑/↓)
        private readonly HistoricoComandos _historico = new();
        private bool _navegando;

        private enum Situacao { Local, Aguardando, Confirmado, NaoEnviado, ErroFirmware, SemResposta }

        private static readonly GrupoEstadoCP[] Grupos = { GrupoEstadoCP.A, GrupoEstadoCP.B, GrupoEstadoCP.C, GrupoEstadoCP.D, GrupoEstadoCP.E };
        private static readonly VariacaoEstadoCP[] Variacoes = { VariacaoEstadoCP.Superior, VariacaoEstadoCP.Nominal, VariacaoEstadoCP.Inferior };
        private static readonly EstadoCaboPP[] Cabos = Enum.GetValues<EstadoCaboPP>();
        private static readonly int[] PinosNaOrdem = { 30, 31, 29, 28, 27, 26, 25, 24, 23, 22 };

        /// <summary>Função de cada relé no circuito CP (tabela 5.1 do design v2), só para a dica.</summary>
        private static readonly Dictionary<int, string> FuncaoRele = new()
        {
            [30] = "RLY6 / S7: habilitação geral de PE. Conecta o diodo do veículo ao condutor de aterramento.",
            [31] = "RLY1 / S8: curto-circuito CP-PE antes do diodo (exclusivo do estado E).",
            [29] = "RLY7 / S9.1: habilitação do ramo resistivo do estado B (+9 V).",
            [28] = "RLY2 / S9.2: seleção de tolerância do estado B.",
            [27] = "RLY5 / S10_C: habilitação do ramo paralelo dos estados C e D.",
            [26] = "RLY8 / S10_D: comutação entre o ramo C e o ramo D.",
            [25] = "RLY3 / S11.1: habilitação do resistor paralelo do estado C (+6 V).",
            [24] = "RLY4 / S11.2: seleção de tolerância do estado C.",
            [23] = "RLY9 / S12.1: habilitação do resistor paralelo do estado D (+3 V).",
            [22] = "RLY10 / S12.2: seleção de tolerância do estado D."
        };

        // ---- Controles
        private readonly CardNordic _hud;
        private readonly LeituraTelemetria _leituraTensao;
        private readonly LeituraTelemetria _leituraCorrente;
        private readonly LeituraTelemetria _leituraResistencia;
        private readonly LeituraTelemetria _leituraSincronismo;
        private readonly CardNordic _cardCP;
        private readonly CartaoEstado[] _cartoesCP;
        private readonly SeletorSegmentado _tolerancia;
        private readonly RotuloNordic _rotuloTolerancia;
        private readonly CardNordic _cardPP;
        private readonly CartaoEstado[] _cartoesPP;
        private readonly CardNordic _cardReles;
        private readonly Dictionary<int, BlocoRele> _reles = new();
        private readonly BotaoNordic[] _acoesReles;
        private readonly CardNordic _cardTerminal;
        private readonly SeletorNordic _portas;
        private readonly BotaoNordic _atualizarPortas;
        private readonly SeletorNordic _baud;
        private readonly BotaoNordic _conectar;
        private readonly LedNordic _ledConexao;
        private readonly RotuloNordic _statusConexao;
        private readonly TerminalView _terminal;
        private readonly CaixaTextoNordic _comando;
        private readonly SeletorNordic _fimDeLinha;
        private readonly BotaoNordic _enviar;
        private readonly BotaoNordic _limpar;
        private readonly BotaoNordic[] _acoesTerminal;
        private readonly BotaoNordic _resetTerminal;
        private readonly ToolTip _dicas = new();

        public TelaBancada()
        {
            // ---- Telemetria
            _hud = new CardNordic { PaddingConteudo = new Padding(20, 12, 20, 12) };
            var sobretitulo = Rotulo("MÓDULO DE TESTES HARDWARE-IN-THE-LOOP (HIL)", PapelTexto.Secundario, ThemeManager.FonteRotulo);
            var titulo = Rotulo("SIMULADOR EVSE & ESTADOS DE PILOTO IEC 61851", PapelTexto.Principal, ThemeManager.FonteTitulo);
            _leituraTensao = new LeituraTelemetria { Text = "TENSÃO CP", Nominal = true };
            _leituraCorrente = new LeituraTelemetria { Text = "CORR. MÁX. PP", Nominal = true };
            _leituraResistencia = new LeituraTelemetria { Text = "RESISTÊNCIA PP", Nominal = true };
            _leituraSincronismo = new LeituraTelemetria { Text = "SINCRONISMO" };
            _hud.Controls.AddRange(new Control[] { sobretitulo, titulo, _leituraTensao, _leituraCorrente, _leituraResistencia, _leituraSincronismo });
            _hud.Layout += (_, _) =>
            {
                int h = _hud.Height;
                int esq = Px(20);
                sobretitulo.Location = new Point(esq, h / 2 - sobretitulo.Height - Px(1));
                titulo.Location = new Point(esq, h / 2 + Px(1));
                int gap = Px(12), lh = Px(50);
                int lw = Math.Clamp((_hud.Width - Px(20) - Px(440) - gap * 3) / 4, Px(132), Px(190));
                int x = _hud.Width - Px(20) - lw;
                foreach (LeituraTelemetria l in new[] { _leituraSincronismo, _leituraResistencia, _leituraCorrente, _leituraTensao })
                {
                    l.SetBounds(x, (h - lh) / 2, lw, lh);
                    x -= lw + gap;
                }
            };
            _dicas.SetToolTip(_leituraTensao, "Tensão nominal do estado CP selecionado (o firmware não mede a tensão).");
            _dicas.SetToolTip(_leituraCorrente, "Corrente máxima nominal do cabo PP selecionado (IEC 62196-2); — quando o canal não codifica corrente.");
            _dicas.SetToolTip(_leituraResistencia, "Resistência nominal do cabo PP selecionado.");

            // ---- Coluna CP
            _cardCP = new CardNordic { Titulo = "CONTROL PILOT (CP)", Selo = "IEC 61851" };
            _cartoesCP = new CartaoEstado[Grupos.Length];
            for (int i = 0; i < Grupos.Length; i++)
            {
                GrupoEstadoCP g = Grupos[i];
                EstadoVeiculoCP nominal = EstadoVeiculoCPExtensions.ObterEstado(g, VariacaoEstadoCP.Nominal);
                var c = new CartaoEstado
                {
                    Indice = i,
                    Text = $"[ESTADO {g}] {RotuloGrupo(g)}",
                    Subtitulo = $"{FormatarTensao(nominal.ObterTensaoNominal())} | {nominal.ObterDescricao()}",
                    Perigo = g == GrupoEstadoCP.E,
                    AccessibleName = $"Estado {g}: {RotuloGrupo(g)}"
                };
                c.Escolhido += CartaoCP_Escolhido;
                _dicas.SetToolTip(c, nominal.ObterDescricao());
                _cartoesCP[i] = c;
            }
            _rotuloTolerancia = RotuloCampo("Tolerância / variação de carga");
            _tolerancia = new SeletorSegmentado { AccessibleName = "Tolerância do estado CP" };
            _tolerancia.DefinirOpcoes("SUP", "NOMINAL", "INF");
            _tolerancia.SegmentoClicado += Tolerancia_Clicada;
            _dicas.SetToolTip(_tolerancia, "Limites de tolerância: só nos estados B, C e D. Clicar na variação ativa reenvia o comando.");
            _cardCP.Controls.AddRange(_cartoesCP);
            _cardCP.Controls.Add(_rotuloTolerancia);
            _cardCP.Controls.Add(_tolerancia);
            _cardCP.Layout += (_, _) => EmpilharCartoes(_cardCP, _cartoesCP, rodape: true);

            // ---- Coluna PP
            _cardPP = new CardNordic { Titulo = "PROXIMITY PILOT (PP)", Selo = "RESISTOR DE CABO" };
            _cartoesPP = new CartaoEstado[Cabos.Length];
            for (int i = 0; i < Cabos.Length; i++)
            {
                EstadoCaboPP cabo = Cabos[i];
                var c = new CartaoEstado
                {
                    Indice = i,
                    Text = $"[PP{(int)cabo}] {NomeCurto(cabo)}" + (cabo.ForaDaNorma() ? " *" : ""),
                    Subtitulo = SubtituloCabo(cabo),
                    Perigo = cabo == EstadoCaboPP.FalhaCurto,
                    AccessibleName = $"Cabo {(int)cabo}: {cabo.ObterDescricao()}"
                };
                c.Escolhido += CartaoPP_Escolhido;
                _dicas.SetToolTip(c, cabo.ForaDaNorma()
                    ? $"Resistor de {cabo.ObterResistenciaNominal()} Ω: {EstadoCaboPPExtensions.NotaValorDaBancada}."
                    : cabo.ObterDescricao());
                _cartoesPP[i] = c;
            }
            _cardPP.Controls.AddRange(_cartoesPP);
            _cardPP.Layout += (_, _) => EmpilharCartoes(_cardPP, _cartoesPP, rodape: false);

            // ---- Relés
            _cardReles = new CardNordic
            {
                Titulo = "MATRIZ DE RELÉS DE COMUTAÇÃO",
                Selo = "D22–D31 (LEITURA)"
            };
            foreach (int pino in PinosNaOrdem)
            {
                var b = new BlocoRele
                {
                    Pino = $"PIN D{pino}",
                    Text = (RelesCP.ObterNome(pino) ?? "").ToUpperInvariant(),
                    Perigo = pino == 31
                };
                _dicas.SetToolTip(b, FuncaoRele.TryGetValue(pino, out string? f) ? $"Relé D{pino} ({f})" : $"Relé D{pino}");
                _reles[pino] = b;
                _cardReles.Controls.Add(b);
            }
            _acoesReles = new[]
            {
                AcaoFirmware("STATUS CP", Icone.Nenhum, "Pede ao firmware o estado CP atual e os relés ativos (sem alterar a seleção).", (_, _) => _engine.SolicitarStatusCP()),
                AcaoFirmware("STATUS PP", Icone.Nenhum, "Consulta o canal do cabo PP ativo no firmware.", (_, _) => _engine.SolicitarStatusCabo()),
                AcaoFirmware("STACK", Icone.Nenhum, "Inspeciona a margem de segurança da pilha das tarefas FreeRTOS.", (_, _) => _engine.SolicitarDebugStack()),
                AcaoFirmware("RESET CP (A)", Icone.Nenhum, "Volta o Control Pilot para o estado A (+12 V, desconectado). O cabo PP não muda.", (_, _) => ResetCP(), EstiloBotao.Perigo)
            };
            _cardReles.Controls.AddRange(_acoesReles);
            _cardReles.Layout += (_, _) => PosicionarReles();

            // ---- Terminal (com a linha de conexão no cabeçalho)
            _cardTerminal = new CardNordic { Titulo = "TERMINAL SERIAL DA BANCADA", PaddingConteudo = new Padding(14, 10, 14, 14) };
            _ledConexao = new LedNordic();
            _statusConexao = Rotulo("Desconectado", PapelTexto.Secundario, ThemeManager.FontePequena);
            _portas = new SeletorNordic { AccessibleName = "Porta serial da bancada" };
            _atualizarPortas = Botao("", EstiloBotao.Secundario, Icone.Atualizar, (_, _) => CarregarPortas());
            _atualizarPortas.AccessibleName = "Atualizar portas";
            _dicas.SetToolTip(_atualizarPortas, "Atualizar a lista de portas COM");
            _baud = new SeletorNordic { AccessibleName = "Baud rate da bancada", Formatar = o => $"{o} bps" };
            _conectar = Botao("Conectar", EstiloBotao.Primario, Icone.Plugue, (_, _) => AlternarConexao());
            _cardTerminal.Controls.AddRange(new Control[] { _ledConexao, _statusConexao, _portas, _atualizarPortas, _baud, _conectar });

            _terminal = new TerminalView
            {
                Titulo = "COMUNICAÇÃO SERIAL BANCADA ENGINE [DESACOPLADA 50 MS]",
                InfoCabecalho = "Dispositivo: Arduino Mega (EVSE HIL)",
                NomeAcessivelLog = "Monitor serial da bancada"
            };
            _comando = new CaixaTextoNordic
            {
                PlaceholderText = "Comando manual (ex.: estado cx, cabo 3, help, debug stack)...",
                InnerFont = ThemeManager.FonteMono,
                NomeAcessivel = "Comando manual"
            };
            _comando.InnerKeyDown += Comando_KeyDown;
            _comando.InnerTextChanged += (_, _) =>
            {
                _comando.Erro = false;
                if (!_navegando) _historico.ResetarNavegacao();
            };
            _fimDeLinha = TelaSupervisorio.CriarSeletorFimDeLinha();
            _enviar = Botao("Enviar", EstiloBotao.Primario, Icone.Enviar, (_, _) => EnviarComando());
            _limpar = Botao("Limpar", EstiloBotao.Secundario, Icone.Lixeira, (_, _) => _terminal.Limpar());
            _dicas.SetToolTip(_limpar, "Limpa o histórico do monitor serial.");

            _acoesTerminal = new[]
            {
                AcaoFirmware("HELP", Icone.Livro, "Solicita ao firmware a lista de comandos seriais disponíveis.", (_, _) => _engine.SolicitarHelp()),
                AcaoFirmware("STATUS CP", Icone.Raio, "Pede ao firmware o estado CP atual e os relés ativos (sem alterar a seleção).", (_, _) => _engine.SolicitarStatusCP()),
                AcaoFirmware("STATUS PP", Icone.Plugue, "Consulta o canal do cabo PP ativo no firmware.", (_, _) => _engine.SolicitarStatusCabo()),
                AcaoFirmware("STACK", Icone.Pilha, "Inspeciona a margem de segurança da pilha das tarefas FreeRTOS.", (_, _) => _engine.SolicitarDebugStack())
            };
            _resetTerminal = AcaoFirmware("RESET CP (ESTADO A)", Icone.Reset,
                "Volta o Control Pilot para o estado A (+12 V, desconectado). O cabo PP não muda.", (_, _) => ResetCP(), EstiloBotao.Perigo);

            _cardTerminal.Controls.AddRange(new Control[] { _terminal, _comando, _fimDeLinha, _enviar, _limpar, _resetTerminal });
            _cardTerminal.Controls.AddRange(_acoesTerminal);
            var rotuloFim = Rotulo("Fim de linha:", PapelTexto.Secundario, ThemeManager.FonteRotulo);
            rotuloFim.Name = "rotuloFim";
            _cardTerminal.Controls.Add(rotuloFim);
            var divisoria = new Panel { Name = "divisoria", Height = 1 };
            divisoria.Paint += (_, e) => e.Graphics.Clear(ThemeManager.BordaSutil);
            _cardTerminal.Controls.Add(divisoria);
            _cardTerminal.Layout += (_, _) => PosicionarTerminal(rotuloFim, divisoria);

            Controls.AddRange(new Control[] { _hud, _cardCP, _cardPP, _cardReles, _cardTerminal });

            // ---- Estado inicial
            CarregarPortas();
            _baud.DefinirItens(new object[] { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 });
            _baud.SelectedItem = 9600;

            _timerRetentativaStatus.Tick += TimerRetentativaStatus_Tick;
            _engine.ComandoEnviado += Engine_ComandoEnviado;
            _engine.RespostaRecebida += Engine_RespostaRecebida;
            _engine.ComandoConcluido += Engine_ComandoConcluido;
            _engine.StatusCPRecebido += Engine_StatusCPRecebido;
            _engine.RelesCPAtualizados += Engine_RelesCPAtualizados;
            _engine.RespostaMultilinhaRecebida += Engine_RespostaMultilinhaRecebida;
            _engine.ConexaoPerdida += Engine_ConexaoPerdida;
            _engine.Reconectado += Engine_Reconectado;

            _carregandoUI = false;
            AtualizarEstadoUI(conectado: false);
            AtualizarSelecaoUI();
            Registrar(TipoLinha.Sistema, "Bancada Engine inicializada. Selecione a porta e conecte.");
        }


        private EstadoVeiculoCP EstadoSelecionadoCP => EstadoVeiculoCPExtensions.ObterEstado(_grupoCP, _variacaoCP);

        private BotaoNordic AcaoFirmware(string texto, Icone icone, string dica, EventHandler clique, EstiloBotao estilo = EstiloBotao.Secundario)
        {
            BotaoNordic b = Botao(texto, estilo, icone, clique);
            b.Font = ThemeManager.FonteBotaoPequeno;
            _dicas.SetToolTip(b, dica);
            return b;
        }

        private static string RotuloGrupo(GrupoEstadoCP grupo) => grupo switch
        {
            GrupoEstadoCP.A => "Desconectado",
            GrupoEstadoCP.B => "Conectado / Standby",
            GrupoEstadoCP.C => "Carga normal",
            GrupoEstadoCP.D => "Carga c/ ventilação",
            GrupoEstadoCP.E => "Falha / Curto CP-PE",
            _ => grupo.ToString()
        };

        /// <summary>Nome do cabo sem a resistência entre parênteses (vem do Core).</summary>
        private static string NomeCurto(EstadoCaboPP cabo)
        {
            string d = cabo.ObterDescricao();
            int p = d.IndexOf(" (", StringComparison.Ordinal);
            return p > 0 ? d[..p] : d;
        }

        private static string SubtituloCabo(EstadoCaboPP cabo)
        {
            int r = cabo.ObterResistenciaNominal();
            string texto = r < 0 ? "Sem cabo inserido (circuito aberto)" : $"Resistência nominal: {r} Ω";
            return cabo.ForaDaNorma() ? $"{texto} · {EstadoCaboPPExtensions.NotaValorDaBancada}" : texto;
        }

        private static string FormatarTensao(double volts) =>
            $"{volts.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture)} V";

        // ------------------------------------------------------------------ Conexão

        private void CarregarPortas()
        {
            string anterior = _portas.TextoSelecionado;
            _portas.DefinirItens(_engine.ObterPortasDisponiveis());
            if (_portas.Itens.Count == 0) return;
            _portas.SelectedItem = _portas.Itens.FirstOrDefault(p => string.Equals(p as string, anterior, StringComparison.OrdinalIgnoreCase));
            if (_portas.SelectedIndex < 0) _portas.SelectedIndex = 0;
        }

        private void AlternarConexao()
        {
            if (_engine.EstaConectado || _engine.TentandoReconectar)
            {
                CancelarRetentativaStatus();
                _engine.Desconectar();
                AtualizarEstadoUI(conectado: false);
                Registrar(TipoLinha.Aviso, "Porta serial desconectada.");
                return;
            }

            string porta = _portas.TextoSelecionado;
            if (string.IsNullOrWhiteSpace(porta))
            {
                Toast.Mostrar(this, "Nenhuma porta COM selecionada. Conecte a bancada e clique em atualizar.", TipoToast.Aviso);
                return;
            }

            try
            {
                int baud = Convert.ToInt32(_baud.SelectedItem, CultureInfo.InvariantCulture);
                _engine.Conectar(porta, baud);
                AtualizarEstadoUI(conectado: true);
                Registrar(TipoLinha.Sistema, $"Conectado à porta {porta} @ {baud} bps.");
                SincronizarComFirmware();
            }
            catch (Exception ex)
            {
                AtualizarEstadoUI(conectado: false);
                Registrar(TipoLinha.Erro, $"Falha ao abrir {porta}: {ex.Message}");
                Toast.Mostrar(this, $"Não foi possível abrir {porta}: {ex.Message}", TipoToast.Erro);
            }
        }

        /// <summary>Ao conectar/reconectar, só PEDE o status ao firmware: a seleção local não é enviada.</summary>
        private void SincronizarComFirmware()
        {
            CancelarRetentativaStatus();
            Registrar(TipoLinha.Sistema, "Sincronizando: pedindo o status do CP ao firmware (a seleção local não é enviada).");
            _engine.SolicitarStatusCP();
            _idStatusInicial = _engine.UltimoIdComando;
            _retentativaStatusFeita = false;
        }

        private void TimerRetentativaStatus_Tick(object? sender, EventArgs e)
        {
            _timerRetentativaStatus.Stop();
            if (!PodeAtualizarUI || !_engine.EstaConectado) return;

            Registrar(TipoLinha.Sistema, "Nova tentativa: pedindo o status do CP ao firmware.");
            _engine.SolicitarStatusCP();
            _idStatusInicial = 0; // só uma nova tentativa
        }

        private void CancelarRetentativaStatus()
        {
            _timerRetentativaStatus.Stop();
            _idStatusInicial = 0;
        }

        private void AtualizarEstadoUI(bool conectado, bool reconectando = false)
        {
            _conectar.Text = conectado || reconectando ? "Desconectar" : "Conectar";
            _conectar.Estilo = conectado || reconectando ? EstiloBotao.Perigo : EstiloBotao.Primario;
            _portas.Enabled = !conectado && !reconectando;
            _baud.Enabled = !conectado && !reconectando;
            _atualizarPortas.Enabled = !conectado && !reconectando;

            if (reconectando)
            {
                _ledConexao.Estado = EstadoLed.Ambar;
                _statusConexao.Text = "Reconectando...";
                _statusConexao.Papel = PapelTexto.Aviso;
            }
            else if (conectado)
            {
                _ledConexao.Estado = EstadoLed.Verde;
                _statusConexao.Text = $"{_portas.TextoSelecionado} @ {_baud.SelectedItem} bps";
                _statusConexao.Papel = PapelTexto.Sucesso;
            }
            else
            {
                ApagarReles();
                _ledConexao.Estado = EstadoLed.Apagado;
                _statusConexao.Text = "Desconectado";
                _statusConexao.Papel = PapelTexto.Secundario;
            }
            _terminal.EstadoLed = _ledConexao.Estado;
            _cardTerminal.PerformLayout();
        }

        // ------------------------------------------------------------------ Seleção e reenvio (CP e PP)

        private void CartaoCP_Escolhido(object? sender, SegmentoEventArgs e)
        {
            if (_carregandoUI) return;
            GrupoEstadoCP grupo = Grupos[e.Indice];
            bool reenvio = e.JaSelecionado && grupo == _grupoCP;
            if (grupo != _grupoCP)
            {
                _variacaoCP = VariacaoEstadoCP.Nominal; // troca de grupo volta à variação nominal
            }
            _grupoCP = grupo;
            EnviarEstadoSelecionado(reenvio);
        }

        private void Tolerancia_Clicada(object? sender, SegmentoEventArgs e)
        {
            if (_carregandoUI || !_grupoCP.TemVariacao()) return;
            VariacaoEstadoCP variacao = Variacoes[e.Indice];
            bool reenvio = variacao == _variacaoCP; // clicar na variação já ativa reenvia o comando
            _variacaoCP = variacao;
            EnviarEstadoSelecionado(reenvio);
        }

        private void CartaoPP_Escolhido(object? sender, SegmentoEventArgs e)
        {
            if (_carregandoUI) return;
            EstadoCaboPP cabo = Cabos[e.Indice];
            EnviarCabo(cabo, reenvio: cabo == _engine.CaboAtual && _situacaoPP != Situacao.Local);
        }

        /// <summary>"Reset CP (A)": só volta o CP para A; o PP não é tocado.</summary>
        private void ResetCP()
        {
            bool reenvio = _grupoCP == GrupoEstadoCP.A;
            _grupoCP = GrupoEstadoCP.A;
            _variacaoCP = VariacaoEstadoCP.Nominal;
            Registrar(TipoLinha.Aviso, $"Reset CP: Control Pilot de volta ao estado A (+12 V). Cabo PP mantido ({_engine.CaboAtual.ObterDescricao()}).");
            EnviarEstadoSelecionado(reenvio);
        }

        private void EnviarEstadoSelecionado(bool reenvio)
        {
            EstadoVeiculoCP estado = EstadoSelecionadoCP;
            if (reenvio) Registrar(TipoLinha.Sistema, $"Reenviando ao firmware: {estado.ObterComandoSerial()} (reenvio manual)");

            // Situação antes do Core: a conclusão (inclusive o NaoEnviado síncrono) nunca é sobrescrita
            DefinirSituacaoCP(Situacao.Aguardando, "Aguardando a confirmação do firmware.");
            _enviandoCP = true;
            try
            {
                _engine.DefinirEstado(estado); // NaoEnviado chega síncrono via ComandoConcluido
            }
            finally
            {
                _enviandoCP = false;
            }
            if (_situacaoCP == Situacao.Aguardando) _idPedidoCP = _engine.UltimoIdComando;
            AtualizarSelecaoUI();
        }

        private void EnviarCabo(EstadoCaboPP cabo, bool reenvio)
        {
            if (reenvio) Registrar(TipoLinha.Sistema, $"Reenviando ao firmware: {cabo.ObterComandoSerial()} (reenvio manual)");

            DefinirSituacaoPP(Situacao.Aguardando, "Aguardando a confirmação do firmware.");
            _enviandoPP = true;
            try
            {
                _engine.DefinirCabo(cabo);
            }
            finally
            {
                _enviandoPP = false;
            }
            if (_situacaoPP == Situacao.Aguardando) _idPedidoPP = _engine.UltimoIdComando;
            AtualizarSelecaoUI();
        }

        private void DefinirSituacaoCP(Situacao s, string detalhe)
        {
            _situacaoCP = s;
            _detalheCP = detalhe;
        }

        private void DefinirSituacaoPP(Situacao s, string detalhe)
        {
            _situacaoPP = s;
            _detalhePP = detalhe;
        }

        /// <summary>
        /// Cartões (seleção local + marca do firmware), tolerância, telemetria nominal e sincronismo.
        /// Sincronismo = CONFIRMADO só quando o firmware confirmou exatamente o CP e o PP selecionados.
        /// </summary>
        private void AtualizarSelecaoUI()
        {
            EstadoVeiculoCP selecionado = EstadoSelecionadoCP;
            EstadoVeiculoCP? cpFirmware = _engine.EstadoConfirmado;
            EstadoCaboPP? ppFirmware = _engine.CaboConfirmado;

            for (int i = 0; i < _cartoesCP.Length; i++)
            {
                _cartoesCP[i].Selecionado = Grupos[i] == _grupoCP;
                _cartoesCP[i].ConfirmadoFirmware = cpFirmware?.ObterGrupo() == Grupos[i];
            }
            for (int i = 0; i < _cartoesPP.Length; i++)
            {
                _cartoesPP[i].Selecionado = Cabos[i] == _engine.CaboAtual;
                _cartoesPP[i].ConfirmadoFirmware = ppFirmware == Cabos[i];
            }

            bool temVariacao = _grupoCP.TemVariacao();
            _tolerancia.Enabled = temVariacao;
            _rotuloTolerancia.Papel = temVariacao ? PapelTexto.Secundario : PapelTexto.Mudo;
            _tolerancia.SelectedIndex = Array.IndexOf(Variacoes, _variacaoCP);
            for (int i = 0; i < Variacoes.Length; i++)
            {
                string nome = Variacoes[i] switch
                {
                    VariacaoEstadoCP.Superior => "SUP",
                    VariacaoEstadoCP.Inferior => "INF",
                    _ => "NOMINAL"
                };
                _tolerancia.DefinirTexto(i, temVariacao
                    ? $"{nome} {FormatarTensao(EstadoVeiculoCPExtensions.ObterEstado(_grupoCP, Variacoes[i]).ObterTensaoNominal())}"
                    : nome);
            }

            _leituraTensao.Valor = FormatarTensao(selecionado.ObterTensaoNominal());
            _leituraCorrente.Valor = _engine.CaboAtual.ObterCorrenteMaxima() is int amperes ? $"{amperes} A" : "—";
            int r = _engine.CaboAtual.ObterResistenciaNominal();
            _leituraResistencia.Valor = r < 0 ? "∞ Ω" : $"{r} Ω";

            bool cpOk = cpFirmware == selecionado;
            bool ppOk = ppFirmware == _engine.CaboAtual;
            bool sincronizado = cpOk && ppOk && _engine.EstaConectado;
            _leituraSincronismo.Valor = sincronizado ? "CONFIRMADO" : "PENDENTE";
            _leituraSincronismo.PapelValor = sincronizado ? PapelTexto.Sucesso
                : _situacaoCP is Situacao.ErroFirmware || _situacaoPP is Situacao.ErroFirmware ? PapelTexto.Perigo
                : PapelTexto.Aviso;

            string linhaCP = cpFirmware is { } cp
                ? $"CP: firmware em {cp.ObterCodigo()} ({FormatarTensao(cp.ObterTensaoNominal())}){(cpOk ? "" : $", seleção local {selecionado.ObterCodigo()}")}"
                : $"CP: sem confirmação do firmware (seleção local {selecionado.ObterCodigo()})";
            string linhaPP = ppFirmware is { } pp
                ? $"PP: firmware no canal {(int)pp}{(ppOk ? "" : $", seleção local {(int)_engine.CaboAtual}")}"
                : $"PP: sem confirmação do firmware (seleção local {(int)_engine.CaboAtual})";
            _dicas.SetToolTip(_leituraSincronismo,
                $"{linhaCP}\n  último pedido: {TextoSituacao(_situacaoCP)} — {_detalheCP}\n" +
                $"{linhaPP}\n  último pedido: {TextoSituacao(_situacaoPP)} — {_detalhePP}");
        }

        private static string TextoSituacao(Situacao s) => s switch
        {
            Situacao.Local => "nada enviado",
            Situacao.Aguardando => "aguardando confirmação",
            Situacao.Confirmado => "confirmado",
            Situacao.NaoEnviado => "não enviado",
            Situacao.ErroFirmware => "rejeitado pelo firmware",
            _ => "sem resposta"
        };

        private static Situacao SituacaoDe(ResultadoComando resultado) => resultado switch
        {
            ResultadoComando.Confirmado => Situacao.Confirmado,
            ResultadoComando.ErroFirmware => Situacao.ErroFirmware,
            ResultadoComando.NaoEnviado => Situacao.NaoEnviado,
            _ => Situacao.SemResposta
        };

        private static string DetalheDe(ConclusaoComando c) => c.Detalhe ?? c.Resultado switch
        {
            ResultadoComando.ErroFirmware when c.Resposta?.Motivo == MotivoErroFirmware.CPIndisponivel =>
                "O firmware informou que o CP não existe nesta placa (Nano). Use o Arduino Mega para emular o CP.",
            ResultadoComando.ErroFirmware => $"Resposta do firmware: {c.Resposta?.Linha}",
            ResultadoComando.NaoEnviado => BancadaEngine.DetalheSemConexao,
            ResultadoComando.SemResposta => "O firmware não respondeu no prazo: o estado real do hardware é desconhecido.",
            _ => $"Resposta do firmware: {c.Resposta?.Linha}"
        };

        // ------------------------------------------------------------------ Relés

        private void MostrarReles(IReadOnlyList<int> pinosAtivos)
        {
            foreach (var (pino, bloco) in _reles)
            {
                bloco.Ligado = pinosAtivos.Contains(pino);
            }
        }

        private void ApagarReles() => MostrarReles(Array.Empty<int>());

        // ------------------------------------------------------------------ Comando manual

        private void EnviarComando()
        {
            string cmd = _comando.Text.Trim();
            FimDeLinha fim = _fimDeLinha.SelectedItem is FimDeLinha f ? f : FimDeLinha.LF;

            // Campo vazio envia só o terminador; vazio sem terminador não tem o que transmitir
            if (cmd.Length == 0 && fim == FimDeLinha.Nenhum)
            {
                _comando.Erro = true;
                Toast.Mostrar(this, "Nada a enviar: digite um comando ou escolha um fim de linha.", TipoToast.Aviso);
                return;
            }

            // Validação local (mesma regra do Core): só ASCII imprimível; nada é transmitido
            if (cmd.Length > 0 && !BancadaEngine.ComandoManualValido(cmd))
            {
                _comando.Erro = true;
                Registrar(TipoLinha.Erro, $"Não enviado: {cmd} ({BancadaEngine.DetalheCaractereInvalido})");
                Toast.Mostrar(this, "Comando inválido: use apenas caracteres ASCII imprimíveis.", TipoToast.Aviso);
                return;
            }

            _manualPendente = (cmd, fim);
            _historico.Adicionar(cmd);
            // O texto fica na caixa depois do envio (pedido do usuário); em falha, o "não enviado" vem do ComandoConcluido
            _ = _engine.EnviarComandoManual(cmd, fim);
        }

        private void Comando_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                EnviarComando();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode is Keys.Up or Keys.Down)
            {
                TelaSupervisorio.NavegarHistorico(_historico, _comando, e.KeyCode == Keys.Up, ref _navegando);
                e.Handled = e.SuppressKeyPress = true;
            }
        }

        private void Registrar(TipoLinha tipo, string texto, string? fimDeLinha = null) =>
            _terminal.AdicionarLinha(tipo, texto, fimDeLinha);

        // ------------------------------------------------------------------ Eventos do motor (qualquer thread)

        /// <summary>true se já está na UI; false se a tela está fechando ou se a chamada foi reagendada na UI.</summary>
        private bool NaThreadDaUI(Action reexecutar)
        {
            if (!PodeAtualizarUI) return false;
            if (!InvokeRequired) return true;
            InvocarNaUI(reexecutar);
            return false;
        }

        private void Engine_ComandoEnviado(object? sender, string cmd)
        {
            if (!NaThreadDaUI(() => Engine_ComandoEnviado(sender, cmd))) return;

            // Comandos automáticos saem em LF; o manual leva o fim de linha escolhido
            FimDeLinha fim = FimDeLinha.LF;
            if (_manualPendente is { } m && m.Comando == cmd)
            {
                fim = m.Fim;
                _manualPendente = null;
            }
            Registrar(TipoLinha.Tx, cmd, fim.ObterRotulo());
        }

        private void Engine_RespostaRecebida(object? sender, RespostaFirmware r)
        {
            if (!NaThreadDaUI(() => Engine_RespostaRecebida(sender, r))) return;

            Registrar(r.EhErro ? TipoLinha.Aviso : TipoLinha.Rx, r.Linha);
            AtualizarSelecaoUI();
        }

        /// <summary>A verdade final de cada comando: só aqui a situação sai de "aguardando".</summary>
        private void Engine_ComandoConcluido(object? sender, ConclusaoComando c)
        {
            if (!NaThreadDaUI(() => Engine_ComandoConcluido(sender, c))) return;

            switch (c.Resultado)
            {
                case ResultadoComando.NaoEnviado:
                    Registrar(TipoLinha.Erro, $"Não enviado: {(c.Comando.Length == 0 ? "(linha vazia)" : c.Comando)} ({DetalheDe(c)})");
                    if (_manualPendente is { } m && m.Comando == c.Comando) _manualPendente = null;
                    break;
                case ResultadoComando.SemResposta:
                    Registrar(TipoLinha.Aviso, $"Sem confirmação: {c.Comando} ({DetalheDe(c)})");
                    break;
                case ResultadoComando.ErroFirmware when c.Resposta?.Motivo == MotivoErroFirmware.CPIndisponivel:
                    Registrar(TipoLinha.Aviso, "CP indisponível nesta placa (Nano): use o Arduino Mega para emular o CP.");
                    Toast.Mostrar(this, "CP indisponível nesta placa (Nano). Use o Arduino Mega para emular o CP.", TipoToast.Aviso);
                    break;
            }

            Situacao situacao = SituacaoDe(c.Resultado);
            string detalhe = DetalheDe(c);

            // Só a conclusão do último pedido da UI mexe na situação (conclusões mais velhas são ignoradas)
            if (c.Estado is not null && EhPedidoAtual(c, _idPedidoCP, _enviandoCP))
            {
                _idPedidoCP = 0;
                DefinirSituacaoCP(situacao, detalhe);
            }
            if (c.Cabo is not null && EhPedidoAtual(c, _idPedidoPP, _enviandoPP))
            {
                _idPedidoPP = 0;
                DefinirSituacaoPP(situacao, detalhe);
            }

            // Os relés ativos só vêm no [STATUS CP]: depois de cada OK de estado (da UI ou manual), pede o status,
            // a menos que um estado mais novo da UI ainda esteja pendente (o OK dele pedirá o status)
            if (c.Estado != null && c.Resultado == ResultadoComando.Confirmado && _idPedidoCP == 0 && _engine.EstaConectado)
            {
                _engine.SolicitarStatusCP();
            }

            // DTR: o status pedido ao conectar se perdeu no reinício da placa -> uma nova tentativa em 2,5 s
            if (c.Id == _idStatusInicial && c.Resultado == ResultadoComando.SemResposta
                && !_retentativaStatusFeita && _engine.EstaConectado)
            {
                _retentativaStatusFeita = true;
                _idStatusInicial = 0;
                Registrar(TipoLinha.Sistema, "Aguardando a placa reiniciar... (nova tentativa de status em 2,5 s)");
                _timerRetentativaStatus.Stop();
                _timerRetentativaStatus.Start();
            }

            AtualizarSelecaoUI();
        }

        private bool EhPedidoAtual(ConclusaoComando c, long idPedido, bool enviando) =>
            (idPedido != 0 && c.Id == idPedido) || (enviando && c.Id == _engine.UltimoIdComando);

        private void Engine_StatusCPRecebido(object? sender, EstadoVeiculoCP estado)
        {
            if (!NaThreadDaUI(() => Engine_StatusCPRecebido(sender, estado))) return;

            // Sincronização sem efeito colateral: a seleção local não é alterada
            if (estado != EstadoSelecionadoCP)
            {
                Registrar(TipoLinha.Sistema, $"Firmware em {estado.ObterCodigo()}; seleção local em {EstadoSelecionadoCP.ObterCodigo()} (não alterada).");
            }
            AtualizarSelecaoUI();
        }

        private void Engine_RelesCPAtualizados(object? sender, IReadOnlyList<int> pinos)
        {
            if (!NaThreadDaUI(() => Engine_RelesCPAtualizados(sender, pinos))) return;
            MostrarReles(pinos);
        }

        /// <summary>Resposta completa de help/debug stack: as linhas já saíram como [RX]; aqui só o resumo.</summary>
        private void Engine_RespostaMultilinhaRecebida(object? sender, RespostaMultilinha r)
        {
            if (!NaThreadDaUI(() => Engine_RespostaMultilinhaRecebida(sender, r))) return;

            if (r.Linhas.Count == 0)
            {
                Registrar(TipoLinha.Aviso, $"Sem resposta: {r.Comando} (nenhuma linha recebida no prazo)");
            }
            else if (r.Stack is { } s)
            {
                Registrar(TipoLinha.Sistema, $"[STACK] SerialCLI livre: {s.SerialCLILivre} de {s.SerialCLITotal} | " +
                                             $"Blink livre: {s.BlinkLivre} de {s.BlinkTotal}");
            }
            else
            {
                Registrar(TipoLinha.Sistema, $"{r.Comando}: {r.Linhas.Count} linha(s) recebida(s).");
            }
        }

        private void Engine_ConexaoPerdida(object? sender, EventArgs e)
        {
            if (!NaThreadDaUI(() => Engine_ConexaoPerdida(sender, e))) return;

            CancelarRetentativaStatus();
            ApagarReles();
            AtualizarEstadoUI(conectado: false, reconectando: true);
            Registrar(TipoLinha.Aviso, "Conexão interrompida. Tentando reconectar automaticamente...");
            AtualizarSelecaoUI();
        }

        private void Engine_Reconectado(object? sender, EventArgs e)
        {
            if (!NaThreadDaUI(() => Engine_Reconectado(sender, e))) return;

            AtualizarEstadoUI(conectado: true);
            Registrar(TipoLinha.Sistema, "Conexão com o Arduino restabelecida.");
            SincronizarComFirmware();
        }

        private bool PodeAtualizarUI => !IsDisposed && !Disposing && IsHandleCreated;

        private void InvocarNaUI(Action acao)
        {
            if (!PodeAtualizarUI) return;
            try
            {
                BeginInvoke(acao);
            }
            catch (InvalidOperationException)
            {
                // Handle destruído entre a checagem e o BeginInvoke (fechamento concorrente)
            }
        }

        // ------------------------------------------------------------------ Layout

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (_cardTerminal is null) return;

            int largura = ClientSize.Width - Padding.Horizontal;
            int x0 = Padding.Left + AutoScrollPosition.X;
            int y = Padding.Top + AutoScrollPosition.Y;
            int gap = Px(16);

            _hud.SetBounds(x0, y, largura, Px(78));
            y += Px(78) + gap;

            // Colunas 1 : 1 : 1,25 (como no protótipo)
            int util = largura - gap * 2;
            int c1 = (int)(util / 3.25), c3 = util - c1 * 2;
            int alturaColunas = Px(36) + Px(16) + 7 * Px(52) + 6 * Px(8) + Px(16);
            _cardCP.SetBounds(x0, y, c1, alturaColunas);
            _cardPP.SetBounds(x0 + c1 + gap, y, c1, alturaColunas);
            _cardReles.SetBounds(x0 + (c1 + gap) * 2, y, c3, alturaColunas);
            y += alturaColunas + Px(18);

            _cardTerminal.SetBounds(x0, y, largura, Px(470));
            y += Px(470);

            AutoScrollMinSize = new Size(Px(960), y - AutoScrollPosition.Y + Padding.Bottom);
        }

        private void EmpilharCartoes(CardNordic card, CartaoEstado[] cartoes, bool rodape)
        {
            int x = card.Padding.Left;
            int w = card.ClientSize.Width - card.Padding.Horizontal;
            int y = card.Padding.Top;
            int h = Px(52), gap = Px(8);
            foreach (CartaoEstado c in cartoes)
            {
                c.SetBounds(x, y, w, h);
                y += h + gap;
            }

            if (rodape)
            {
                y += Px(6);
                _rotuloTolerancia.Location = new Point(x, y);
                y += _rotuloTolerancia.Height + Px(8);
                _tolerancia.SetBounds(x, y, w, Px(34));
            }
        }

        private void PosicionarReles()
        {
            int x = _cardReles.Padding.Left;
            int w = _cardReles.ClientSize.Width - _cardReles.Padding.Horizontal;
            int y = _cardReles.Padding.Top;
            int gap = Px(10);
            int lw = (w - gap) / 2, lh = Px(46);
            for (int i = 0; i < PinosNaOrdem.Length; i++)
            {
                _reles[PinosNaOrdem[i]].SetBounds(x + (i % 2) * (lw + gap), y + (i / 2) * (lh + gap), lw, lh);
            }
            y += 5 * lh + 4 * gap + Px(16);

            int ga = Px(8);
            int la = (w - ga * 3) / 4;
            for (int i = 0; i < _acoesReles.Length; i++)
            {
                _acoesReles[i].SetBounds(x + i * (la + ga), y, la, Px(32));
            }
        }

        private void PosicionarTerminal(Control rotuloFim, Control divisoria)
        {
            CardNordic c = _cardTerminal;
            int esq = c.Padding.Left;
            int dir = c.ClientSize.Width - c.Padding.Right;
            int w = dir - esq;

            // Linha de conexão no cabeçalho (à direita do título)
            int hCab = Px(36);
            int yCab = Px(8) + (hCab - Px(8) - Px(30)) / 2;
            int x = dir;
            _conectar.SetBounds(x -= Px(130), yCab, Px(130), Px(30));
            _baud.SetBounds(x -= Px(8) + Px(120), yCab, Px(120), Px(30));
            _atualizarPortas.SetBounds(x -= Px(6) + Px(34), yCab, Px(34), Px(30));
            _portas.SetBounds(x -= Px(6) + Px(110), yCab, Px(110), Px(30));
            _statusConexao.Location = new Point(x - Px(12) - _statusConexao.Width, yCab + (Px(30) - _statusConexao.Height) / 2);
            _ledConexao.SetBounds(_statusConexao.Left - Px(18), yCab + (Px(30) - Px(14)) / 2, Px(14), Px(14));

            int y = c.Padding.Top;
            int alturaBarras = Px(36) + Px(10) + Px(1) + Px(10) + Px(32);
            int hTerm = c.ClientSize.Height - c.Padding.Bottom - y - Px(10) - alturaBarras;
            _terminal.SetBounds(esq, y, w, Math.Max(Px(120), hTerm));
            y = _terminal.Bottom + Px(10);

            int lEnviar = Px(110), lLimpar = Px(100), lFim = Px(140);
            int xb = dir;
            _limpar.SetBounds(xb -= lLimpar, y, lLimpar, Px(34));
            _enviar.SetBounds(xb -= Px(8) + lEnviar, y, lEnviar, Px(34));
            _fimDeLinha.SetBounds(xb -= Px(8) + lFim, y + Px(1), lFim, Px(32));
            rotuloFim.Location = new Point(xb - Px(6) - rotuloFim.Width, y + (Px(34) - rotuloFim.Height) / 2);
            _comando.SetBounds(esq, y, Math.Max(Px(100), rotuloFim.Left - Px(10) - esq), Px(34));
            y += Px(34) + Px(10);

            divisoria.SetBounds(esq, y, w, 1);
            y += 1 + Px(10);

            int xa = esq;
            foreach (BotaoNordic b in _acoesTerminal)
            {
                b.SetBounds(xa, y, Px(118), Px(32));
                xa += Px(118) + Px(8);
            }
            _resetTerminal.SetBounds(dir - Px(190), y, Px(190), Px(32));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timerRetentativaStatus.Stop();
                _timerRetentativaStatus.Tick -= TimerRetentativaStatus_Tick;
                _timerRetentativaStatus.Dispose();

                // Desassina antes do Dispose: nenhum callback da serial chega com a tela fechando
                _engine.ComandoEnviado -= Engine_ComandoEnviado;
                _engine.RespostaRecebida -= Engine_RespostaRecebida;
                _engine.ComandoConcluido -= Engine_ComandoConcluido;
                _engine.StatusCPRecebido -= Engine_StatusCPRecebido;
                _engine.RelesCPAtualizados -= Engine_RelesCPAtualizados;
                _engine.RespostaMultilinhaRecebida -= Engine_RespostaMultilinhaRecebida;
                _engine.ConexaoPerdida -= Engine_ConexaoPerdida;
                _engine.Reconectado -= Engine_Reconectado;
                _engine.Dispose();
                _dicas.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
