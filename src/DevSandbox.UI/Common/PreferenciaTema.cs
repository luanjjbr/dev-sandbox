using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace DevSandbox.UI.Common
{
    /// <summary>
    /// Persistência da escolha de tema em <c>%AppData%\DevSandbox\tema.json</c>.
    /// Na 1ª execução (sem arquivo) segue o Windows (<c>AppsUseLightTheme</c>); depois fica fixo até o usuário trocar.
    /// Falhas de disco/registro nunca derrubam o app: caem no tema do Windows ou no claro.
    /// </summary>
    internal static class PreferenciaTema
    {
        private sealed record Arquivo(string Tema);

        private static string Caminho => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DevSandbox", "tema.json");

        public static TemaVisual Carregar()
        {
            try
            {
                if (File.Exists(Caminho))
                {
                    Arquivo? a = JsonSerializer.Deserialize<Arquivo>(File.ReadAllText(Caminho));
                    if (a is not null && Enum.TryParse(a.Tema, ignoreCase: true, out TemaVisual tema)
                        && Enum.IsDefined(tema))
                    {
                        return tema;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                // Arquivo ilegível: segue o Windows
            }
            return TemaDoWindows();
        }

        public static void Salvar(TemaVisual tema)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Caminho)!);
                File.WriteAllText(Caminho, JsonSerializer.Serialize(new Arquivo(tema.ToString())));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Sem permissão de escrita: o tema vale só nesta sessão
            }
        }

        private static TemaVisual TemaDoWindows()
        {
            try
            {
                using RegistryKey? chave = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (chave?.GetValue("AppsUseLightTheme") is int claro)
                {
                    return claro == 0 ? TemaVisual.Escuro : TemaVisual.Claro;
                }
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
            }
            return TemaVisual.Claro;
        }
    }
}
