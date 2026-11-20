using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace DevSandbox.UI.Common
{
    /// <summary>Ícones vetoriais desenhados em GDI+ (a UI não usa emoji como ícone).</summary>
    public enum Icone
    {
        Nenhum,
        Inicio,
        Plugue,
        Raio,
        Hash,
        Relogio,
        Lua,
        Sol,
        Atualizar,
        Copiar,
        Check,
        Livro,
        Pilha,
        Enviar,
        Lixeira,
        Play,
        Pausa,
        Parar,
        Reset
    }

    public static class Icones
    {
        /// <summary>Desenha o ícone em traço de 1,6 px dentro de <paramref name="area"/> (quadrado, 16 px de projeto).</summary>
        public static void Desenhar(Graphics g, Icone icone, RectangleF area, Color cor)
        {
            if (icone == Icone.Nenhum) return;

            GraphicsState estado = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = Math.Min(area.Width, area.Height) / 16f;
            g.TranslateTransform(area.X + (area.Width - 16 * s) / 2, area.Y + (area.Height - 16 * s) / 2);
            g.ScaleTransform(s, s);

            using var pen = new Pen(cor, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            using var brush = new SolidBrush(cor);

            switch (icone)
            {
                case Icone.Inicio:
                    g.DrawLines(pen, new PointF[] { new(2, 8), new(8, 2.5f), new(14, 8) });
                    g.DrawLines(pen, new PointF[] { new(4, 6.5f), new(4, 13.5f), new(12, 13.5f), new(12, 6.5f) });
                    g.DrawLine(pen, 7, 13.5f, 7, 10);
                    g.DrawLine(pen, 9, 10, 9, 13.5f);
                    g.DrawLine(pen, 7, 10, 9, 10);
                    break;
                case Icone.Plugue:
                    g.DrawLine(pen, 6, 2, 6, 5);
                    g.DrawLine(pen, 10, 2, 10, 5);
                    using (GraphicsPath p = ThemeManager.CriarRetanguloArredondado(new RectangleF(3.5f, 5, 9, 5), 1.5f))
                        g.DrawPath(pen, p);
                    g.DrawLines(pen, new PointF[] { new(8, 10), new(8, 12), new(8, 14) });
                    break;
                case Icone.Raio:
                    g.DrawPolygon(pen, new PointF[] { new(9, 1.5f), new(3.5f, 9), new(7.5f, 9), new(6.5f, 14.5f), new(12.5f, 6.5f), new(8.5f, 6.5f) });
                    break;
                case Icone.Hash:
                    g.DrawLine(pen, 6, 2.5f, 5, 13.5f);
                    g.DrawLine(pen, 11, 2.5f, 10, 13.5f);
                    g.DrawLine(pen, 2.5f, 6, 13.5f, 6);
                    g.DrawLine(pen, 2.5f, 10, 13.5f, 10);
                    break;
                case Icone.Relogio:
                    g.DrawEllipse(pen, 2, 3, 12, 12);
                    g.DrawLines(pen, new PointF[] { new(8, 6), new(8, 9), new(10.5f, 10.5f) });
                    g.DrawLine(pen, 6.5f, 1.2f, 9.5f, 1.2f);
                    break;
                case Icone.Lua:
                    using (var p = new GraphicsPath())
                    {
                        p.AddArc(2, 2, 12, 12, 300, 300);
                        p.AddArc(5.5f, 0.5f, 10, 10, 160, -230);
                        p.CloseFigure();
                        g.DrawPath(pen, p);
                    }
                    break;
                case Icone.Sol:
                    g.DrawEllipse(pen, 5, 5, 6, 6);
                    for (int i = 0; i < 8; i++)
                    {
                        double a = i * Math.PI / 4;
                        g.DrawLine(pen, 8 + (float)Math.Cos(a) * 5, 8 + (float)Math.Sin(a) * 5,
                                        8 + (float)Math.Cos(a) * 6.8f, 8 + (float)Math.Sin(a) * 6.8f);
                    }
                    break;
                case Icone.Atualizar:
                    g.DrawArc(pen, 2.5f, 2.5f, 11, 11, -60, 290);
                    g.DrawLines(pen, new PointF[] { new(10.5f, 1.5f), new(11.2f, 3.6f), new(9, 4.6f) });
                    break;
                case Icone.Copiar:
                    using (GraphicsPath p = ThemeManager.CriarRetanguloArredondado(new RectangleF(5.5f, 5.5f, 8, 9), 1.5f))
                        g.DrawPath(pen, p);
                    g.DrawLines(pen, new PointF[] { new(3, 11), new(2.5f, 11), new(2.5f, 2.5f), new(10, 2.5f), new(10, 3) });
                    break;
                case Icone.Check:
                    g.DrawLines(pen, new PointF[] { new(3, 8.5f), new(6.5f, 12), new(13, 4.5f) });
                    break;
                case Icone.Livro:
                    g.DrawLines(pen, new PointF[] { new(8, 4), new(5, 3), new(2, 3.5f), new(2, 13), new(5, 12.5f), new(8, 13.5f), new(11, 12.5f), new(14, 13), new(14, 3.5f), new(11, 3), new(8, 4) });
                    g.DrawLine(pen, 8, 4, 8, 13.5f);
                    break;
                case Icone.Pilha:
                    g.DrawPolygon(pen, new PointF[] { new(8, 2), new(14, 5), new(8, 8), new(2, 5) });
                    g.DrawLines(pen, new PointF[] { new(2, 8), new(8, 11), new(14, 8) });
                    g.DrawLines(pen, new PointF[] { new(2, 11), new(8, 14), new(14, 11) });
                    break;
                case Icone.Enviar:
                    g.DrawPolygon(pen, new PointF[] { new(2, 2.5f), new(14, 8), new(2, 13.5f), new(4, 8) });
                    g.DrawLine(pen, 4, 8, 9, 8);
                    break;
                case Icone.Lixeira:
                    g.DrawLine(pen, 2.5f, 4, 13.5f, 4);
                    g.DrawLines(pen, new PointF[] { new(6, 4), new(6, 2.5f), new(10, 2.5f), new(10, 4) });
                    g.DrawLines(pen, new PointF[] { new(4, 4), new(4.8f, 14), new(11.2f, 14), new(12, 4) });
                    break;
                case Icone.Play:
                    g.FillPolygon(brush, new PointF[] { new(4.5f, 2.5f), new(13, 8), new(4.5f, 13.5f) });
                    break;
                case Icone.Pausa:
                    g.FillRectangle(brush, 4, 3, 3, 10);
                    g.FillRectangle(brush, 9, 3, 3, 10);
                    break;
                case Icone.Parar:
                    using (GraphicsPath p = ThemeManager.CriarRetanguloArredondado(new RectangleF(3.5f, 3.5f, 9, 9), 1.5f))
                        g.FillPath(brush, p);
                    break;
                case Icone.Reset:
                    g.DrawArc(pen, 2.5f, 2.5f, 11, 11, 200, 290);
                    g.DrawLines(pen, new PointF[] { new(2, 2.5f), new(2.6f, 5.6f), new(5.6f, 5) });
                    break;
            }

            g.Restore(estado);
        }
    }
}
