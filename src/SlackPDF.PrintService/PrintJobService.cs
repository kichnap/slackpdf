using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SlackPDF.PrinterShared;

namespace SlackPDF.PrintService;

// The Windows Print Spooler (via Local Port monitor / localspl.dll) opens the port
// C:\ProgramData\SlackPDF\spool\input.ps and writes raw PostScript into it.
// We watch for that file, rename it immediately so the next job can land, then convert.
public class PrintJobService : BackgroundService
{
    private static readonly string SpoolDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "SlackPDF", "spool");

    private static readonly string InputFilePath = Path.Combine(SpoolDir, "input.ps");

    private static readonly string LogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "SlackPDF", "service.log");

    [DllImport("kernel32.dll")] private static extern int GetACP();

    // Resolved once at startup: the Windows ANSI code page (e.g. 1251 on Russian Windows).
    // PSCRIPT5.DLL encodes non-ASCII document titles in PS hex strings using this code page.
    private static readonly Encoding AnsiEncoding;

    static PrintJobService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        try { AnsiEncoding = Encoding.GetEncoding(GetACP()); }
        catch { AnsiEncoding = Encoding.Latin1; }
    }

    private readonly GhostscriptConverter     _converter;
    private readonly TrayNotification         _tray;
    private readonly ILogger<PrintJobService> _logger;
    private int _jobRunning; // Interlocked flag: 0 = idle, 1 = running

    public PrintJobService(
        GhostscriptConverter converter,
        TrayNotification tray,
        ILogger<PrintJobService> logger)
    {
        _converter = converter;
        _tray      = tray;
        _logger    = logger;
    }

    private static void FileLog(string msg)
    {
        try { File.AppendAllText(LogFile, $"{DateTime.Now:HH:mm:ss.fff} {msg}{Environment.NewLine}"); }
        catch { }
    }

    protected override Task ExecuteAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(SpoolDir);
        FileLog($"=== Service started (user={Environment.UserName}, pid={Environment.ProcessId}) ===");
        FileLog($"Watching: {InputFilePath}");
        _logger.LogInformation("SlackPDF PrintService watching {Path}", InputFilePath);

        // In case the spooler wrote the file while the service was stopped
        if (File.Exists(InputFilePath))
        {
            FileLog("input.ps present at startup — scheduling processing.");
            _ = HandleJobLoopAsync(ct);
        }

        var watcher = new FileSystemWatcher(SpoolDir, "input.ps")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        watcher.Created += (_, _) => { FileLog("FSW: Created"); _ = HandleJobLoopAsync(ct); };
        watcher.Changed += (_, _) => { FileLog("FSW: Changed"); _ = HandleJobLoopAsync(ct); };

        ct.Register(() => { watcher.EnableRaisingEvents = false; watcher.Dispose(); });

        // Keep ExecuteAsync alive until the service is stopped
        return Task.Delay(Timeout.Infinite, ct).ContinueWith(_ => { }, TaskContinuationOptions.None);
    }

    // Reentrancy guard: at most one job loop runs at a time.
    // After a job finishes, checks for a new input.ps and loops if found.
    private async Task HandleJobLoopAsync(CancellationToken ct)
    {
        if (Interlocked.CompareExchange(ref _jobRunning, 1, 0) != 0) return;
        try
        {
            do
            {
                var psPath = await WaitAndRenameInputAsync(ct);
                if (psPath == null) break;

                var jobName = ExtractPsTitle(psPath);
                var psBytes = new FileInfo(psPath).Length;
                FileLog($"Job: '{jobName}', {psBytes} bytes → {Path.GetFileName(psPath)}");
                _logger.LogInformation("Received job: {Job} ({Path})", jobName, psPath);

                await ProcessJobAsync(psPath, jobName, ct);
            }
            while (File.Exists(InputFilePath) && !ct.IsCancellationRequested);
        }
        finally
        {
            Interlocked.Exchange(ref _jobRunning, 0);

            // A new input.ps might have arrived while we held the lock
            if (File.Exists(InputFilePath) && !ct.IsCancellationRequested)
                _ = HandleJobLoopAsync(ct);
        }
    }

    // Polls until input.ps is no longer locked by the spooler, then renames it
    // to a unique file so the next print job can land on input.ps immediately.
    private static async Task<string?> WaitAndRenameInputAsync(CancellationToken ct)
    {
        for (int i = 0; i < 50; i++)   // up to 10 s (50 × 200 ms)
        {
            await Task.Delay(200, ct);
            if (!File.Exists(InputFilePath)) return null;
            try
            {
                // Exclusive open succeeds only after the spooler closes the file
                using (var probe = File.Open(InputFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    probe.Close();

                var dest = Path.Combine(SpoolDir, $"{Guid.NewGuid():N}.ps");
                File.Move(InputFilePath, dest, overwrite: false);
                FileLog($"Renamed input.ps → {Path.GetFileName(dest)}");
                return dest;
            }
            catch (IOException)
            {
                // File still held by the spooler — retry
            }
        }
        FileLog("ERROR: input.ps still locked after 50 retries — skipping job");
        return null;
    }

    private static string ExtractPsTitle(string psPath)
    {
        try
        {
            using var sr = new StreamReader(psPath, Encoding.Latin1);
            for (int i = 0; i < 30; i++)
            {
                var line = sr.ReadLine();
                if (line == null) break;
                if (line.StartsWith("%%Title:", StringComparison.Ordinal))
                {
                    var value = line[8..].Trim();
                    // PSCRIPT5.DLL encodes non-ASCII titles as PS hex strings: <4D6963726F736F...>
                    // Decode using the system ANSI code page (e.g. CP1251 on Russian Windows).
                    if (value.StartsWith('<') && value.EndsWith('>'))
                    {
                        try
                        {
                            var bytes = Convert.FromHexString(value[1..^1]);
                            return AnsiEncoding.GetString(bytes);
                        }
                        catch { return value; }
                    }
                    return value;
                }
                if (line.StartsWith("%%EndComments", StringComparison.Ordinal)) break;
            }
        }
        catch { }
        return "Document";
    }

    private async Task ProcessJobAsync(string psPath, string jobName, CancellationToken ct)
    {
        _tray.ShowProcessing(jobName);
        try
        {
            var settings  = PrinterSettings.Load();
            FileLog($"ProcessJob: outputFolder='{settings.OutputFolder}' showDialog={settings.ShowSaveDialog}");
            var processor = new PrintJobProcessor(settings);

            // Consume hint before SaveAsync so the output path and page dimensions are known.
            var hint = NextJobHint.Consume();
            if (hint != null)
                FileLog($"Hint: output='{hint.OutputPath}' size={hint.PageWidthPts:F0}×{hint.PageHeightPts:F0} pts");

            var tempPdf = await _converter.ConvertAsync(psPath, settings.Quality, ct);
            FileLog($"GS converted → {tempPdf}");

            // PSCRIPT5.DLL has two landscape strategies:
            //   A) Standard sizes: portrait media box + 90° content rotation → GS outputs portrait PDF.
            //      We must add /Rotate 90 so viewers display it landscape.
            //   B) Custom/elongated sizes: landscape dimensions used directly → GS outputs landscape PDF.
            //      /Rotate must NOT be added — it would flip the page to portrait.
            // Fix: apply /Rotate 90 only when the intended output is landscape (hint widthPts > heightPts)
            // AND the PDF GS produced is portrait (page height > width).
            var intendedLandscape = hint?.PageWidthPts > hint?.PageHeightPts;
            if (intendedLandscape && PdfPageRotator.IsPortrait(tempPdf))
            {
                PdfPageRotator.SetRotation(tempPdf, 90);
                FileLog("Applied /Rotate 90 (PSCRIPT5 portrait+rotation encoding → landscape PDF)");
            }
            else if (intendedLandscape)
            {
                FileLog("Landscape PDF already has correct dimensions — /Rotate skipped");
            }

            var saved = await processor.SaveAsync(tempPdf, jobName, appName: null, hint, ct);

            if (saved != null)
            {
                FileLog($"Saved PDF → {saved}");
                _logger.LogInformation("Saved: {Path}", saved);
                _tray.ShowDone(saved);
            }
            else
            {
                FileLog($"Skipped (conflict strategy): {jobName}");
                _logger.LogInformation("Skipped: {Job}", jobName);
            }
        }
        catch (Exception ex)
        {
            FileLog($"ProcessJobAsync error: {ex.GetType().Name}: {ex.Message}");
            _logger.LogError(ex, "Failed to process job: {Job}", jobName);
            _tray.ShowError(ex.Message);
        }
        finally
        {
            _tray.Hide();
            try { File.Delete(psPath); } catch { }
        }
    }
}
