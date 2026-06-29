namespace SlackPDF.PrinterInstaller;

public static class GhostscriptDownloader
{
    private const string GsVersion    = "10.03.1";
    private const string GsVersionTag = "gs10031";
    private const string GsInstallerName = "gs10031w64.exe";
    private const string GsUrl =
        $"https://github.com/ArtifexSoftware/ghostpdl-downloads/releases/download/{GsVersionTag}/{GsInstallerName}";

    public static async Task DownloadAndInstallAsync(string installDir)
    {
        var gsDir         = Path.Combine(installDir, "ghostscript");
        var gsDll         = Path.Combine(gsDir, "bin", "gsdll64.dll");

        if (File.Exists(gsDll))
        {
            Console.WriteLine($"Ghostscript already present at {gsDll}, skipping download.");
            return;
        }

        Console.WriteLine($"Downloading Ghostscript {GsVersion}...");
        var installerPath = Path.Combine(Path.GetTempPath(), GsInstallerName);

        using var http  = new HttpClient();
        http.Timeout    = TimeSpan.FromMinutes(5);
        var bytes = await http.GetByteArrayAsync(GsUrl);
        await File.WriteAllBytesAsync(installerPath, bytes);

        Console.WriteLine("Installing Ghostscript silently...");
        Directory.CreateDirectory(gsDir);

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName        = installerPath,
            Arguments       = $"/S /D={gsDir}",
            UseShellExecute = false,
            CreateNoWindow  = true,
            Verb            = "runas"
        };
        using var proc = System.Diagnostics.Process.Start(psi)!;
        await proc.WaitForExitAsync();

        File.Delete(installerPath);
        Console.WriteLine("Ghostscript installation complete.");
    }
}
