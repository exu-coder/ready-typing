using System;
using System.Windows.Forms;

namespace BijoyTrainer;

/// <summary>
/// Application entry point for the Ready Typing Bijoy trainer.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Configures high-DPI rendering and starts the main window.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
