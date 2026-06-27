using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using SlackPDF.Services;
using System.Diagnostics;

namespace SlackPDF.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ThumbnailService _thumbs;
    private AppSettings _settings;

    [ObservableProperty] private bool _isEnglish = true;
    [ObservableProperty] private bool _isRussian;
    [ObservableProperty] private bool _isLightTheme = true;
    [ObservableProperty] private bool _isDarkTheme;
    [ObservableProperty] private bool _postSaveNothing = true;
    [ObservableProperty] private bool _postSaveOpenFolder;
    [ObservableProperty] private bool _postSaveOpenFile;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public SettingsViewModel(SlackPDF.Core.PdfOperations ops, ThumbnailService thumbs)
    {
        _thumbs = thumbs;
        _settings = SettingsService.Load();
        IsEnglish    = _settings.Language == "en-US";
        IsRussian    = _settings.Language == "ru-RU";
        IsLightTheme = _settings.Theme != "Dark";
        IsDarkTheme  = _settings.Theme == "Dark";
        PostSaveNothing    = _settings.PostSave == PostSaveAction.Nothing;
        PostSaveOpenFolder = _settings.PostSave == PostSaveAction.OpenFolder;
        PostSaveOpenFile   = _settings.PostSave == PostSaveAction.OpenFile;
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
    private void SetPostSave(string value)
    {
        var action = Enum.Parse<PostSaveAction>(value);
        _settings = _settings with { PostSave = action };
        SettingsService.Save(_settings);
        PostSaveNothing    = action == PostSaveAction.Nothing;
        PostSaveOpenFolder = action == PostSaveAction.OpenFolder;
        PostSaveOpenFile   = action == PostSaveAction.OpenFile;
    }

    [RelayCommand]
    private void ClearCache()
    {
        _thumbs.ClearCache();
        StatusMessage = Localization.LocalizationManager.Get("Settings.CacheCleared");
    }

    [RelayCommand]
    private void OpenGitHub()
    {
        Process.Start(new ProcessStartInfo("https://github.com/kichnap/slackpdf")
        {
            UseShellExecute = true
        });
    }
}
