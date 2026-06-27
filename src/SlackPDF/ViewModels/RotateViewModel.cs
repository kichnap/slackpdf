using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SlackPDF.Core;
using SlackPDF.Core.Models;
using SlackPDF.Services;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace SlackPDF.ViewModels;

public partial class RotatePageThumb : ObservableObject
{
    public required int PageIndex { get; init; }
    public int PageNumber => PageIndex + 1;

    [ObservableProperty] private BitmapSource? _thumbnail;
    [ObservableProperty] private bool _thumbnailLoaded;
    [ObservableProperty] private bool _isSelected;

    public bool ThumbnailFailed => ThumbnailLoaded && Thumbnail == null;

    partial void OnThumbnailLoadedChanged(bool value) => OnPropertyChanged(nameof(ThumbnailFailed));
    partial void OnThumbnailChanged(BitmapSource? value) => OnPropertyChanged(nameof(ThumbnailFailed));
}

public partial class RotateViewModel : BaseOperationViewModel
{
    private readonly ThumbnailService _thumbs;

    [ObservableProperty] private string _inputFilePath = string.Empty;
    [ObservableProperty] private string _inputFileName = string.Empty;
    [ObservableProperty] private int _inputPageCount;
    [ObservableProperty] private int _selectedAngle = 90;
    [ObservableProperty] private string _pageSelectionMode = "All";
    [ObservableProperty] private string _pageSelectionText = string.Empty;
    [ObservableProperty] private bool _overwriteOriginal;
    [ObservableProperty] private bool _isPagePickerOpen;
    [ObservableProperty] private bool _isLoadingThumbs;
    [ObservableProperty] private ObservableCollection<RotatePageThumb> _pageThumbs = [];

    public int[] AvailableAngles { get; } = [90, 180, 270];

    public RotateViewModel(PdfOperations ops, ThumbnailService thumbs) : base(ops)
    {
        _thumbs = thumbs;
    }

    public void SetInputFile(string path)
    {
        InputFilePath = path;
        InputFileName = Path.GetFileName(path);
        PageThumbs.Clear();
        IsPagePickerOpen = false;
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

    partial void OnInputFilePathChanged(string value) => OpenPagePickerCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanOpenPagePicker))]
    private async Task OpenPagePickerAsync()
    {
        if (string.IsNullOrWhiteSpace(InputFilePath)) return;

        if (PageThumbs.Count == 0)
        {
            IsLoadingThumbs = true;
            await Task.Delay(1);
            try
            {
                // Seed thumbs with placeholders first
                for (int i = 0; i < InputPageCount; i++)
                    PageThumbs.Add(new RotatePageThumb { PageIndex = i });

                // Pre-select pages already listed in PageSelectionText when mode is Custom
                if (PageSelectionMode == "Custom" && !string.IsNullOrWhiteSpace(PageSelectionText))
                {
                    var sel = PageSelection.Parse(PageSelectionText);
                    foreach (var t in PageThumbs)
                        t.IsSelected = sel.Contains(t.PageNumber);
                }

                // Load thumbnails in background
                _ = LoadThumbsInBackgroundAsync();
            }
            finally
            {
                IsLoadingThumbs = false;
            }
        }

        IsPagePickerOpen = true;
    }

    private bool CanOpenPagePicker() => !string.IsNullOrWhiteSpace(InputFilePath);

    private async Task LoadThumbsInBackgroundAsync()
    {
        foreach (var thumb in PageThumbs.ToList())
        {
            var bmp = await _thumbs.GetThumbnailAsync(InputFilePath, thumb.PageIndex);
            thumb.Thumbnail = bmp;
            thumb.ThumbnailLoaded = true;
        }
    }

    [RelayCommand]
    private void TogglePage(RotatePageThumb thumb)
    {
        thumb.IsSelected = !thumb.IsSelected;
    }

    [RelayCommand]
    private void ApplyPagePicker()
    {
        var selected = PageThumbs.Where(t => t.IsSelected).Select(t => t.PageNumber).OrderBy(n => n).ToList();
        if (selected.Count == 0 || selected.Count == InputPageCount)
        {
            PageSelectionMode = "All";
            PageSelectionText = string.Empty;
        }
        else
        {
            PageSelectionText = FormatPageNumbers(selected);
            PageSelectionMode = "Custom";
        }
        IsPagePickerOpen = false;
    }

    [RelayCommand]
    private void ClosePagePicker()
    {
        IsPagePickerOpen = false;
    }

    [RelayCommand]
    private void PickerSelectAll()
    {
        foreach (var t in PageThumbs) t.IsSelected = true;
    }

    [RelayCommand]
    private void PickerClearAll()
    {
        foreach (var t in PageThumbs) t.IsSelected = false;
    }

    private static string FormatPageNumbers(IList<int> sorted)
    {
        var parts = new List<string>();
        int i = 0;
        while (i < sorted.Count)
        {
            int start = sorted[i];
            int end = start;
            while (i + 1 < sorted.Count && sorted[i + 1] == end + 1)
            {
                i++;
                end = sorted[i];
            }
            parts.Add(end == start ? $"{start}" : $"{start}-{end}");
            i++;
        }
        return string.Join(",", parts);
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
