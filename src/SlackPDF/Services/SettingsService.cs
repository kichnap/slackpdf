using System.Text.Json;

namespace SlackPDF.Services;

public enum PostSaveAction { Nothing, OpenFolder, OpenFile }

public record AppSettings(
    string Language,
    string PdfEngine,
    string Theme,
    string ThumbnailCache)
{
    // Non-positional so missing from old JSON files gives default (Nothing), not a parse error
    public PostSaveAction PostSave { get; init; } = PostSaveAction.Nothing;

    public static AppSettings Default => new("en-US", "PDFsharp", "Light",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SlackPDF", "thumbcache"));
}

public static class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SlackPDF", "settings.json");

    // Cached for fast access from any ViewModel (e.g. BaseOperationViewModel post-save action)
    public static AppSettings Current { get; private set; } = Load();

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? AppSettings.Default;
            }
        }
        catch { }
        return AppSettings.Default;
    }

    public static void Save(AppSettings settings)
    {
        Current = settings;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
}
