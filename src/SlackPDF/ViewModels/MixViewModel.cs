using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SlackPDF.Core;
using SlackPDF.Core.Models;
using System.Collections.ObjectModel;

namespace SlackPDF.ViewModels;

public partial class MixFileEntry : ObservableObject
{
    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _fileName = string.Empty;
    [ObservableProperty] private int _pageCount;
    [ObservableProperty] private bool _reverse;
}

public partial class MixViewModel : BaseOperationViewModel
{
    [ObservableProperty] private ObservableCollection<MixFileEntry> _files = [];

    public MixViewModel(PdfOperations ops) : base(ops) { }

    [RelayCommand]
    private void AddFiles()
    {
        var dlg = new OpenFileDialog { Filter = "PDF files (*.pdf)|*.pdf", Multiselect = true };
        if (dlg.ShowDialog() != true) return;
        foreach (var f in dlg.FileNames)
        {
            if (Files.Any(x => x.FilePath == f)) continue;
            try
            {
                using var doc = PdfSharp.Pdf.IO.PdfReader.Open(f, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
                Files.Add(new MixFileEntry
                {
                    FilePath = f,
                    FileName = Path.GetFileName(f),
                    PageCount = doc.PageCount
                });
            }
            catch { }
        }
    }

    [RelayCommand]
    private void RemoveFile(MixFileEntry entry) => Files.Remove(entry);

    [RelayCommand]
    private void ClearAll() => Files.Clear();

    [RelayCommand]
    private async Task RunAsync()
    {
        if (Files.Count < 2 || string.IsNullOrWhiteSpace(OutputPath)) return;
        await RunOperationAsync((progress, ct) =>
        {
            var inputs = Files.Select(f => (f.FilePath, f.Reverse));
            return _ops.MixAsync(inputs, OutputPath, progress, ct);
        });
    }
}
