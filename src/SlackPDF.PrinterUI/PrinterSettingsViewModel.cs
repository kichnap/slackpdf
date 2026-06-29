using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlackPDF.PrinterShared;
using System.Windows;
using System.Windows.Forms;

namespace SlackPDF.PrinterUI;

public partial class PrinterSettingsViewModel : ObservableObject
{
    [ObservableProperty] bool   showSaveDialog;
    [ObservableProperty] string outputFolder       = string.Empty;
    [ObservableProperty] string fileNameTemplate   = "%[DocName]%";
    [ObservableProperty] bool   stripPathFromDocName = true;
    [ObservableProperty] FileConflictStrategy conflictStrategy;
    [ObservableProperty] PdfQuality quality;
    [ObservableProperty] string fileNamePreview    = string.Empty;

    public PrinterSettingsViewModel()
    {
        var s = PrinterSettings.Load();
        ShowSaveDialog       = s.ShowSaveDialog;
        OutputFolder         = s.OutputFolder;
        FileNameTemplate     = s.FileNameTemplate;
        StripPathFromDocName = s.StripPathFromDocName;
        ConflictStrategy     = s.ConflictStrategy;
        Quality              = s.Quality;
        UpdatePreview();
    }

    partial void OnFileNameTemplateChanged(string value) => UpdatePreview();

    private void UpdatePreview()
    {
        var now = DateTime.Now;
        FileNamePreview = FileNameTemplate
            .Replace("%[DocName]%", "Договор")
            .Replace("%[AppName]%", "Microsoft Word")
            .Replace("%[Year]%",    now.ToString("yyyy"))
            .Replace("%[Month]%",   now.ToString("MM"))
            .Replace("%[Day]%",     now.ToString("dd"))
            .Replace("%[Hour]%",    now.ToString("HH"))
            .Replace("%[Minute]%",  now.ToString("mm"))
            .Replace("%[Second]%",  now.ToString("ss"))
            + ".pdf";
    }

    [RelayCommand]
    void BrowseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            SelectedPath = OutputFolder
        };
        if (dialog.ShowDialog() == DialogResult.OK)
            OutputFolder = dialog.SelectedPath;
    }

    [RelayCommand]
    void Save()
    {
        var settings = new PrinterSettings
        {
            ShowSaveDialog       = ShowSaveDialog,
            OutputFolder         = OutputFolder,
            FileNameTemplate     = FileNameTemplate,
            StripPathFromDocName = StripPathFromDocName,
            ConflictStrategy     = ConflictStrategy,
            Quality              = Quality
        };
        settings.Save();
        System.Windows.Application.Current?.Shutdown();
    }

    [RelayCommand]
    void Cancel() => System.Windows.Application.Current?.Shutdown();
}
