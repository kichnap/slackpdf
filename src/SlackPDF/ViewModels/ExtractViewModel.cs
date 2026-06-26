using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SlackPDF.Core;
using SlackPDF.Core.Models;
using SlackPDF.Services;

namespace SlackPDF.ViewModels;

public partial class ExtractViewModel : BaseOperationViewModel
{
    private readonly ThumbnailService _thumbs;

    [ObservableProperty] private string _inputFilePath = string.Empty;
    [ObservableProperty] private string _inputFileName = string.Empty;
    [ObservableProperty] private int _inputPageCount;
    [ObservableProperty] private string _pageSelectionText = string.Empty;
    [ObservableProperty] private ExtractMode _selectedMode = ExtractMode.SingleFile;

    public ExtractViewModel(PdfOperations ops, ThumbnailService thumbs) : base(ops)
    {
        _thumbs = thumbs;
    }

    public IEnumerable<ExtractMode> ExtractModes => Enum.GetValues<ExtractMode>();

    [RelayCommand]
    private void BrowseInput()
    {
        var dlg = new OpenFileDialog { Filter = "PDF files (*.pdf)|*.pdf" };
        if (dlg.ShowDialog() != true) return;
        InputFilePath = dlg.FileName;
        InputFileName = Path.GetFileName(dlg.FileName);
        try
        {
            using var doc = PdfSharp.Pdf.IO.PdfReader.Open(dlg.FileName, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
            InputPageCount = doc.PageCount;
        }
        catch { InputPageCount = 0; }
    }

    [RelayCommand]
    private async Task RunAsync()
    {
        if (string.IsNullOrWhiteSpace(InputFilePath) || string.IsNullOrWhiteSpace(OutputPath)) return;
        var pages = PageSelection.Parse(PageSelectionText);
        await RunOperationAsync((progress, ct) =>
            _ops.ExtractAsync(InputFilePath, OutputPath, pages, SelectedMode, progress, ct));
    }
}
