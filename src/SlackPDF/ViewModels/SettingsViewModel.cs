using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using SlackPDF.Core;
using SlackPDF.Core.Engines;
using SlackPDF.Services;

namespace SlackPDF.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly PdfOperations _ops;
    private readonly ThumbnailService _thumbs;
    private AppSettings _settings;

    [ObservableProperty] private bool _isEnglish = true;
    [ObservableProperty] private bool _isRussian;
    [ObservableProperty] private bool _isPdfSharp = true;
    [ObservableProperty] private bool _isIText;
    [ObservableProperty] private bool _isLightTheme = true;
    [ObservableProperty] private bool _isDarkTheme;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public SettingsViewModel(PdfOperations ops, ThumbnailService thumbs)
    {
        _ops = ops;
        _thumbs = thumbs;
        _settings = SettingsService.Load();
        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        IsEnglish  = _settings.Language == "en-US";
        IsRussian  = _settings.Language == "ru-RU";
        IsPdfSharp = _settings.PdfEngine == "PDFsharp";
        IsIText    = _settings.PdfEngine == "iText";
        IsLightTheme = _settings.Theme == "Light";
        IsDarkTheme  = _settings.Theme == "Dark";
    }

    [RelayCommand]
    private void SetLanguage(string code)
    {
        Localization.LocalizationManager.Apply(code);
        _settings = _settings with { Language = code };
        SettingsService.Save(_settings);
        IsEnglish = code == "en-US";
        IsRussian = code == "ru-RU";
    }

    [RelayCommand]
    private void SetEngine(string name)
    {
        IPdfEngine engine = name == "iText" ? new ITextEngine() : new PdfSharpEngine();
        _ops.SetEngine(engine);
        _settings = _settings with { PdfEngine = name };
        SettingsService.Save(_settings);
        IsPdfSharp = name == "PDFsharp";
        IsIText    = name == "iText";
    }

    [RelayCommand]
    private void SetTheme(string theme)
    {
        var paletteHelper = new PaletteHelper();
        var existingTheme = paletteHelper.GetTheme();
        existingTheme.SetBaseTheme(theme == "Dark" ? BaseTheme.Dark : BaseTheme.Light);
        paletteHelper.SetTheme(existingTheme);
        _settings = _settings with { Theme = theme };
        SettingsService.Save(_settings);
        IsLightTheme = theme == "Light";
        IsDarkTheme  = theme == "Dark";
    }

    [RelayCommand]
    private void ClearCache()
    {
        _thumbs.ClearCache();
        StatusMessage = "Cache cleared.";
    }
}
