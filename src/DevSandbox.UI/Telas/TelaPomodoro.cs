using System;
using System.Drawing;
using System.Globalization;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DevSandbox.Core.Pomodoro;
using DevSandbox.UI.Common;

namespace DevSandbox.UI.Telas
{
    /// <summary>
    /// Pomodoro: modos (Foco 25, Pausa curta 5, Pausa longa 15), anel de progresso que esvazia
    /// (<see cref="PomodoroEngine.Progresso"/>), controles Iniciar/Pausar/Continuar + Resetar e estatísticas da sessão
    /// (<see cref="EstatisticasSessao"/>). Fim de ciclo: som + janela piscando na barra de tarefas, sem caixa modal.
    /// </summary>
    public sealed class TelaPomodoro : TelaBase
    {
        private static readonly (string Rotulo, string Estado, int Minutos, EstadoPomodoro Modo)[] Modos =
        {
            ("FOCO (25 MIN)", "Foco profundo", 25, EstadoPomodoro.Foco),
            ("PAUSA CURTA (5 MIN)", "Pausa curta", 5, EstadoPomodoro.DescansoCurto),
            ("PAUSA LONGA (15 MIN)", "Pausa longa", 15, EstadoPomodoro.DescansoLongo)
        };

        private readonly PomodoroEngine _engine = new();
        private readonly EstatisticasSessao _estatisticas;
        private int _modo;

        private readonly SeletorSegmentado _modos;
        private readonly ProgressRingControl _anel;
        private readonly BotaoNordic _iniciar;
        private readonly BotaoNordic _resetar;
        private readonly CardNordic _faixaEstatisticas;
        private readonly RotuloNordic _valorCiclo;
        private readonly RotuloNordic _valorFoco;
        private readonly RotuloNordic _valorInterrupcoes;
        private readonly Panel _palco;

        public TelaPomodoro()
        {
            _estatisticas = new EstatisticasSessao(_engine);

            _modos = new SeletorSegmentado { EmCartao = true, AccessibleName = "Modo do ciclo" };
            _modos.DefinirOpcoes(Array.ConvertAll(Modos, m => m.Rotulo));
            _modos.SelectedIndex = 0;
            _modos.SegmentoClicado += (_, e) => EscolherModo(e.Indice);

            _anel = new ProgressRingControl();
            _iniciar = Botao("INICIAR SESSÃO", EstiloBotao.Primario, Icone.Play, (_, _) => IniciarOuPausar());
            _iniciar.Font = ThemeManager.FonteUiForte;
            _resetar = Botao("RESETAR", EstiloBotao.Secundario, Icone.Reset, (_, _) => Resetar());

            _faixaEstatisticas = new CardNordic { PaddingConteudo = new Padding(12) };
            var grade = Grade(3, 2);
            grade.Dock = DockStyle.Fill;
            for (int i = 0; i < 3; i++) grade.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            grade.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            grade.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            _valorCiclo = AdicionarEstatistica(grade, 0, "CICLO ATUAL", PapelTexto.Sucesso);
            _valorFoco = AdicionarEstatistica(grade, 1, "TEMPO TOTAL EM FOCO", PapelTexto.Acento);
            _valorInterrupcoes = AdicionarEstatistica(grade, 2, "INTERRUPÇÕES", PapelTexto.Mudo);
            _faixaEstatisticas.Controls.Add(grade);

            _palco = PainelTransparente();
            _palco.Controls.AddRange(new Control[] { _modos, _anel, _iniciar, _resetar, _faixaEstatisticas });
            _palco.Layout += (_, _) => PosicionarPalco();
            Controls.Add(_palco);

            _engine.OnTick += Engine_OnTick;
            _engine.OnEstadoMudou += Engine_OnEstadoMudou;
            _estatisticas.Mudou += Estatisticas_Mudou;

            MostrarParado();
            AtualizarEstatisticas();
        }

