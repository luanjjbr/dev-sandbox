using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevSandbox.Core.Converters;
using DevSandbox.UI.Common;

namespace DevSandbox.UI.Telas
{
    /// <summary>
    /// Calculadora Hex &amp; Bits (64 bits): entrada com detecção de base (Core), quatro vistas clicáveis para copiar
    /// (Hex, UInt64, Int64, ASCII), matriz 4×16 clicável e operações de engenharia. Toda conversão e operação de bits
    /// é do Core (<see cref="CalculadoraHex"/> e <see cref="OperacoesBits"/>); a tela só apresenta.
    /// </summary>
    public sealed class TelaCalculadora : TelaBase
    {
        private readonly CalculadoraHex _calculadora = new();
        private ulong _valor = 0xDEADBEEFCAFE0001UL;
        private bool _sincronizandoEntrada;

        private readonly CardNordic _cardEntrada;
        private readonly SeloNordic _seloBase;
        private readonly CaixaTextoNordic _entrada;
        private readonly (CardNordic Card, RotuloNordic Titulo, RotuloNordic Valor, string TituloOriginal)[] _vistas;
        private readonly CardNordic _cardMatriz;
        private readonly BitMatrixControl _matriz;
        private readonly CardNordic _cardOperacoes;
        private readonly BotaoNordic[] _operacoes;
        private readonly ToolTip _dicas = new();

        // Vistas com feedback "Copiado" em andamento: evita reentrância (duplo clique) por vista
        private readonly HashSet<CardNordic> _copiando = new();

        public TelaCalculadora()
        {
            // ---- Entrada
            _cardEntrada = new CardNordic { PaddingConteudo = new Padding(20, 16, 20, 16) };
            var rotuloEntrada = Rotulo("ENTRADA NUMÉRICA (DETECTA HEXADECIMAL '0x', BINÁRIO '0b' OU DECIMAL — 64 BITS)",
                                       PapelTexto.Secundario, ThemeManager.FonteRotulo, autoSize: false);
            rotuloEntrada.TextAlign = ContentAlignment.MiddleLeft;
            _seloBase = new SeloNordic();
            _entrada = new CaixaTextoNordic
            {
                Destaque = true,
                InnerFont = ThemeManager.FonteMonoHero,
                NomeAcessivel = "Entrada numérica",
                PlaceholderText = "0x0000000000000000"
            };
            _entrada.InnerTextChanged += (_, _) => AoDigitar();
            _entrada.InnerKeyDown += Entrada_KeyDown;
            _dicas.SetToolTip(_seloBase, "Base detectada pelo Core. Enter confirma; entrada inválida fica com borda vermelha.");

            var gradeEntrada = Grade(2, 2);
            gradeEntrada.Dock = DockStyle.Fill;
            gradeEntrada.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            gradeEntrada.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            gradeEntrada.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            gradeEntrada.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            rotuloEntrada.Dock = DockStyle.Fill;
            _seloBase.Dock = DockStyle.Fill;
            _entrada.Dock = DockStyle.Fill;
            _entrada.Margin = new Padding(0, 8, 0, 0);
            gradeEntrada.Controls.Add(rotuloEntrada, 0, 0);
            gradeEntrada.Controls.Add(_seloBase, 1, 0);
            gradeEntrada.Controls.Add(_entrada, 0, 1);
            gradeEntrada.SetColumnSpan(_entrada, 2);
            _cardEntrada.Controls.Add(gradeEntrada);

            // ---- Vistas (clique copia)
            _vistas = new[]
            {
                CriarVista("HEXADECIMAL (BASE 16)", PapelTexto.Acento),
                CriarVista("DECIMAL UNSIGNED (UINT64)", PapelTexto.Principal),
                CriarVista("DECIMAL SIGNED (INT64 COMPL. 2)", PapelTexto.Aviso),
                CriarVista("ASCII / 8 BYTES", PapelTexto.Sucesso)
            };

            // ---- Matriz
            _cardMatriz = new CardNordic
            {
                Titulo = "MATRIZ DE 64 BITS (4 LINHAS × 16 BITS — CLIQUE PARA COMUTAR QUALQUER BIT)",
                Selo = "MSB [63] TOPO ESQUERDO · LSB [0] INFERIOR DIREITO"
            };
            _matriz = new BitMatrixControl { Dock = DockStyle.Fill };
            _matriz.BitClicado += (_, bit) => DefinirValor(OperacoesBits.InverterBit(_valor, bit));
            _cardMatriz.Controls.Add(_matriz);

            // ---- Operações
            _cardOperacoes = new CardNordic { Titulo = "TRANSFORMAÇÕES DE ENGENHARIA & ENDIANNESS (64 BITS / QWORD)" };
            _operacoes = new[]
            {
                Operacao("SWAP ENDIAN (8 BYTES)", "Inverte a ordem dos 8 bytes (big-endian ↔ little-endian)", OperacoesBits.TrocarEndian),
                Operacao("SHIFT ESQ (<< 1)", "Deslocamento de 1 bit à esquerda (<< 1)", OperacoesBits.DeslocarEsquerda),
                Operacao("SHIFT DIR (>> 1)", "Deslocamento lógico de 1 bit à direita (>> 1)", OperacoesBits.DeslocarDireita),
                Operacao("INVERSÃO NOT (~)", "Inverte todos os 64 bits (~)", OperacoesBits.Not),
                Operacao("ZERAR (0x0)", "Zera todos os 64 bits (0x0000000000000000)", _ => 0UL),
                Operacao("MÁXIMO (0xFF..FF)", "Liga todos os 64 bits (0xFFFFFFFFFFFFFFFF)", _ => OperacoesBits.Maximo)
            };
            var gradeOps = Grade(_operacoes.Length, 1);
            gradeOps.Dock = DockStyle.Fill;
            for (int i = 0; i < _operacoes.Length; i++)
            {
                gradeOps.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / _operacoes.Length));
                _operacoes[i].Dock = DockStyle.Fill;
                _operacoes[i].Margin = new Padding(i == 0 ? 0 : 5, 0, i == _operacoes.Length - 1 ? 0 : 5, 0);
                gradeOps.Controls.Add(_operacoes[i], i, 0);
            }
            _cardOperacoes.Controls.Add(gradeOps);

