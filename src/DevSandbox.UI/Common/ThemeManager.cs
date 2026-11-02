using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DevSandbox.UI.Common
{
    /// <summary>
    /// Gerenciador de tema centralizado com os tokens oficiais do Windows 11 Fluent Design (WinUI 3 Dark).
    /// </summary>
    public static class ThemeManager
    {
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        // --- Paleta Oficial Windows 11 Fluent Dark (Mica & Surfaces) ---
        public static readonly Color FundoJanela = Color.FromArgb(32, 32, 32);         // #202020 (Mica Dark)
        public static readonly Color FundoCard = Color.FromArgb(43, 43, 43);           // #2B2B2B (Card Background)
        public static readonly Color FundoCardHover = Color.FromArgb(50, 50, 50);      // #323232
        public static readonly Color FundoCardClick = Color.FromArgb(38, 38, 38);      // #262626
        public static readonly Color BordaCard = Color.FromArgb(56, 56, 56);           // #383838 (Card Border)
        public static readonly Color BordaSuave = Color.FromArgb(56, 56, 56);          // Compatibilidade

        // --- Controles e Entradas ---
        public static readonly Color FundoInput = Color.FromArgb(30, 30, 30);          // #1E1E1E (Input Dark)
        public static readonly Color BordaInput = Color.FromArgb(58, 58, 58);          // #3A3A3A
        public static readonly Color FundoHover = Color.FromArgb(56, 56, 56);          // #383838 (Hover padrão)
        
        // --- Botão Secundário / Padrão ---
        public static readonly Color BotaoSecundarioFundo = Color.FromArgb(56, 56, 56);// #383838
        public static readonly Color BotaoSecundarioHover = Color.FromArgb(68, 68, 68);// #444444
        public static readonly Color BotaoSecundarioPress = Color.FromArgb(48, 48, 48);// #303030

        // --- Tipografia Windows 11 ---
        public static readonly Color TextoPrincipal = Color.FromArgb(255, 255, 255);   // #FFFFFF
        public static readonly Color TextoSecundario = Color.FromArgb(160, 160, 160);  // #A0A0A0 (Subtitle / Subtext)
        public static readonly Color TextoDesabilitado = Color.FromArgb(118, 118, 118);// #767676

        // --- Cor de Acento Windows 11 (Vibrant Cyan / Blue) ---
        public static readonly Color CorAcento = Color.FromArgb(76, 194, 255);         // #4CC2FF (Windows 11 Accent Dark)
        public static readonly Color CorAcentoHover = Color.FromArgb(71, 180, 236);    // #47B4EC
        public static readonly Color CorAcentoPress = Color.FromArgb(65, 165, 216);    // #41A5D8
        public static readonly Color CorAcentoTexto = Color.FromArgb(0, 0, 0);          // #000000 (Alto contraste sobre o acento)

        // --- Compatibilidade e Cores de Alerta ---
        public static readonly Color AzulPrimario = CorAcento;
        public static readonly Color AzulEscuro = Color.FromArgb(0, 120, 212);         // #0078D4
        public static readonly Color VerdeTerminal = Color.FromArgb(34, 197, 94);      // #22C55E
        public static readonly Color VerdeEscuro = Color.FromArgb(22, 163, 74);        // #16A34A
        public static readonly Color LaranjaAlerta = Color.FromArgb(245, 158, 11);     // #F59E0B
        public static readonly Color VermelhoPerigo = Color.FromArgb(239, 68, 68);     // #EF4444
        public static readonly Color RoxoDestaque = Color.FromArgb(168, 85, 247);      // #A855F7

        // --- Fontes compartilhadas (cache GDI) ---
        // Instâncias únicas, criadas uma vez e reutilizadas por toda a vida do processo.
        // NÃO descartar: controles que as recebem não são donos delas.
        public static readonly Font FontePadrao = new("Segoe UI", 9.5F, FontStyle.Regular);
        public static readonly Font FonteBotao = new("Segoe UI", 9.5F, FontStyle.Bold);
        public static readonly Font FonteEntrada = new("Segoe UI", 10F, FontStyle.Regular);
        public static readonly Font FonteTerminal = new("Consolas", 10F, FontStyle.Regular);
        public static readonly Font FonteSubtitulo = new("Segoe UI", 8.5F, FontStyle.Regular);

        // --- Indicador de foco (navegação por teclado) ---
        public static readonly Color CorFoco = Color.FromArgb(255, 255, 255);         // Contorno de foco (alto contraste)

        /// <summary>
        /// Aplica o modo escuro à barra de título nativa do Windows (Windows 10 Build 1809+ e Windows 11).
        /// </summary>
        public static void AplicarBarraDeTituloEscura(Form form)
        {
            try
            {
                int useDarkMode = 1;
                int res = DwmSetWindowAttribute(form.Handle, 20, ref useDarkMode, sizeof(int));
                if (res != 0)
                {
                    DwmSetWindowAttribute(form.Handle, 19, ref useDarkMode, sizeof(int));
                }
            }
            catch
            {
                // Ignora em sistemas onde DWM não suportar a chamada
            }
        }

        /// <summary>
        /// Gera um GraphicsPath com cantos arredondados (para desenho antialiased de cartões, botões e caixas de texto).
        /// </summary>
        public static GraphicsPath CriarRetanguloArredondado(Rectangle rect, int raio)
        {
            var path = new GraphicsPath();
            if (raio <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int diametro = raio * 2;
            var arc = new Rectangle(rect.X, rect.Y, diametro, diametro);

            // Canto superior esquerdo
            path.AddArc(arc, 180, 90);

            // Canto superior direito
            arc.X = rect.Right - diametro;
            path.AddArc(arc, 270, 90);

            // Canto inferior direito
            arc.Y = rect.Bottom - diametro;
            path.AddArc(arc, 0, 90);

            // Canto inferior esquerdo
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        public static void EstilarBotao(Button btn, Color corFundo, Color corTexto)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = corFundo;
            btn.ForeColor = corTexto;
            btn.Cursor = Cursors.Hand;
            btn.Font = FonteBotao;
        }

        public static void EstilarComboBox(ComboBox cbo)
        {
            cbo.FlatStyle = FlatStyle.Flat;
            cbo.BackColor = FundoCard;
            cbo.ForeColor = TextoPrincipal;
            cbo.Font = FontePadrao;
        }

        public static void EstilarTextBox(TextBox txt, bool isTerminal = false)
        {
            txt.BackColor = FundoInput;
            txt.BorderStyle = BorderStyle.FixedSingle;
            if (isTerminal)
            {
                txt.ForeColor = VerdeTerminal;
                txt.Font = FonteTerminal;
            }
            else
            {
                txt.ForeColor = TextoPrincipal;
                txt.Font = FontePadrao;
            }
        }

        /// <summary>
        /// Desenha o contorno de foco no padrão Fluent (anel arredondado inset de 2 px),
        /// usado pelos controles customizados quando recebem foco pelo teclado.
        /// </summary>
        public static void DesenharFoco(Graphics g, Rectangle area, int raio)
        {
            Rectangle anel = Rectangle.Inflate(area, -1, -1);
            if (anel.Width <= 2 || anel.Height <= 2) return;

            using GraphicsPath path = CriarRetanguloArredondado(anel, Math.Max(0, raio - 1));
            using var pen = new Pen(CorFoco, 2f);
            g.DrawPath(pen, path);
        }

        /// <summary>
        /// Resolve a cor de fundo real do container pai, garantindo que cantos arredondados de botões e cartões
        /// não apresentem bordas ou cantos pretos no Windows Forms.
        /// </summary>
        public static Color ObterCorFundoEfetiva(Control? control)
        {
            Control? p = control?.Parent;
            while (p != null)
            {
                if (p.BackColor != Color.Transparent && p.BackColor.A == 255)
                {
                    return p.BackColor;
                }
                p = p.Parent;
            }
            return FundoJanela;
        }
    }
}
