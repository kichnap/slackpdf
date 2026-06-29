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

    // Primary selected item — bound to DataGrid.SelectedItem for visual focus tracking.
    [ObservableProperty] private PdfFileInfo? _selectedFile;

    // All currently selected items — populated by MergeView.xaml.cs on SelectionChanged.
    public List<PdfFileInfo> SelectedFiles { get; set; } = [];

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

    // Called by code-behind after DataGrid.SelectionChanged.
    public void NotifySelectionChanged()
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

    [RelayCommand(CanExecute = nameof(CanActOnSelection))]
    private void MoveUp()
    {
        // Move all selected items up; process in ascending index order so earlier
        // items don't push later ones out of position.
        var toMove = SelectedFiles.OrderBy(f => Files.IndexOf(f)).ToList();
        foreach (var file in toMove)
        {
            int i = Files.IndexOf(file);
            // Skip if at top OR if the item above is also selected (the group is already packed).
            if (i > 0 && !toMove.Contains(Files[i - 1]))
                Files.Move(i, i - 1);
        }
    }

    [RelayCommand(CanExecute = nameof(CanActOnSelection))]
    private void MoveDown()
    {
        // Process in descending index order so later items don't push earlier ones.
        var toMove = SelectedFiles.OrderByDescending(f => Files.IndexOf(f)).ToList();
        foreach (var file in toMove)
        {
            int i = Files.IndexOf(file);
            if (i < Files.Count - 1 && !toMove.Contains(Files[i + 1]))
                Files.Move(i, i + 1);
        }
    }

    [RelayCommand(CanExecute = nameof(CanActOnSelection))]
    private void RemoveFile()
    {
        foreach (var file in SelectedFiles.ToList())
            Files.Remove(file);
        SelectedFiles.Clear();
        SelectedFile = null;
    }

    private bool CanActOnSelection() => SelectedFiles.Count > 0;

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
