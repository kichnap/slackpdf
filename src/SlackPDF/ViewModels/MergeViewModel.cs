using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SlackPDF.Core;
using SlackPDF.Core.Models;
using System.Collections.ObjectModel;

namespace SlackPDF.ViewModels;

public partial class MergeViewModel : BaseOperationViewModel
{
    [ObservableProperty] private ObservableCollection<PdfFileInfo> _files = [];
    [ObservableProperty] private BookmarkBehavior _selectedBookmarks = BookmarkBehavior.Merge;
    [ObservableProperty] private AcroFormBehavior _selectedAcroForms = AcroFormBehavior.Discard;
    [ObservableProperty] private bool _addTableOfContents;
    [ObservableProperty] private PdfFileInfo? _selectedFile;

    public MergeViewModel(PdfOperations ops) : base(ops) { }

    public int SelectedBookmarkIndex
    {
        get => (int)SelectedBookmarks;
        set => SelectedBookmarks = (BookmarkBehavior)value;
    }

    public int SelectedAcroFormIndex
    {
        get => (int)SelectedAcroForms;
        set => SelectedAcroForms = (AcroFormBehavior)value;
    }

    partial void OnSelectedBookmarksChanged(BookmarkBehavior value)
        => OnPropertyChanged(nameof(SelectedBookmarkIndex));

    partial void OnSelectedAcroFormsChanged(AcroFormBehavior value)
        => OnPropertyChanged(nameof(SelectedAcroFormIndex));

    [RelayCommand]
    private void AddFiles()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            Multiselect = true
        };
        if (dlg.ShowDialog() != true) return;
        foreach (var f in dlg.FileNames)
            AddFile(f);
    }

    public void AddFile(string filePath)
    {
        if (Files.Any(f => f.FilePath == filePath)) return;
        try
        {
            using var doc = PdfSharp.Pdf.IO.PdfReader.Open(filePath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
            Files.Add(new PdfFileInfo(filePath, Path.GetFileName(filePath),
                doc.PageCount, new FileInfo(filePath).Length, PageSelection.All));
        }
        catch { }
    }

    [RelayCommand]
    private void RemoveFile(PdfFileInfo file) => Files.Remove(file);

    [RelayCommand]
    private void MoveUp(PdfFileInfo file)
    {
        int i = Files.IndexOf(file);
        if (i > 0) Files.Move(i, i - 1);
    }

    [RelayCommand]
    private void MoveDown(PdfFileInfo file)
    {
        int i = Files.IndexOf(file);
        if (i < Files.Count - 1) Files.Move(i, i + 1);
    }

    [RelayCommand]
    private void ClearAll() => Files.Clear();

    [RelayCommand]
    private async Task RunAsync()
    {
        if (Files.Count < 2 || string.IsNullOrWhiteSpace(OutputPath)) return;
        await RunOperationAsync((progress, ct) =>
        {
            var inputs = Files.Select(f => (f.FilePath, f.Selection));
            var options = new MergeOptions(SelectedBookmarks, SelectedAcroForms, AddTableOfContents);
            return _ops.MergeAsync(inputs, OutputPath, options, progress, ct);
        });
    }
}
