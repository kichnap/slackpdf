using System.IO;
using System.Windows;

namespace SlackPDF.PrinterUI;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var args = e.Args;

        if (args.Length > 0 && args[0] == "/savedialog")
        {
            // args[1] = suggested filename (e.g. "Report.pdf")
            // args[2] = path to result file — service reads chosen path from it
            var suggested  = args.Length > 1 ? args[1] : "Document.pdf";
            var resultFile = args.Length > 2 ? args[2] : null;

            // Microsoft.Win32.SaveFileDialog is not IDisposable — don't use `using`
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title      = "Сохранить PDF",
                Filter     = "PDF files (*.pdf)|*.pdf",
                FileName   = suggested,
                DefaultExt = ".pdf"
            };

            // Write the full chosen path to the result file (empty string = cancelled).
            // Using a file instead of stdout because the service launches via CreateProcessAsUser
            // without a console, so stdout is not available.
            var chosenPath = dialog.ShowDialog() == true ? dialog.FileName : string.Empty;
            if (resultFile != null)
            {
                try { File.WriteAllText(resultFile, chosenPath); }
                catch { /* best-effort — service will treat missing file as cancel */ }
            }

            Shutdown();
            return;
        }

        new PrinterSettingsWindow().Show();
    }
}
