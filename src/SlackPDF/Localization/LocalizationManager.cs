using System.Globalization;
using System.Windows;

namespace SlackPDF.Localization;

public static class LocalizationManager
{
    private const string DictPathTemplate =
        "pack://application:,,,/SlackPDF;component/Localization/Strings.{0}.xaml";

    public static readonly string[] SupportedLanguages = ["en-US", "ru-RU"];

    public static string CurrentLanguage { get; private set; } = "en-US";

    public static void Apply(string languageCode)
    {
        var uri = new Uri(string.Format(DictPathTemplate, languageCode));
        var dict = new ResourceDictionary { Source = uri };

        var existing = Application.Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("/Localization/") == true);

        if (existing != null)
            Application.Current.Resources.MergedDictionaries.Remove(existing);

        Application.Current.Resources.MergedDictionaries.Add(dict);
        CurrentLanguage = languageCode;
    }

    public static string ApplyFromSystem()
    {
        var lang = CultureInfo.CurrentUICulture.Name;
        var code = SupportedLanguages.Contains(lang) ? lang : "en-US";
        Apply(code);
        return code;
    }

    public static string Get(string key)
        => Application.Current.TryFindResource(key) as string ?? $"[{key}]";

    public static string Get(string key, params object[] args)
        => string.Format(Get(key), args);
}
