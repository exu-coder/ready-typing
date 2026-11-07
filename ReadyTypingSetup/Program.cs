using System;
using System.Windows.Forms;

namespace ReadyTypingSetup;

internal static class Program
{
    /// <summary>Entry point for the Ready Typing installer.</summary>
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ShowUnexpectedError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException +=
            (_, e) => ShowUnexpectedError(e.ExceptionObject as Exception);

        using var form = new InstallerForm();
        Application.Run(form);
    }

    private static void ShowUnexpectedError(Exception? error)
    {
        MessageBox.Show(
            "Ready Typing Setup hit an unexpected problem.\n\n" +
            (error?.Message ?? "Unknown error."),
            "Ready Typing Setup",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
