using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DevSandbox.UI.Common
{
    /// <summary>Os dois temas do visual Nordic (Direção 4 aprovada).</summary>
    public enum TemaVisual
    {
        Claro,
        Escuro
    }

    /// <summary>
    /// Gerenciador de tema do visual Nordic (Híbrido Nordic Clean &amp; Slate Dark): tokens de cor dos dois temas
    /// (valores do protótipo <c>direcao-4-hibrido-nordic.html</c>), fontes compartilhadas, desenho de foco e a
    /// barra de título nativa. Toda cor da UI sai daqui; controles leem os tokens na hora de pintar e reagem ao
    /// evento <see cref="TemaMudou"/>.
    /// </summary>
    public static class ThemeManager
    {
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_ANTIGO = 19;
        private const int DWMWA_CAPTION_COLOR = 35;

        private static TemaVisual _tema = PreferenciaTema.Carregar();

        /// <summary>Disparado na thread da UI depois que o tema muda (todos os tokens já trocados).</summary>
        public static event EventHandler? TemaMudou;

        public static TemaVisual Tema => _tema;
        public static bool Escuro => _tema == TemaVisual.Escuro;

        /// <summary>Troca o tema, grava a escolha (fica fixa até o usuário trocar de novo) e avisa as telas.</summary>
        public static void DefinirTema(TemaVisual tema)
        {
            if (_tema == tema) return;
            _tema = tema;
            PreferenciaTema.Salvar(tema);

            foreach (Form f in Application.OpenForms)
            {
                AplicarBarraDeTitulo(f);
            }
            TemaMudou?.Invoke(null, EventArgs.Empty);
        }

        public static void Alternar() => DefinirTema(Escuro ? TemaVisual.Claro : TemaVisual.Escuro);

        private static Color Hex(int rgb) => Color.FromArgb(255, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
        private static Color T(int claro, int escuro) => Hex(Escuro ? escuro : claro);

        /// <summary>Mistura <paramref name="cor"/> sobre <paramref name="fundo"/> (equivale ao rgba do protótipo).</summary>
        public static Color Misturar(Color cor, Color fundo, float alfa) => Color.FromArgb(
            (int)Math.Round(cor.R * alfa + fundo.R * (1 - alfa)),
            (int)Math.Round(cor.G * alfa + fundo.G * (1 - alfa)),
            (int)Math.Round(cor.B * alfa + fundo.B * (1 - alfa)));

        // ------------------------------------------------------------------ Tokens: superfícies
        public static Color FundoApp => T(0xF1F5F9, 0x0B0F17);
        public static Color FundoCard => T(0xFFFFFF, 0x151D2A);
        public static Color FundoInput => T(0xF8FAFC, 0x0B0F17);
        public static Color FundoHover => T(0xF1F5F9, 0x1E293B);
        public static Color FundoMudo => T(0xE2E8F0, 0x1E293B);
        public static Color FundoCabecalho => T(0xFFFFFF, 0x151D2A);

        // ------------------------------------------------------------------ Tokens: bordas
        public static Color BordaSutil => T(0xE2E8F0, 0x1E293B);
        public static Color BordaForte => T(0xCBD5E1, 0x334155);
        public static Color BordaFoco => T(0x2563EB, 0x3B82F6);

        // ------------------------------------------------------------------ Tokens: texto
        public static Color TextoPrincipal => T(0x0F172A, 0xF8FAFC);
        public static Color TextoSecundario => T(0x475569, 0x94A3B8);
        public static Color TextoMudo => T(0x94A3B8, 0x64748B);
        public static Color TextoInverso => T(0xFFFFFF, 0x0B0F17);

        // ------------------------------------------------------------------ Tokens: acento
        public static Color Acento => T(0x2563EB, 0x3B82F6);
        public static Color AcentoHover => T(0x1D4ED8, 0x2563EB);
        public static Color AcentoSuave => Escuro ? Misturar(Acento, FundoCard, 0.15f) : Hex(0xEFF6FF);
        public static Color AcentoSecundario => T(0x0D9488, 0x14B8A6);
        /// <summary>Texto sobre botão de acento/perigo (branco nos dois temas, como no protótipo).</summary>
        public static Color TextoSobreAcento => Color.White;

        // ------------------------------------------------------------------ Tokens: status
        public static Color Sucesso => T(0x16A34A, 0x22C55E);
        public static Color SucessoFundo => Escuro ? Misturar(Sucesso, FundoCard, 0.15f) : Hex(0xDCFCE7);
        public static Color Aviso => T(0xD97706, 0xF59E0B);
        public static Color AvisoFundo => Escuro ? Misturar(Aviso, FundoCard, 0.15f) : Hex(0xFEF3C7);
        public static Color Perigo => T(0xDC2626, 0xEF4444);
        public static Color PerigoHover => Hex(0xB91C1C);
        public static Color PerigoFundo => Escuro ? Misturar(Perigo, FundoCard, 0.15f) : Hex(0xFEE2E2);
        /// <summary>Borda de campo inválido: estritamente #EF4444 (regra do usuário, skill design-interfaces §3.1).</summary>
        public static Color BordaErro => Hex(0xEF4444);

        // ------------------------------------------------------------------ Tokens: terminal embutido
        public static Color TermFundo => T(0x0F172A, 0x06090E);
        public static Color TermCabecalho => T(0x1E293B, 0x0E141F);
        public static Color TermBorda => T(0x334155, 0x1E293B);
        public static Color TermTexto => T(0xE2E8F0, 0xF1F5F9);
        public static Color TermVerde => T(0x10B981, 0x34D399);
        public static Color TermCiano => Hex(0x38BDF8);
        public static Color TermAmarelo => T(0xFBBF24, 0xFCD34D);
        public static Color TermVermelho => Hex(0xF87171);
        public static Color TermApagado => Hex(0x64748B);

        // ------------------------------------------------------------------ Geometria (px a 96 dpi)
        public const int RaioPequeno = 4;
        public const int RaioMedio = 6;
        public const int RaioGrande = 8;

        // ------------------------------------------------------------------ Fontes (cache GDI: nunca descartar)
        private static readonly string FamiliaUi = EscolherFamilia("Segoe UI Variable Text", "Segoe UI");
        private static readonly string FamiliaMono = EscolherFamilia("Cascadia Mono", "Consolas");

        public static readonly Font FonteUi = new(FamiliaUi, 9.75F, FontStyle.Regular);
        public static readonly Font FonteUiForte = new(FamiliaUi, 9.75F, FontStyle.Bold);
        public static readonly Font FonteBotao = new(FamiliaUi, 9F, FontStyle.Bold);
        public static readonly Font FonteBotaoPequeno = new(FamiliaUi, 8.25F, FontStyle.Bold);
        public static readonly Font FontePequena = new(FamiliaUi, 8.5F, FontStyle.Regular);
        public static readonly Font FonteRotulo = new(FamiliaUi, 8.25F, FontStyle.Bold);
        public static readonly Font FonteMicro = new(FamiliaUi, 7F, FontStyle.Bold);
        public static readonly Font FonteTituloCard = new(FamiliaUi, 9.75F, FontStyle.Bold);
        public static readonly Font FonteTitulo = new(FamiliaUi, 12F, FontStyle.Bold);
        public static readonly Font FonteDestaque = new(FamiliaUi, 18F, FontStyle.Bold);
        public static readonly Font FonteMono = new(FamiliaMono, 9F, FontStyle.Regular);
        public static readonly Font FonteMonoForte = new(FamiliaMono, 9F, FontStyle.Bold);
        public static readonly Font FonteMonoPequena = new(FamiliaMono, 7F, FontStyle.Regular);
        public static readonly Font FonteMonoValor = new(FamiliaMono, 10F, FontStyle.Bold);
        public static readonly Font FonteMonoBase = new(FamiliaMono, 11F, FontStyle.Bold);
        public static readonly Font FonteMonoHero = new(FamiliaMono, 16.5F, FontStyle.Bold);
        public static readonly Font FonteRelogio = new(FamiliaMono, 34F, FontStyle.Bold);

        private static string EscolherFamilia(string preferida, string reserva)
        {
            try
            {
                using var f = new FontFamily(preferida);
                return f.Name;
            }
            catch (ArgumentException)
            {
                return reserva; // família não instalada
            }
        }

        // ------------------------------------------------------------------ Janela

        /// <summary>
        /// Barra de título nativa no tema atual (modo escuro do DWM e, no Windows 11, a cor do cabeçalho).
        /// Chamar em <c>OnHandleCreated</c>; o <see cref="DefinirTema"/> reaplica em todas as janelas abertas.
        /// </summary>
        public static void AplicarBarraDeTitulo(Form form)
        {
            if (!form.IsHandleCreated) return;
            try
            {
                int escuro = Escuro ? 1 : 0;
                if (DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref escuro, sizeof(int)) != 0)
                {
                    // Builds antigos do Windows 10: melhor esforço, o HRESULT é descartado
                    _ = DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_ANTIGO, ref escuro, sizeof(int));
                }

                Color c = FundoCabecalho;
                int corRef = c.R | (c.G << 8) | (c.B << 16); // COLORREF = 0x00BBGGRR
                _ = DwmSetWindowAttribute(form.Handle, DWMWA_CAPTION_COLOR, ref corRef, sizeof(int)); // só Windows 11
            }
            catch (DllNotFoundException)
            {
                // Sem DWM: barra de título padrão
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        // ------------------------------------------------------------------ Desenho

        /// <summary>GraphicsPath com cantos arredondados (cartões, botões, caixas de texto).</summary>
        public static GraphicsPath CriarRetanguloArredondado(RectangleF rect, float raio)
        {
            var path = new GraphicsPath();
            if (raio <= 0 || rect.Width <= 0 || rect.Height <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            float d = Math.Min(raio * 2, Math.Min(rect.Width, rect.Height));
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static GraphicsPath CriarRetanguloArredondado(Rectangle rect, int raio) =>
            CriarRetanguloArredondado((RectangleF)rect, raio);

        /// <summary>Preenche e contorna um retângulo arredondado (borda de 1 px nítida).</summary>
        public static void DesenharCaixa(Graphics g, Rectangle area, int raio, Color fundo, Color? borda, float espessura = 1f)
        {
            var r = new RectangleF(area.X + 0.5f, area.Y + 0.5f, area.Width - 1f, area.Height - 1f);
            using GraphicsPath path = CriarRetanguloArredondado(r, raio);
            using (var b = new SolidBrush(fundo))
            {
                g.FillPath(b, path);
            }
            if (borda is { } cor && cor.A > 0)
            {
                using var p = new Pen(cor, espessura);
                g.DrawPath(p, path);
            }
        }

        /// <summary>Anel de foco (2 px, cor de foco) para navegação por teclado nos controles próprios.</summary>
        public static void DesenharFoco(Graphics g, Rectangle area, int raio)
        {
            var anel = new RectangleF(area.X + 1.5f, area.Y + 1.5f, area.Width - 3f, area.Height - 3f);
            if (anel.Width <= 2 || anel.Height <= 2) return;

            using GraphicsPath path = CriarRetanguloArredondado(anel, Math.Max(0, raio - 1));
            using var pen = new Pen(BordaFoco, 2f);
            g.DrawPath(pen, path);
        }

        public static void PrepararAntialias(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }

        /// <summary>Cor de fundo real do primeiro ancestral opaco (cantos arredondados sem "pontas" de outra cor).</summary>
        public static Color ObterCorFundoEfetiva(Control? control)
        {
            Control? p = control?.Parent;
            while (p != null)
            {
                if (p.BackColor.A == 255)
                {
                    return p.BackColor;
                }
                p = p.Parent;
            }
            return FundoApp;
        }

        /// <summary>Converte px de projeto (96 dpi) para px do dispositivo do controle.</summary>
        public static int Px(Control c, int px) => (int)Math.Round(px * c.DeviceDpi / 96.0);
    }
}
