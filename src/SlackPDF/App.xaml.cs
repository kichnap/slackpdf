using SlackPDF.Localization;
using SlackPDF.Services;
using System.Windows;

namespace SlackPDF;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var settings = SettingsService.Load();
        var lang = settings.Language ?? LocalizationManager.ApplyFromSystem();
        LocalizationManager.Apply(lang);
    }
}
