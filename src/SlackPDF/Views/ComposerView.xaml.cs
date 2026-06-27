using SlackPDF.ViewModels;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Input;

namespace SlackPDF.Views;

public partial class ComposerView : UserControl
{
    private ComposerPageThumb? _dragSource;
    private Point _dragStartPos;
    private bool _isDragging;
    private int _lastClickedIndex = -1; // anchor for Shift+click range select

    public ComposerView()
    {
        InitializeComponent();
        Loaded += OnViewLoaded;
    }

    // Restore column width from ViewModel after the view is recreated (navigation away/back)
    private void OnViewLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ComposerViewModel vm)
            SourceColumn.Width = new GridLength(vm.SourcePanelWidth, GridUnitType.Pixel);

        // Save width only when the user finishes dragging the splitter,
        // NOT on every SizeChanged (which fires during initial layout and would overwrite the stored value)
        SourceSplitter.AddHandler(
            Thumb.DragCompletedEvent,
            new DragCompletedEventHandler(OnSplitterDragCompleted));
    }

    private void OnSplitterDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (DataContext is ComposerViewModel vm)
            vm.SourcePanelWidth = SourceColumn.ActualWidth;
    }

    private void SourceThumb_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ComposerPageThumb thumb)
        {
            _dragStartPos = e.GetPosition(null);
            _isDragging = false;
            _dragSource = thumb;

            var pages = (DataContext as ComposerViewModel)?.ActiveDocument?.Pages;

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && pages != null)
            {
                // Range select from the last anchor to current
                int currentIdx = pages.IndexOf(thumb);
                int anchorIdx = _lastClickedIndex >= 0 && _lastClickedIndex < pages.Count
                    ? _lastClickedIndex : currentIdx;

                int from = Math.Min(anchorIdx, currentIdx);
                int to   = Math.Max(anchorIdx, currentIdx);
                foreach (var t in pages) t.IsSelected = false;
                for (int i = from; i <= to; i++) pages[i].IsSelected = true;
                // Shift+click does NOT move the anchor (_lastClickedIndex stays)
            }
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                // Toggle this thumb without disturbing others
                thumb.IsSelected = !thumb.IsSelected;
                if (pages != null) _lastClickedIndex = pages.IndexOf(thumb);
            }
            // Plain click: defer selection change to MouseUp so dragging already-selected
            // items doesn't first deselect them. We need MouseUp to know if drag happened.
        }
    }

    private void SourceThumb_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragSource == null || _isDragging) return;
        if (sender is not UIElement element) return;

        Point pos = e.GetPosition(null);
        if (Math.Abs(pos.X - _dragStartPos.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - _dragStartPos.Y) < SystemParameters.MinimumVerticalDragDistance)
            return; // movement below drag threshold — still a potential click

        _isDragging = true;

        List<PageDragItem> pagesToDrag = [];
        if (DataContext is ComposerViewModel vm && vm.ActiveDocument != null)
        {
            var pages = vm.ActiveDocument.Pages;

            if (_dragSource.IsSelected)
            {
                // Drag source is part of the selection → drag everything selected
                pagesToDrag = pages
                    .Where(p => p.IsSelected)
                    .OrderBy(p => p.PageIndex)
                    .Select(p => new PageDragItem { FilePath = p.FilePath, PageIndex = p.PageIndex })
                    .ToList();
            }
            else
            {
                // Dragging an unselected item → clear selection, select+drag only this one
                foreach (var t in pages) t.IsSelected = false;
                _dragSource.IsSelected = true;
                _lastClickedIndex = pages.IndexOf(_dragSource);
                pagesToDrag = [new PageDragItem { FilePath = _dragSource.FilePath, PageIndex = _dragSource.PageIndex }];
            }
        }

        if (pagesToDrag.Count == 0)
            pagesToDrag = [new PageDragItem { FilePath = _dragSource.FilePath, PageIndex = _dragSource.PageIndex }];

        // DoDragDrop blocks until the user drops or cancels
        DragDrop.DoDragDrop(element, new DataObject("SlackPdfPages", pagesToDrag), DragDropEffects.Copy);
        _dragSource = null;
        _isDragging = false;
    }

    private void SourceThumb_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // _dragSource is null after a completed drag, so this only runs for plain clicks
        if (_dragSource == null) return;

        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) &&
            !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            // Plain click: select only the clicked thumb
            if (DataContext is ComposerViewModel vm && vm.ActiveDocument != null)
            {
                var pages = vm.ActiveDocument.Pages;
                foreach (var t in pages) t.IsSelected = false;
                _dragSource.IsSelected = true;
                _lastClickedIndex = pages.IndexOf(_dragSource);
            }
        }
        _dragSource = null;
    }

    private void Assembly_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent("SlackPdfPages")
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Assembly_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData("SlackPdfPages") is not List<PageDragItem> pages) return;
        if (DataContext is not ComposerViewModel vm) return;
        foreach (var p in pages)
            vm.InsertPage(p.FilePath, p.PageIndex, vm.ComposedPages.Count);
    }
}

public class PageDragItem
{
    public string FilePath { get; set; } = string.Empty;
    public int    PageIndex { get; set; }
}