            Controls.Add(_cardEntrada);
            foreach (var v in _vistas) Controls.Add(v.Card);
            Controls.Add(_cardMatriz);
            Controls.Add(_cardOperacoes);

            DefinirValor(_valor);
        }

        public override void AoAtivar()
        {
            _entrada.Focus();
            _entrada.SelectAll();
        }

        private (CardNordic, RotuloNordic, RotuloNordic, string) CriarVista(string titulo, PapelTexto papel)
        {
            var card = new CardNordic { Clicavel = true, PaddingConteudo = new Padding(14, 12, 14, 12) };
            var rotulo = Rotulo(titulo, PapelTexto.Secundario, ThemeManager.FonteRotulo, autoSize: false);
            rotulo.Dock = DockStyle.Top;
            rotulo.Height = 20;
            var valor = Rotulo("", papel, ThemeManager.FonteMonoBase, autoSize: false);
            valor.Dock = DockStyle.Fill;
            valor.TextAlign = ContentAlignment.MiddleLeft;
            card.Controls.Add(valor);
            card.Controls.Add(rotulo);

            async void Copiar(object? s, EventArgs e) => await CopiarVistaAsync(card);
            card.Click += Copiar;
            rotulo.Click += Copiar;
            valor.Click += Copiar;
            rotulo.Cursor = valor.Cursor = Cursors.Hand;
            _dicas.SetToolTip(card, "Clique para copiar");
            _dicas.SetToolTip(valor, "Clique para copiar");
            return (card, rotulo, valor, titulo);
        }

        private BotaoNordic Operacao(string texto, string dica, Func<ulong, ulong> operacao)
        {
            BotaoNordic b = Botao(texto, EstiloBotao.Secundario, Icone.Nenhum, (_, _) => DefinirValor(operacao(_valor)));
            b.Font = ThemeManager.FonteBotaoPequeno;
            _dicas.SetToolTip(b, dica);
            return b;
        }

        // ------------------------------------------------------------------ Entrada

        private void AoDigitar()
        {
            if (_sincronizandoEntrada) return;

            string texto = _entrada.Text;
            if (string.IsNullOrWhiteSpace(texto))
            {
                _entrada.Erro = false;
                (_seloBase.Text, _seloBase.Tipo) = ("AGUARDANDO ENTRADA", TipoSelo.Neutro);
                return;
            }

            if (TentarConverter(texto, out ulong valor, out BaseNumerica b, out _))
            {
                _entrada.Erro = false;
                MostrarBase(b);
                AtualizarVistas(valor);
            }
            else
            {
                _entrada.Erro = true;
                (_seloBase.Text, _seloBase.Tipo) = ("ENTRADA INVÁLIDA", TipoSelo.Perigo);
            }
        }

        private void Entrada_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;

            if (TentarConverter(_entrada.Text, out ulong valor, out _, out string? erro))
            {
                DefinirValor(valor); // normaliza a entrada em 0x + 16 dígitos
            }
            else
            {
                _entrada.Erro = true;
                Toast.Mostrar(this, erro ?? "Entrada inválida.", TipoToast.Erro);
            }
        }

        /// <summary>Converte pelo Core; em erro devolve a mensagem do Core (negativo, hífen, dígito, limite de 64 bits).</summary>
        private bool TentarConverter(string texto, out ulong valor, out BaseNumerica baseDetectada, out string? erro)
        {
            valor = 0;
            baseDetectada = BaseNumerica.Decimal;
            erro = null;
            try
            {
                valor = unchecked((ulong)_calculadora.ConverterEntrada(texto, out baseDetectada));
                return true;
            }
            catch (ArgumentException)
            {
                erro = "Entrada sem dígitos: digite um valor para converter.";
            }
            catch (FormatException ex)
            {
                erro = ex.Message;
            }
            catch (OverflowException)
            {
                erro = "O valor excede a capacidade de 64 bits.";
            }
            return false;
        }

        private void MostrarBase(BaseNumerica b) =>
            (_seloBase.Text, _seloBase.Tipo) = b switch
            {
                BaseNumerica.Hexadecimal => ("HEXADECIMAL (64 BITS)", TipoSelo.Sucesso),
                BaseNumerica.Binario => ("BINÁRIO (64 BITS)", TipoSelo.Sucesso),
                _ => ("DECIMAL (64 BITS)", TipoSelo.Sucesso)
            };

        /// <summary>Novo valor vindo da matriz, de uma operação ou do Enter: atualiza tudo e normaliza a entrada.</summary>
        private void DefinirValor(ulong valor)
        {
            _sincronizandoEntrada = true;
            try
            {
                _entrada.Text = FormatarHex(valor);
            }
            finally
            {
                _sincronizandoEntrada = false;
            }
            _entrada.Erro = false;
            MostrarBase(BaseNumerica.Hexadecimal);
            AtualizarVistas(valor);
        }

        private void AtualizarVistas(ulong valor)
        {
            _valor = valor;
            _vistas[0].Valor.Text = FormatarHex(valor);
            _vistas[1].Valor.Text = valor.ToString(CultureInfo.InvariantCulture);
            _vistas[2].Valor.Text = OperacoesBits.ParaInt64(valor).ToString(CultureInfo.InvariantCulture);
            _vistas[3].Valor.Text = OperacoesBits.ParaAscii(valor);
            _matriz.Valor = valor;
        }

        private static string FormatarHex(ulong v) => "0x" + v.ToString("X16", CultureInfo.InvariantCulture);

        // ------------------------------------------------------------------ Cópia com feedback

        private bool TelaAtiva => !IsDisposed && !Disposing && IsHandleCreated;

        private async Task CopiarVistaAsync(CardNordic card)
        {
            int i = Array.FindIndex(_vistas, v => v.Card == card);
            if (i < 0) return;
            var vista = _vistas[i];
            string valor = vista.Valor.Text;
            if (string.IsNullOrWhiteSpace(valor)) return;

            // Reentrância: se o feedback ainda está na tela, só recopia, sem empilhar outro ciclo
            if (_copiando.Contains(card))
            {
                TentarCopiar(valor);
                return;
            }

            if (!TentarCopiar(valor))
            {
                Toast.Mostrar(this, "Área de transferência ocupada por outro programa. Tente de novo.", TipoToast.Aviso);
                return;
            }

            _copiando.Add(card);
            try
            {
                vista.Titulo.Text = "COPIADO ✓";
                vista.Titulo.Papel = PapelTexto.Sucesso;
                await Task.Delay(1200);
            }
            finally
            {
                _copiando.Remove(card);
                if (TelaAtiva && !vista.Titulo.IsDisposed)
                {
                    vista.Titulo.Text = vista.TituloOriginal;
                    vista.Titulo.Papel = PapelTexto.Secundario;
                }
            }
        }

        private static bool TentarCopiar(string valor)
        {
            try
            {
                Clipboard.SetText(valor);
                return true;
            }
            catch (ExternalException)
            {
                return false; // área de transferência ocupada
            }
        }

        // ------------------------------------------------------------------ Layout

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (_vistas is null) return;

            int largura = ClientSize.Width - Padding.Horizontal;
            int x0 = Padding.Left + AutoScrollPosition.X;
            int y = Padding.Top + AutoScrollPosition.Y;
            int gap = Px(16);

            _cardEntrada.SetBounds(x0, y, largura, Px(120));
            y += Px(120) + gap;

            int gapVista = Px(12);
            int lv = (largura - gapVista * 3) / 4;
            for (int i = 0; i < _vistas.Length; i++)
            {
                _vistas[i].Card.SetBounds(x0 + i * (lv + gapVista), y, lv, Px(78));
                _vistas[i].Titulo.Height = Px(20);
            }
            y += Px(78) + gap;

            _cardMatriz.SetBounds(x0, y, largura, Px(268));
            y += Px(268) + gap;

            _cardOperacoes.SetBounds(x0, y, largura, Px(98));
            y += Px(98);

            AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y + Padding.Bottom);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _dicas.Dispose();
            base.Dispose(disposing);
        }
    }
}
