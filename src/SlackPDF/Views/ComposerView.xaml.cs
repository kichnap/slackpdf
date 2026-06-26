using SlackPDF.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SlackPDF.Views;

public partial class ComposerView : UserControl
{
    private ComposerPageThumb? _dragSource;

    public ComposerView()
    {
        InitializeComponent();
    }

    private void SourceThumb_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ComposerPageThumb thumb)
            _dragSource = thumb;
    }

    private void SourceThumb_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragSource == null) return;
        if (sender is not UIElement element) return;

        var data = new DataObject("SlackPdfPage", new PageDragData
        {
            SourceFilePath = _dragSource.FilePath,
            PageIndex      = _dragSource.PageIndex
        });
        DragDrop.DoDragDrop(element, data, DragDropEffects.Copy);
        _dragSource = null;
    }

    private void Assembly_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent("SlackPdfPage")
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Assembly_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData("SlackPdfPage") is not PageDragData pd) return;
        if (DataContext is not ComposerViewModel vm) return;
        vm.InsertPage(pd.SourceFilePath, pd.PageIndex, vm.ComposedPages.Count);
    }
}

public class PageDragData
{
    public string SourceFilePath { get; set; } = string.Empty;
    public int    PageIndex      { get; set; }
}
