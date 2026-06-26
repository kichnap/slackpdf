using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SlackPDF.Core;
using SlackPDF.Core.Models;
using SlackPDF.Services;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SlackPDF.ViewModels;

public partial class ComposerPage : ObservableObject
{
    public required string SourceFilePath { get; init; }
    public required int SourcePageIndex  { get; init; }
    public required string DocumentLabel { get; init; }
    public required Color  DocumentColor { get; init; }

    [ObservableProperty] private BitmapSource? _thumbnail;

    public string DisplayLabel => $"{DocumentLabel}·p.{SourcePageIndex + 1}";

    public SolidColorBrush ColorBrush => new(DocumentColor);
    public SolidColorBrush FadedColorBrush => new(Color.FromArgb(40, DocumentColor.R, DocumentColor.G, DocumentColor.B));
}

public partial class ComposerDocument : ObservableObject
{
    public required PdfFileInfo Info { get; init; }
    public required string Label    { get; init; }
    public required Color  Color    { get; init; }
    public ObservableCollection<ComposerPageThumb> Pages { get; } = [];
    public SolidColorBrush ColorBrush => new(Color);
}

public partial class ComposerPageThumb : ObservableObject
{
    public required string FilePath  { get; init; }
    public required int    PageIndex { get; init; }
    public required string Label     { get; init; }
    [ObservableProperty] private BitmapSource? _thumbnail;
    [ObservableProperty] private bool _isSelected;
}

public partial class ComposerViewModel : BaseOperationViewModel
{
    private static readonly Color[] Palette =
    [
        Color.FromRgb(103, 58, 183),  // DeepPurple
        Color.FromRgb(76, 175, 80),   // Green
        Color.FromRgb(255, 152, 0),   // Orange
        Color.FromRgb(33, 150, 243),  // Blue
        Color.FromRgb(233, 30, 99),   // Pink
        Color.FromRgb(255, 193, 7),   // Amber
        Color.FromRgb(244, 67, 54),   // Red
    ];

    private readonly ThumbnailService _thumbs;

    [ObservableProperty] private ObservableCollection<ComposerDocument> _documents = [];
    [ObservableProperty] private ObservableCollection<ComposerPage> _composedPages = [];
    [ObservableProperty] private ComposerDocument? _activeDocument;

    public ComposerViewModel(PdfOperations ops, ThumbnailService thumbs) : base(ops)
    {
        _thumbs = thumbs;
    }

    public string AssemblyCountLabel =>
        Localization.LocalizationManager.Get("Composer.PageCount", ComposedPages.Count);

    [RelayCommand]
    private async Task AddDocumentAsync()
    {
        var dlg = new OpenFileDialog { Filter = "PDF files (*.pdf)|*.pdf" };
        if (dlg.ShowDialog() != true) return;

        try
        {
            int labelIdx = Documents.Count;
            string label = labelIdx < 26
                ? ((char)('A' + labelIdx)).ToString()
                : $"#{labelIdx + 1}";
            var color = Palette[labelIdx % Palette.Length];

            using var doc = PdfSharp.Pdf.IO.PdfReader.Open(dlg.FileName, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
            var info = new PdfFileInfo(dlg.FileName, Path.GetFileName(dlg.FileName),
                doc.PageCount, new FileInfo(dlg.FileName).Length, PageSelection.All)
            { Label = label, Color = color };

            var compDoc = new ComposerDocument { Info = info, Label = label, Color = color };
            Documents.Add(compDoc);
            ActiveDocument = compDoc;

            _ = LoadDocumentThumbnailsAsync(compDoc);
        }
        catch { }
    }

    private async Task LoadDocumentThumbnailsAsync(ComposerDocument doc)
    {
        for (int i = 0; i < doc.Info.PageCount; i++)
        {
            var thumb = new ComposerPageThumb
            {
                FilePath = doc.Info.FilePath,
                PageIndex = i,
                Label = $"{doc.Label}·{i + 1}"
            };
            doc.Pages.Add(thumb);
            var bmp = await _thumbs.GetThumbnailAsync(doc.Info.FilePath, i);
            thumb.Thumbnail = bmp;
        }
    }

    public void InsertPage(string filePath, int pageIndex, int insertIndex)
    {
        var doc = Documents.FirstOrDefault(d => d.Info.FilePath == filePath);
        if (doc == null) return;

        var page = new ComposerPage
        {
            SourceFilePath  = filePath,
            SourcePageIndex = pageIndex,
            DocumentLabel   = doc.Label,
            DocumentColor   = doc.Color
        };

        if (insertIndex < 0 || insertIndex >= ComposedPages.Count)
            ComposedPages.Add(page);
        else
            ComposedPages.Insert(insertIndex, page);

        OnPropertyChanged(nameof(AssemblyCountLabel));
        _ = LoadPageThumbnailAsync(page);
    }

    private async Task LoadPageThumbnailAsync(ComposerPage page)
    {
        var bmp = await _thumbs.GetThumbnailAsync(page.SourceFilePath, page.SourcePageIndex);
        page.Thumbnail = bmp;
    }

    [RelayCommand]
    private void RemovePage(ComposerPage page)
    {
        ComposedPages.Remove(page);
        OnPropertyChanged(nameof(AssemblyCountLabel));
    }

    [RelayCommand]
    private void MovePageFirst(ComposerPage page)
    {
        ComposedPages.Remove(page);
        ComposedPages.Insert(0, page);
        OnPropertyChanged(nameof(AssemblyCountLabel));
    }

    [RelayCommand]
    private void MovePageLast(ComposerPage page)
    {
        ComposedPages.Remove(page);
        ComposedPages.Add(page);
        OnPropertyChanged(nameof(AssemblyCountLabel));
    }

    [RelayCommand]
    private void DuplicatePage(ComposerPage page)
    {
        int idx = ComposedPages.IndexOf(page);
        var copy = new ComposerPage
        {
            SourceFilePath  = page.SourceFilePath,
            SourcePageIndex = page.SourcePageIndex,
            DocumentLabel   = page.DocumentLabel,
            DocumentColor   = page.DocumentColor,
            Thumbnail       = page.Thumbnail
        };
        ComposedPages.Insert(idx + 1, copy);
        OnPropertyChanged(nameof(AssemblyCountLabel));
    }

    [RelayCommand]
    private void ClearAll()
    {
        ComposedPages.Clear();
        OnPropertyChanged(nameof(AssemblyCountLabel));
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (ComposedPages.Count == 0) return;
        var dlg = new SaveFileDialog { Filter = "PDF files (*.pdf)|*.pdf", DefaultExt = ".pdf" };
        if (dlg.ShowDialog() != true) return;
        OutputPath = dlg.FileName;

        var sequence = ComposedPages.Select(p => (p.SourceFilePath, p.SourcePageIndex));
        await RunOperationAsync((progress, ct) =>
            _ops.ComposeAsync(sequence, OutputPath, progress, ct));
    }

    [RelayCommand]
    private void AutoOrder()
    {
        var sorted = ComposedPages
            .OrderBy(p => p.DocumentLabel)
            .ThenBy(p => p.SourcePageIndex)
            .ToList();
        ComposedPages.Clear();
        foreach (var p in sorted)
            ComposedPages.Add(p);
        OnPropertyChanged(nameof(AssemblyCountLabel));
    }
}
