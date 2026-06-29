using System.Text.Json;

namespace SlackPDF.PrinterShared;

/// <summary>
/// A one-shot hint that overrides the output path for the next print job.
/// External applications (Revit macros, AutoCAD scripts, etc.) write this file
/// before sending a print job so the service saves the PDF to a specific location.
///
/// Usage from PowerShell:
///   [SlackPDF.PrinterShared.NextJobHint]::Write("D:\Projects\Drawing.pdf")
///
/// Usage from C# / any process:
///   NextJobHint.Write(@"D:\Projects\Drawing.pdf");
///
/// The hint is automatically consumed (deleted) after the first job that reads it.
/// It expires after 5 minutes to prevent stale hints from affecting later jobs.
/// </summary>
public sealed class NextJobHint
{
    public string   OutputPath   { get; init; } = string.Empty;
    public DateTime ExpiresAt    { get; init; }           // UTC

    /// <summary>
    /// When set, Ghostscript uses these exact page dimensions for the output PDF.
    /// Required for correct landscape output because PSCRIPT5.DLL encodes landscape as
    /// portrait media + 90° content rotation — GS cannot infer the intended orientation.
    /// Values are in PostScript points (1 mm = 2.83465 pt).
    /// </summary>
    public double?  PageWidthPts  { get; init; }
    public double?  PageHeightPts { get; init; }

    private static readonly string HintPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "SlackPDF", "next_output.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // -----------------------------------------------------------------------
    // Service side: read and delete the hint atomically.
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the stored hint if it exists and has not expired, then deletes it.
    /// Returns <c>null</c> if there is no hint, it has expired, or it is malformed.
    /// </summary>
    public static NextJobHint? Consume()
    {
        if (!File.Exists(HintPath)) return null;
        try
        {
            var json = File.ReadAllText(HintPath);
            var hint = JsonSerializer.Deserialize<NextJobHint>(json, JsonOptions);
            // Delete unconditionally so a bad hint is never retried
            try { File.Delete(HintPath); } catch { }

            if (hint == null || string.IsNullOrWhiteSpace(hint.OutputPath)) return null;
            if (hint.ExpiresAt < DateTime.UtcNow) return null;  // expired
            return hint;
        }
        catch
        {
            try { File.Delete(HintPath); } catch { }
            return null;
        }
    }

    // -----------------------------------------------------------------------
    // Client side: write a hint for the next print job.
    // -----------------------------------------------------------------------

    /// <summary>
    /// Writes a hint that the next print job should be saved to <paramref name="outputPath"/>.
    /// <paramref name="outputPath"/> can be either:
    ///   • a full file path  (e.g. <c>D:\Projects\Drawing.pdf</c>) — used as-is
    ///   • a directory path  (e.g. <c>D:\Projects\</c>)            — file name is appended
    /// The hint expires after <paramref name="ttl"/> (default 5 minutes).
    /// </summary>
    public static void Write(string outputPath, TimeSpan? ttl = null)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path cannot be empty.", nameof(outputPath));

        var hint = new NextJobHint
        {
            OutputPath = outputPath,
            ExpiresAt  = DateTime.UtcNow + (ttl ?? TimeSpan.FromMinutes(5))
        };

        Directory.CreateDirectory(Path.GetDirectoryName(HintPath)!);
        File.WriteAllText(HintPath, JsonSerializer.Serialize(hint, JsonOptions));
    }

    /// <summary>Removes any existing hint without using it.</summary>
    public static void Cancel()
    {
        try { File.Delete(HintPath); } catch { }
    }
}
