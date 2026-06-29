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
    [ObservableProperty] private MixFileEntry? _selectedFile;

    public MixViewModel(PdfOperations ops) : base(ops) { }

    partial void OnSelectedFileChanged(MixFileEntry? value)
    {
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
        RemoveFileCommand.NotifyCanExecuteChanged();
    }

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

    public void AddFile(string path)
    {
        if (Files.Any(x => x.FilePath == path)) return;
        try
        {
            using var doc = PdfSharp.Pdf.IO.PdfReader.Open(path, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
            Files.Add(new MixFileEntry { FilePath = path, FileName = Path.GetFileName(path), PageCount = doc.PageCount });
        }
        catch { }
    }

    [RelayCommand(CanExecute = nameof(CanActOnSelection))]
    private void MoveUp()
    {
        if (SelectedFile is not { } file) return;
        int i = Files.IndexOf(file);
        if (i > 0) Files.Move(i, i - 1);
    }

    [RelayCommand(CanExecute = nameof(CanActOnSelection))]
    private void MoveDown()
    {
        if (SelectedFile is not { } file) return;
        int i = Files.IndexOf(file);
        if (i < Files.Count - 1) Files.Move(i, i + 1);
    }

    [RelayCommand(CanExecute = nameof(CanActOnSelection))]
    private void RemoveFile()
    {
        if (SelectedFile is not { } file) return;
        Files.Remove(file);
        SelectedFile = null;
    }

    private bool CanActOnSelection() => SelectedFile != null;

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
