using System.Text.Json;

namespace SlackPDF.Services;

public record AppSettings(
    string Language,
    string PdfEngine,
    string Theme,
    string ThumbnailCache)
{
    public static AppSettings Default => new("en-US", "PDFsharp", "Light",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SlackPDF", "thumbcache"));
}

public static class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SlackPDF", "settings.json");

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
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
}
