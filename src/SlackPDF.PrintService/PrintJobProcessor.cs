using SlackPDF.PrinterShared;

namespace SlackPDF.PrintService;

public class PrintJobProcessor
{
    private readonly PrinterSettings _settings;

    public PrintJobProcessor(PrinterSettings settings)
    {
        _settings = settings;
    }

    public async Task<string?> SaveAsync(
        string tempPdfPath,
        string jobName,
        string? appName,
        NextJobHint? hint,
        CancellationToken ct)
    {
        var fileName = BuildFileName(jobName, appName) + ".pdf";

        // External-app hint consumed before conversion (in PrintJobService) so that
        // page dimensions were available for Ghostscript. Reuse it here for the output path.
        if (hint != null)
        {
            // If the hint is a full .pdf path use it directly; if it's a directory, append name.
            var hintPath = Path.HasExtension(hint.OutputPath) && !hint.OutputPath.EndsWith('\\')
                ? hint.OutputPath
                : Path.Combine(hint.OutputPath, fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(hintPath)!);
            File.Copy(tempPdfPath, hintPath, overwrite: true);
            File.Delete(tempPdfPath);
            return hintPath;
        }

        if (_settings.ShowSaveDialog)
        {
            // Ask the user where to save via a dialog launched in their desktop session.
            // The returned path is the full chosen file path (or null if cancelled).
            var chosenPath = await ShowSaveDialogAsync(fileName, ct);
            if (chosenPath == null) return null;

            Directory.CreateDirectory(Path.GetDirectoryName(chosenPath)!);
            File.Copy(tempPdfPath, chosenPath, overwrite: true);
            File.Delete(tempPdfPath);
            return chosenPath;
        }

        var outputDir = _settings.OutputFolder;
        Directory.CreateDirectory(outputDir);

        var outputPath = Path.Combine(outputDir, fileName);
        outputPath = ResolveConflict(outputPath);

        if (outputPath == null)
            return null; // Skip strategy: file exists, do nothing

        File.Copy(tempPdfPath, outputPath, overwrite: true);
        File.Delete(tempPdfPath);

        return outputPath;
    }

    public string BuildFileName(string jobName, string? appName)
    {
        var name = jobName;

        if (_settings.StripPathFromDocName && name.Contains('\\'))
            name = Path.GetFileNameWithoutExtension(name);

        if (Path.HasExtension(name))
            name = Path.GetFileNameWithoutExtension(name);

        var now = DateTime.Now;
        var result = _settings.FileNameTemplate
            .Replace("%[DocName]%", name)
            .Replace("%[AppName]%", appName ?? "Unknown")
            .Replace("%[Year]%",    now.ToString("yyyy"))
            .Replace("%[Month]%",   now.ToString("MM"))
            .Replace("%[Day]%",     now.ToString("dd"))
            .Replace("%[Hour]%",    now.ToString("HH"))
            .Replace("%[Minute]%",  now.ToString("mm"))
            .Replace("%[Second]%",  now.ToString("ss"));

        return SanitizeFileName(result);
    }

    private string? ResolveConflict(string path)
    {
        if (!File.Exists(path)) return path;

        return _settings.ConflictStrategy switch
        {
            FileConflictStrategy.Overwrite      => path,
            FileConflictStrategy.Skip           => null,
            FileConflictStrategy.AppendDateTime => AppendDateTime(path),
            FileConflictStrategy.AutoNumber     => AutoNumber(path),
            FileConflictStrategy.ShowSaveDialog => path, // handled above
            _                                   => AutoNumber(path)
        };
    }

    private static string AutoNumber(string path)
    {
        var dir  = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext  = Path.GetExtension(path);
        int n = 2;
        string result;
        do { result = Path.Combine(dir, $"{name}_{n++}{ext}"); }
        while (File.Exists(result));
        return result;
    }

    private static string AppendDateTime(string path)
    {
        var dir  = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext  = Path.GetExtension(path);
        var ts   = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
        return Path.Combine(dir, $"{name}_{ts}{ext}");
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }

    // Launches PrinterUI in /savedialog mode in the user's desktop session
    // (not Session 0 where the service runs). IPC via a temp file.
    // Returns the full chosen path, or null if cancelled or no user is logged in.
    private static async Task<string?> ShowSaveDialogAsync(string suggestedName, CancellationToken ct)
    {
        var printerUiPath = Path.Combine(
            AppContext.BaseDirectory, "..", "printerui", "SlackPDF.PrinterUI.exe");

        var resultFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "SlackPDF", $"dialog_{Guid.NewGuid():N}.result");

        try
        {
            // Escape quotes in the filename for the command line
            var safeFileName = suggestedName.Replace("\"", "\\\"");
            var safeResult   = resultFile.Replace("\"", "\\\"");
            var args         = $"/savedialog \"{safeFileName}\" \"{safeResult}\"";

            await UserSessionLauncher.LaunchAsync(printerUiPath, args, TimeSpan.FromMinutes(10), ct);

            if (!File.Exists(resultFile)) return null;
            var chosen = (await File.ReadAllTextAsync(resultFile, ct)).Trim();
            return string.IsNullOrEmpty(chosen) ? null : chosen;
        }
        finally
        {
            try { File.Delete(resultFile); } catch { }
        }
    }
}
