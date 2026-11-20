using System;
using System.Drawing;
using System.Windows.Forms;
using DevSandbox.UI.Common;

namespace DevSandbox.UI.Telas
{
    /// <summary>
    /// Aba Início: título, descrição e um cartão por ferramenta. Clicar no cartão (ou no botão) leva à aba
    /// correspondente (<see cref="AbrirTela"/> com o índice da aba).
    /// </summary>
    public sealed class TelaInicio : TelaBase
    {
        private readonly RotuloNordic _titulo;
        private readonly RotuloNordic _descricao;
        private readonly CardNordic[] _cards;
        private readonly CardNordic _rodape;

        /// <summary>Índice da aba a abrir (1 = Supervisório … 4 = Pomodoro).</summary>
        public event EventHandler<int>? AbrirTela;

        public TelaInicio()
        {
            _titulo = Rotulo("Central de Utilitários de Engenharia & Bancada", PapelTexto.Principal, ThemeManager.FonteDestaque, autoSize: false);
            _titulo.TextAlign = ContentAlignment.MiddleCenter;
            _descricao = Rotulo("Ambiente unificado para depuração de firmwares FreeRTOS, emulação veicular IEC 61851-1, " +
                                "cálculos de hardware e instrumentação.", PapelTexto.Secundario, ThemeManager.FonteUi, autoSize: false);
            _descricao.TextAlign = ContentAlignment.TopCenter;

            _cards = new[]
            {
                CriarCard(1, "Supervisório Serial", Icone.Plugue, "COM / UART", TipoSelo.Neutro,
                    "Comunicação serial com microcontroladores (AVR / ESP32), reconexão automática, watchdog e terminal colorido.",
                    "Abrir Supervisório"),
                CriarCard(2, "Bancada EVSE", Icone.Raio, "IEC 61851-1", TipoSelo.Sucesso,
                    "Emulação dos estados do CP (A–E) e do cabo PP (0–6), matriz de 10 relés (D22–D31) confirmada pelo firmware e terminal serial.",
                    "Abrir Bancada"),
                CriarCard(3, "Calculadora Hex & Bits", Icone.Hash, "64 bits", TipoSelo.Neutro,
                    "Conversor com detecção automática de base, matriz de 64 bits clicável, operações bitwise, Swap Endian e cópia rápida.",
                    "Abrir Calculadora"),
                CriarCard(4, "Pomodoro Timer", Icone.Relogio, "TimeProvider", TipoSelo.Neutro,
                    "Temporizador com anel de progresso, modos de foco e pausa e estatísticas da sessão.",
                    "Abrir Pomodoro")
            };

            _rodape = new CardNordic { PaddingConteudo = new Padding(18, 0, 18, 0) };
            var info = Rotulo("Ambiente: .NET 10 (WinForms)    ·    Núcleo: DevSandbox.Core (net10.0, sem dependência visual)",
                              PapelTexto.Secundario, ThemeManager.FontePequena, autoSize: false);
            info.Dock = DockStyle.Fill;
            info.TextAlign = ContentAlignment.MiddleLeft;
            _rodape.Controls.Add(info);

            Controls.Add(_titulo);
            Controls.Add(_descricao);
            Controls.AddRange(_cards);
            Controls.Add(_rodape);
        }

        private CardNordic CriarCard(int aba, string titulo, Icone icone, string selo, TipoSelo tipo, string descricao, string acao)
        {
            var card = new CardNordic
            {
                Titulo = titulo,
                IconeTitulo = icone,
                Selo = selo,
                TipoSelo = tipo,
                Clicavel = true
            };

            var desc = Rotulo(descricao, PapelTexto.Secundario, ThemeManager.FontePequena, autoSize: false);
            desc.Dock = DockStyle.Fill;
            desc.Cursor = Cursors.Hand;
            var botao = Botao(acao, EstiloBotao.Primario);
            botao.Dock = DockStyle.Bottom;
            botao.Height = 34;

            void Abrir(object? s, EventArgs e) => AbrirTela?.Invoke(this, aba);
            card.Click += Abrir;
            desc.Click += Abrir;
            botao.Click += Abrir;
            desc.MouseEnter += (_, _) => card.Invalidate();

            card.Controls.Add(desc);
            card.Controls.Add(botao);
            return card;
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (_cards is null) return;

            int largura = ClientSize.Width - Padding.Horizontal;
            int x0 = Padding.Left + AutoScrollPosition.X;
            int y = Padding.Top + AutoScrollPosition.Y;

            _titulo.SetBounds(x0, y, largura, Px(36));
            y += Px(40);
            int larguraDesc = Math.Min(largura, Px(640));
            _descricao.SetBounds(x0 + (largura - larguraDesc) / 2, y, larguraDesc, Px(42));
            y += Px(42) + Px(24);

            int gap = Px(18);
            int colunas = largura >= Px(1000) ? 4 : 2;
            int larguraCard = (largura - gap * (colunas - 1)) / colunas;
            int alturaCard = Px(210);
            for (int i = 0; i < _cards.Length; i++)
            {
                int col = i % colunas;
                int lin = i / colunas;
                _cards[i].SetBounds(x0 + col * (larguraCard + gap), y + lin * (alturaCard + gap), larguraCard, alturaCard);
                _cards[i].Controls[1].Height = Px(34);
            }
            int linhas = (_cards.Length + colunas - 1) / colunas;
            y += linhas * alturaCard + (linhas - 1) * gap + Px(24);

            _rodape.SetBounds(x0, y, largura, Px(46));
            y += Px(46);

            AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y + Padding.Bottom);
        }
    }
}
