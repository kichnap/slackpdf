using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SlackPDF.Core;
using SlackPDF.Core.Models;
using SlackPDF.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SlackPDF.ViewModels;

public abstract partial class BaseOperationViewModel : ObservableObject
{
    protected readonly PdfOperations _ops;

    [ObservableProperty] private int _progress;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _outputPath = string.Empty;

    private CancellationTokenSource? _cts;

    protected BaseOperationViewModel(PdfOperations ops)
    {
        _ops = ops;
    }

    [RelayCommand]
    protected void BrowseOutput()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            DefaultExt = ".pdf"
        };
        if (dlg.ShowDialog() == true)
            OutputPath = dlg.FileName;
    }

    [RelayCommand]
    protected void Cancel()
    {
        _cts?.Cancel();
    }

    protected async Task RunOperationAsync(Func<IProgress<int>, CancellationToken, Task<OperationResult>> operation)
    {
        if (IsBusy) return;
        _cts = new CancellationTokenSource();
        IsBusy = true;
        Progress = 0;
        StatusMessage = string.Empty;

        try
        {
            var prog = new Progress<int>(p => Progress = p);
            // Run on a thread-pool thread so the UI stays responsive
            var result = await Task.Run(async () => await operation(prog, _cts.Token));

            if (result.Success)
            {
                StatusMessage = Localization.LocalizationManager.Get("Common.Success", result.OutputPath ?? string.Empty);
                ApplyPostSaveAction(result.OutputPath);
            }
            else
            {
                StatusMessage = Localization.LocalizationManager.Get("Common.Error", result.ErrorMessage ?? "Unknown error");
            }
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private static void ApplyPostSaveAction(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            switch (SettingsService.Current.PostSave)
            {
                case PostSaveAction.OpenFolder:
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                    break;
                case PostSaveAction.OpenFile:
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                    break;
            }
        }
        catch { }
    }

    protected static PdfFileInfo? OpenPdfFile()
    {
        var dlg = new OpenFileDialog { Filter = "PDF files (*.pdf)|*.pdf" };
        if (dlg.ShowDialog() != true) return null;
        try
        {
            using var doc = PdfSharp.Pdf.IO.PdfReader.Open(dlg.FileName, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
            return new PdfFileInfo(dlg.FileName, Path.GetFileName(dlg.FileName),
                doc.PageCount, new FileInfo(dlg.FileName).Length, PageSelection.All);
        }
        catch { return null; }
    }
}
