using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SlackPDF.Core;
using SlackPDF.Core.Models;

namespace SlackPDF.ViewModels;

public partial class SplitViewModel : BaseOperationViewModel
{
    [ObservableProperty] private string _inputFilePath = string.Empty;
    [ObservableProperty] private string _inputFileName = string.Empty;
    [ObservableProperty] private int _inputPageCount;
    [ObservableProperty] private SplitMode _selectedMode = SplitMode.EveryPage;
    [ObservableProperty] private int _nPages = 2;
    [ObservableProperty] private string _atPagesText = string.Empty;
    [ObservableProperty] private double _maxSizeMb = 5.0;
    [ObservableProperty] private int _bookmarkLevel = 1;
    [ObservableProperty] private string _fileNamePrefix = string.Empty;
    [ObservableProperty] private string _outputFolder = string.Empty;

    public SplitViewModel(PdfOperations ops) : base(ops) { }

    public IEnumerable<SplitMode> SplitModes => Enum.GetValues<SplitMode>();

    public void SetInputFile(string path)
    {
        InputFilePath = path;
        InputFileName = Path.GetFileName(path);
        FileNamePrefix = Path.GetFileNameWithoutExtension(path);
        try
        {
            using var doc = PdfSharp.Pdf.IO.PdfReader.Open(path, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
            InputPageCount = doc.PageCount;
        }
        catch { InputPageCount = 0; }
    }

    [RelayCommand]
    private void BrowseInput()
    {
        var dlg = new OpenFileDialog { Filter = "PDF files (*.pdf)|*.pdf" };
        if (dlg.ShowDialog() != true) return;
        SetInputFile(dlg.FileName);
    }

    [RelayCommand]
    private void BrowseOutputFolder()
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "Select output folder" };
        if (dlg.ShowDialog() == true)
            OutputFolder = dlg.FolderName;
    }

    [RelayCommand]
    private async Task RunAsync()
    {
        if (string.IsNullOrWhiteSpace(InputFilePath)) return;

        var outputFolder = string.IsNullOrWhiteSpace(OutputFolder)
            ? Path.GetDirectoryName(InputFilePath)!
            : OutputFolder;

        int[]? atPages = null;
        if (SelectedMode == SplitMode.AtPages && !string.IsNullOrWhiteSpace(AtPagesText))
            atPages = AtPagesText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p => int.TryParse(p, out int n) ? n : -1)
                .Where(n => n > 0)
                .ToArray();

        var options = new SplitOptions(SelectedMode, NPages, atPages, MaxSizeMb, BookmarkLevel, FileNamePrefix);
        await RunOperationAsync((progress, ct) =>
            _ops.SplitAsync(InputFilePath, outputFolder, options, progress, ct));
    }
}