        private static RotuloNordic AdicionarEstatistica(TableLayoutPanel grade, int coluna, string titulo, PapelTexto papel)
        {
            var rotulo = Rotulo(titulo, PapelTexto.Secundario, ThemeManager.FonteRotulo, autoSize: false);
            rotulo.Dock = DockStyle.Fill;
            rotulo.TextAlign = ContentAlignment.BottomCenter;
            var valor = Rotulo("", papel, ThemeManager.FonteMonoValor, autoSize: false);
            valor.Dock = DockStyle.Fill;
            valor.TextAlign = ContentAlignment.TopCenter;
            grade.Controls.Add(rotulo, coluna, 0);
            grade.Controls.Add(valor, coluna, 1);
            return valor;
        }

        public override void AoAtivar() => _iniciar.Focus();

        // ------------------------------------------------------------------ Ações

        private void EscolherModo(int indice)
        {
            // Trocar de modo encerra o ciclo atual (conta como interrupção se era foco — regra do Core)
            if (_engine.EstaRodando) _engine.Parar();
            _modo = indice;
            MostrarParado();
        }

        private void IniciarOuPausar()
        {
            if (!_engine.EstaRodando)
            {
                var m = Modos[_modo];
                _engine.Iniciar(TimeSpan.FromMinutes(m.Minutos), m.Modo);
                MostrarRodando();
            }
            else if (_engine.EstaPausado)
            {
                _engine.Retomar();
                MostrarRodando();
            }
            else
            {
                _engine.Pausar();
                MostrarPausado();
            }
        }

        private void Resetar()
        {
            _engine.Parar();
            MostrarParado();
        }

        // ------------------------------------------------------------------ Apresentação

        private void MostrarParado()
        {
            var m = Modos[_modo];
            _modos.SelectedIndex = _modo;
            _anel.Text = $"{m.Minutos:00}:00";
            _anel.Progresso = 1.0;
            _anel.Rotulo = m.Estado;
            _anel.PapelCor = CorDoModo(m.Modo);
            _iniciar.Text = "INICIAR SESSÃO";
            _iniciar.Icone = Icone.Play;
            _resetar.Enabled = false;
        }

        private void MostrarRodando()
        {
            var m = Modos[_modo];
            _anel.Rotulo = m.Estado;
            _anel.PapelCor = CorDoModo(m.Modo);
            _iniciar.Text = "PAUSAR";
            _iniciar.Icone = Icone.Pausa;
            _resetar.Enabled = true;
        }

        private void MostrarPausado()
        {
            _anel.Rotulo = "Pausado";
            _anel.PapelCor = PapelTexto.Secundario;
            _iniciar.Text = "CONTINUAR";
            _iniciar.Icone = Icone.Play;
            _resetar.Enabled = true;
        }

        private static PapelTexto CorDoModo(EstadoPomodoro modo) =>
            modo == EstadoPomodoro.Foco ? PapelTexto.Acento : PapelTexto.Sucesso;

        private void AtualizarEstatisticas()
        {
            _valorCiclo.Text = $"{_estatisticas.CicloAtual} / {EstatisticasSessao.CiclosPorSerie}";
            TimeSpan foco = _estatisticas.TempoEmFoco;
            _valorFoco.Text = $"{(int)foco.TotalHours:00}h {foco.Minutes:00}m";
            _valorInterrupcoes.Text = _estatisticas.Interrupcoes.ToString(CultureInfo.InvariantCulture);
        }

        // ------------------------------------------------------------------ Eventos do motor (thread do timer)

        private bool PodeAtualizarUI => !IsDisposed && !Disposing && IsHandleCreated;

        private void NaUI(Action acao)
        {
            if (!PodeAtualizarUI) return;
            if (!InvokeRequired)
            {
                acao();
                return;
            }
            try
            {
                BeginInvoke(acao);
            }
            catch (InvalidOperationException)
            {
                // Handle destruído durante o fechamento
            }
        }

