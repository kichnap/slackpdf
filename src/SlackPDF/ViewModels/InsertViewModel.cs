using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SlackPDF.Core;
using SlackPDF.Core.Models;

namespace SlackPDF.ViewModels;

public partial class InsertViewModel : BaseOperationViewModel
{
    [ObservableProperty] private string _baseFilePath = string.Empty;
    [ObservableProperty] private string _baseFileName = string.Empty;
    [ObservableProperty] private int _basePageCount;
    [ObservableProperty] private string _insertFilePath = string.Empty;
    [ObservableProperty] private string _insertFileName = string.Empty;
    [ObservableProperty] private int _insertPageCount;
    [ObservableProperty] private string _insertPageSelectionText = string.Empty;
    [ObservableProperty] private InsertMode _selectedMode = InsertMode.AtPosition;
    [ObservableProperty] private int _position = 1;

    public IEnumerable<InsertMode> InsertModes => Enum.GetValues<InsertMode>();

    public InsertViewModel(PdfOperations ops) : base(ops) { }

    [RelayCommand]
    private void BrowseBase()
    {
        var dlg = new OpenFileDialog { Filter = "PDF files (*.pdf)|*.pdf" };
        if (dlg.ShowDialog() != true) return;
        BaseFilePath = dlg.FileName;
        BaseFileName = Path.GetFileName(dlg.FileName);
        try
        {
            using var doc = PdfSharp.Pdf.IO.PdfReader.Open(dlg.FileName, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
            BasePageCount = doc.PageCount;
        }
        catch { BasePageCount = 0; }
    }

    [RelayCommand]
    private void BrowseInsert()
    {
        var dlg = new OpenFileDialog { Filter = "PDF files (*.pdf)|*.pdf" };
        if (dlg.ShowDialog() != true) return;
        InsertFilePath = dlg.FileName;
        InsertFileName = Path.GetFileName(dlg.FileName);
        try
        {
            using var doc = PdfSharp.Pdf.IO.PdfReader.Open(dlg.FileName, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
            InsertPageCount = doc.PageCount;
        }
        catch { InsertPageCount = 0; }
    }

    [RelayCommand]
    private async Task RunAsync()
    {
        if (string.IsNullOrWhiteSpace(BaseFilePath) ||
            string.IsNullOrWhiteSpace(InsertFilePath) ||
            string.IsNullOrWhiteSpace(OutputPath)) return;

        var insertPages = PageSelection.Parse(InsertPageSelectionText);
        var options = new InsertOptions(SelectedMode, Position);
        await RunOperationAsync((progress, ct) =>
            _ops.InsertAsync(BaseFilePath, InsertFilePath, insertPages, options, OutputPath, progress, ct));
    }
}
