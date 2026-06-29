using SlackPDF.PrinterInstaller;

var logPath = System.IO.Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "SlackPDF", "install.log");

void Log(string msg)
{
    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);
    var line = $"{DateTime.Now:HH:mm:ss} {msg}";
    System.IO.File.AppendAllText(logPath, line + Environment.NewLine);
    Console.WriteLine(line);
}

if (args.Length == 0)
{
    Log("ERROR: no arguments");
    Console.Error.WriteLine("Usage: SlackPDF.PrinterInstaller.exe <install <dir>|uninstall|download-ghostscript <dir>>");
    return 1;
}

Log($"Command: {string.Join(" ", args)}");

try
{
    switch (args[0].ToLowerInvariant())
    {
        case "install":
        {
            var installDir = args.Length > 1 ? args[1] : AppContext.BaseDirectory;
            PrinterInstaller.Install(installDir);
            break;
        }
        case "uninstall":
            PrinterInstaller.Uninstall();
            break;

        case "download-ghostscript":
        {
            var installDir = args.Length > 1 ? args[1] : AppContext.BaseDirectory;
            await GhostscriptDownloader.DownloadAndInstallAsync(installDir);
            break;
        }
        default:
            Log($"ERROR: unknown command: {args[0]}");
            return 1;
    }

    Log("Done.");
    return 0;
}
catch (Exception ex)
{
    Log($"EXCEPTION: {ex}");
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}