        private void Engine_OnTick(TimeSpan restante) => NaUI(() =>
        {
            if (!_engine.EstaRodando) return;
            _anel.Text = restante.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
            _anel.Progresso = _engine.Progresso;
            AtualizarEstatisticas(); // tempo em foco corre durante o foco
        });

        private void Estatisticas_Mudou(object? sender, EventArgs e) => NaUI(AtualizarEstatisticas);

        private void Engine_OnEstadoMudou(EstadoPomodoro estadoFinalizado) => NaUI(() => TratarMudancaDeEstado(estadoFinalizado));

        private void TratarMudancaDeEstado(EstadoPomodoro estadoFinalizado)
        {
            if (estadoFinalizado == EstadoPomodoro.Pausado) return;

            // Se um novo ciclo começou entre o fim no motor e este BeginInvoke, o aviso está obsoleto
            if (_engine.EstaRodando) return;

            MostrarParado();
            AtualizarEstatisticas();
            Update();

            try
            {
                SystemSounds.Asterisk.Play();
            }
            catch (InvalidOperationException)
            {
                // Sem dispositivo de som
            }

            Toast.Mostrar(this, $"Ciclo concluído: {NomeDoCiclo(estadoFinalizado)}.", TipoToast.Sucesso);

            Form? janela = FindForm();
            if (janela is not null && Form.ActiveForm != janela)
            {
                PiscarJanela(janela);
            }
        }

        private static string NomeDoCiclo(EstadoPomodoro estado) => estado switch
        {
            EstadoPomodoro.Foco => "Foco",
            EstadoPomodoro.DescansoCurto => "Pausa curta",
            EstadoPomodoro.DescansoLongo => "Pausa longa",
            _ => estado.ToString()
        };

        // --- Aviso não bloqueante via FlashWindowEx (pisca até a janela voltar ao primeiro plano) ---
        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        private const uint FLASHW_ALL = 0x00000003;
        private const uint FLASHW_TIMERNOFG = 0x0000000C;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        private static void PiscarJanela(Form janela)
        {
            var info = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
                hwnd = janela.Handle,
                dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG,
                uCount = uint.MaxValue,
                dwTimeout = 0
            };
            _ = FlashWindowEx(ref info); // recurso cosmético: o retorno não importa
        }

        // ------------------------------------------------------------------ Layout

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (_palco is null) return;
            int largura = Math.Min(ClientSize.Width - Padding.Horizontal, Px(560));
            int altura = Px(560);
            _palco.SetBounds(Padding.Left + AutoScrollPosition.X + (ClientSize.Width - Padding.Horizontal - largura) / 2,
                             Padding.Top + AutoScrollPosition.Y + Px(8), largura, altura);
            AutoScrollMinSize = new Size(0, altura + Padding.Vertical + Px(8));
        }

        private void PosicionarPalco()
        {
            int w = _palco.Width;
            int y = 0;
            int larguraModos = Math.Min(w, Px(470));
            _modos.SetBounds((w - larguraModos) / 2, y, larguraModos, Px(44));
            y += Px(44) + Px(24);

            int anel = Px(250);
            _anel.SetBounds((w - anel) / 2, y, anel, anel);
            y += anel + Px(24);

            int lb = Px(190), lr = Px(130), hb = Px(40), gap = Px(12);
            int x = (w - lb - gap - lr) / 2;
            _iniciar.SetBounds(x, y, lb, hb);
            _resetar.SetBounds(x + lb + gap, y, lr, hb);
            y += hb + Px(24);

            _faixaEstatisticas.SetBounds(0, y, w, Px(72));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _engine.OnTick -= Engine_OnTick;
                _engine.OnEstadoMudou -= Engine_OnEstadoMudou;
                _estatisticas.Mudou -= Estatisticas_Mudou;
                _estatisticas.Dispose();
                _engine.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
