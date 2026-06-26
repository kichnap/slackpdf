using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SlackPDF.Core;
using SlackPDF.Core.Models;

namespace SlackPDF.ViewModels;

public partial class RotateViewModel : BaseOperationViewModel
{
    [ObservableProperty] private string _inputFilePath = string.Empty;
    [ObservableProperty] private string _inputFileName = string.Empty;
    [ObservableProperty] private int _inputPageCount;
    [ObservableProperty] private int _selectedAngle = 90;
    [ObservableProperty] private string _pageSelectionMode = "All";
    [ObservableProperty] private string _pageSelectionText = string.Empty;
    [ObservableProperty] private bool _overwriteOriginal;

    public int[] AvailableAngles { get; } = [90, 180, 270];

    public RotateViewModel(PdfOperations ops) : base(ops) { }

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
        if (string.IsNullOrWhiteSpace(InputFilePath)) return;
        string outPath = OverwriteOriginal ? InputFilePath : OutputPath;
        if (!OverwriteOriginal && string.IsNullOrWhiteSpace(outPath)) return;

        var pages = PageSelectionMode switch
        {
            "Even" => PageSelection.Parse(
                string.Join(",", Enumerable.Range(1, InputPageCount).Where(p => p % 2 == 0))),
            "Odd" => PageSelection.Parse(
                string.Join(",", Enumerable.Range(1, InputPageCount).Where(p => p % 2 != 0))),
            "Custom" => PageSelection.Parse(PageSelectionText),
            _ => PageSelection.All
        };

        await RunOperationAsync((progress, ct) =>
            _ops.RotateAsync(InputFilePath, outPath, SelectedAngle, pages, progress, ct));
    }
}
