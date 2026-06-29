using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlackPDF.PrinterShared;

public record PrinterSettings
{
    public bool ShowSaveDialog { get; init; } = false;
    public string OutputFolder { get; init; } =
        Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
    public string FileNameTemplate { get; init; } = "%[DocName]%";
    public bool StripPathFromDocName { get; init; } = true;
    public FileConflictStrategy ConflictStrategy { get; init; } = FileConflictStrategy.AutoNumber;
    public PdfQuality Quality { get; init; } = PdfQuality.High;

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "SlackPDF", "PrinterSettings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static PrinterSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<PrinterSettings>(json, JsonOptions) ?? new();
            }
        }
        catch { }
        return new();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
    }
}

public enum FileConflictStrategy
{
    Overwrite,
    AutoNumber,
    AppendDateTime,
    ShowSaveDialog,
    Skip
}

public enum PdfQuality
{
    Screen,
    Ebook,
    High,
    Prepress
}
