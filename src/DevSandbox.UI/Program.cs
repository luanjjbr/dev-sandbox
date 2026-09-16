using System;
using System.Windows.Forms;
using DevSandbox.UI.Forms;

namespace DevSandbox.UI
{
    internal static class Program
    {
        /// <summary>
        /// Ponto de entrada principal para a aplicação.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
