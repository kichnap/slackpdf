using SlackPDF.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace SlackPDF.Views;

public partial class ComposerView : UserControl
{
    private ComposerPageThumb? _dragSource;
    private Point _dragStartPos;
    private bool _isDragging;
    private int _lastClickedIndex = -1;
    private int _insertionIndex = -1;
    private InsertAdorner? _insertAdorner;

    // Assembly reorder state
    private ComposerPage? _assemblyDragSource;
    private Point _assemblyDragStartPos;

    public ComposerView()
    {
        InitializeComponent();
        Loaded += OnViewLoaded;
    }

    private void OnViewLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ComposerViewModel vm)
            SourceColumn.Width = new GridLength(vm.SourcePanelWidth, GridUnitType.Pixel);

        SourceSplitter.AddHandler(
            Thumb.DragCompletedEvent,
            new DragCompletedEventHandler(OnSplitterDragCompleted));
    }

    private void OnSplitterDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (DataContext is ComposerViewModel vm)
            vm.SourcePanelWidth = SourceColumn.ActualWidth;
    }

    // ─── Source thumbnail selection ─────────────────────────────────────────

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
                int currentIdx = pages.IndexOf(thumb);
                int anchorIdx = _lastClickedIndex >= 0 && _lastClickedIndex < pages.Count
                    ? _lastClickedIndex : currentIdx;

                int from = Math.Min(anchorIdx, currentIdx);
                int to   = Math.Max(anchorIdx, currentIdx);
                foreach (var t in pages) t.IsSelected = false;
                for (int i = from; i <= to; i++) pages[i].IsSelected = true;
            }
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                thumb.IsSelected = !thumb.IsSelected;
                if (pages != null) _lastClickedIndex = pages.IndexOf(thumb);
            }
            // Plain click: defer to MouseUp so an already-selected item can be dragged
        }
    }

    private void SourceThumb_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragSource == null || _isDragging) return;
        if (sender is not UIElement element) return;

        Point pos = e.GetPosition(null);
        if (Math.Abs(pos.X - _dragStartPos.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - _dragStartPos.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        _isDragging = true;

        List<PageDragItem> pagesToDrag = [];
        if (DataContext is ComposerViewModel vm && vm.ActiveDocument != null)
        {
            var pages = vm.ActiveDocument.Pages;

            if (_dragSource.IsSelected)
            {
                pagesToDrag = pages
                    .Where(p => p.IsSelected)
                    .OrderBy(p => p.PageIndex)
                    .Select(p => new PageDragItem { FilePath = p.FilePath, PageIndex = p.PageIndex })
                    .ToList();
            }
            else
            {
                foreach (var t in pages) t.IsSelected = false;
                _dragSource.IsSelected = true;
                _lastClickedIndex = pages.IndexOf(_dragSource);
                pagesToDrag = [new PageDragItem { FilePath = _dragSource.FilePath, PageIndex = _dragSource.PageIndex }];
            }
        }

        if (pagesToDrag.Count == 0)
            pagesToDrag = [new PageDragItem { FilePath = _dragSource.FilePath, PageIndex = _dragSource.PageIndex }];

        DragDrop.DoDragDrop(element, new DataObject("SlackPdfPages", pagesToDrag), DragDropEffects.Copy);
        _dragSource = null;
        _isDragging = false;
    }

    private void SourceThumb_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragSource == null) return;

        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) &&
            !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            if (DataContext is ComposerViewModel vm && vm.ActiveDocument != null)
            {
                var pages = vm.ActiveDocument.Pages;
                // Re-click on the only selected item → deselect (toggle off)
                bool wasSelected = _dragSource.IsSelected;
                foreach (var t in pages) t.IsSelected = false;
                if (!wasSelected)
                {
                    _dragSource.IsSelected = true;
                    _lastClickedIndex = pages.IndexOf(_dragSource);
                }
            }
        }
        _dragSource = null;
    }

    // ─── Toolbar confirmation dialogs ──────────────────────────────────────

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        string msg = Application.Current.TryFindResource("Composer.ClearConfirm") as string
            ?? "Clear all pages from the assembly?";
        if (MessageBox.Show(msg, "SlackPDF", MessageBoxButton.OKCancel, MessageBoxImage.Question)
            == MessageBoxResult.OK && DataContext is ComposerViewModel vm)
            vm.ClearAllCommand.Execute(null);
    }

    private void AutoOrder_Click(object sender, RoutedEventArgs e)
    {
        string msg = Application.Current.TryFindResource("Composer.AutoOrderConfirm") as string
            ?? "Sort all pages by document label and page number?";
        if (MessageBox.Show(msg, "SlackPDF", MessageBoxButton.OKCancel, MessageBoxImage.Question)
            == MessageBoxResult.OK && DataContext is ComposerViewModel vm)
            vm.AutoOrderCommand.Execute(null);
    }

    // ─── Source panel: Explorer drop (adds new document) ───────────────────

    private void SourcePanel_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            e.Effects = files.Any(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                ? DragDropEffects.Copy : DragDropEffects.None;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private async void SourcePanel_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files) return;
        if (DataContext is not ComposerViewModel vm) return;

        vm.IsLoadingDocuments = true;
        try
        {
            ComposerDocument? firstAdded = null;
            foreach (var f in files.Where(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)))
            {
                var doc = await vm.AddDocumentFromFileAsync(f);
                firstAdded ??= doc;
            }
            if (firstAdded != null)
                vm.ActiveDocument = firstAdded;
        }
        finally
        {
            vm.IsLoadingDocuments = false;
        }
    }

    // ─── Assembly reorder drag ──────────────────────────────────────────────

    private void AssemblyThumb_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ComposerPage page)
        {
            _assemblyDragStartPos = e.GetPosition(null);
            _assemblyDragSource = page;
            e.Handled = true;
        }
    }

    private void AssemblyThumb_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _assemblyDragSource == null) return;
        if (sender is not UIElement element) return;

        Point pos = e.GetPosition(null);
        if (Math.Abs(pos.X - _assemblyDragStartPos.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - _assemblyDragStartPos.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        var page = _assemblyDragSource;
        _assemblyDragSource = null;
        DragDrop.DoDragDrop(element, new DataObject("SlackPdfReorder", page), DragDropEffects.Move);
    }

    // ─── Assembly drop zone ─────────────────────────────────────────────────

    private void Assembly_DragOver(object sender, DragEventArgs e)
    {
        bool hasPages = e.Data.GetDataPresent("SlackPdfPages");
        bool hasReorder = e.Data.GetDataPresent("SlackPdfReorder");

        if (!hasPages && !hasReorder)
        {
            e.Effects = DragDropEffects.None;
            HideInsertAdorner();
            e.Handled = true;
            return;
        }

        e.Effects = hasReorder ? DragDropEffects.Move : DragDropEffects.Copy;

        var panel = GetAssemblyWrapPanel();
        if (panel != null)
        {
            Point pt = e.GetPosition(panel);
            _insertionIndex = FindInsertionIndex(panel, pt, out double lx, out double ly, out double lh);
            EnsureInsertAdorner(panel);
            _insertAdorner?.SetLine(lx, ly, lh);
        }

        e.Handled = true;
    }

    private void Assembly_DragLeave(object sender, DragEventArgs e)
    {
        // Only hide when cursor actually leaves the ScrollViewer bounds (not on child transitions)
        if (sender is FrameworkElement fe)
        {
            Point pos = e.GetPosition(fe);
            if (pos.X < 0 || pos.Y < 0 || pos.X > fe.ActualWidth || pos.Y > fe.ActualHeight)
                HideInsertAdorner();
        }
    }

    private void Assembly_Drop(object sender, DragEventArgs e)
    {
        HideInsertAdorner();
        if (DataContext is not ComposerViewModel vm) return;

        int idx = _insertionIndex >= 0 ? _insertionIndex : vm.ComposedPages.Count;

        if (e.Data.GetData("SlackPdfReorder") is ComposerPage reorderPage)
        {
            int fromIdx = vm.ComposedPages.IndexOf(reorderPage);
            if (fromIdx >= 0)
            {
                int toIdx = idx > fromIdx ? idx - 1 : idx;
                toIdx = Math.Max(0, Math.Min(vm.ComposedPages.Count - 1, toIdx));
                if (fromIdx != toIdx)
                    vm.ComposedPages.Move(fromIdx, toIdx);
            }
        }
        else if (e.Data.GetData("SlackPdfPages") is List<PageDragItem> pages)
        {
            foreach (var p in pages)
                vm.InsertPage(p.FilePath, p.PageIndex, idx++);
        }

        _insertionIndex = -1;
    }

    // ─── Insertion indicator adorner ────────────────────────────────────────

    private void EnsureInsertAdorner(WrapPanel panel)
    {
        if (_insertAdorner != null) return;
        var layer = AdornerLayer.GetAdornerLayer(panel);
        if (layer == null) return;
        _insertAdorner = new InsertAdorner(panel);
        layer.Add(_insertAdorner);
    }

    private void HideInsertAdorner()
    {
        if (_insertAdorner == null) return;
        var layer = AdornerLayer.GetAdornerLayer(_insertAdorner.AdornedElement);
        layer?.Remove(_insertAdorner);
        _insertAdorner = null;
    }

    /// <summary>
    /// Finds the insertion gap in the assembly WrapPanel closest to <paramref name="pt"/>.
    /// Returns the target index and the (x, y, height) of where to draw the indicator line.
    /// </summary>
    private static int FindInsertionIndex(WrapPanel panel, Point pt,
        out double lineX, out double lineY, out double lineH)
    {
        lineX = 8; lineY = 8; lineH = 120;
        int n = panel.Children.Count;
        if (n == 0) return 0;

        FrameworkElement? lastOnRow = null;
        int lastOnRowIdx = -1;

        for (int i = 0; i < n; i++)
        {
            if (panel.Children[i] is not FrameworkElement child) continue;
            var origin = child.TranslatePoint(new Point(0, 0), panel);
            double cw = child.ActualWidth, ch = child.ActualHeight;

            bool onRow = pt.Y >= origin.Y - 4 && pt.Y <= origin.Y + ch + 4;
            if (!onRow) continue;

            lastOnRow = child;
            lastOnRowIdx = i;

            if (pt.X <= origin.X + cw / 2)
            {
                // Insert before item i
                lineX = origin.X;
                lineY = origin.Y + 4;
                lineH = ch - 8;
                return i;
            }
        }

        if (lastOnRow != null)
        {
            // Cursor is past the midpoint of the last item on its row → insert after it
            var origin = lastOnRow.TranslatePoint(new Point(0, 0), panel);
            lineX = origin.X + lastOnRow.ActualWidth;
            lineY = origin.Y + 4;
            lineH = lastOnRow.ActualHeight - 8;
            return lastOnRowIdx + 1;
        }

        // Below all items → append at end
        if (panel.Children[n - 1] is FrameworkElement last)
        {
            var origin = last.TranslatePoint(new Point(0, 0), panel);
            lineX = origin.X + last.ActualWidth;
            lineY = origin.Y + 4;
            lineH = last.ActualHeight - 8;
        }
        return n;
    }

    private WrapPanel? GetAssemblyWrapPanel() => FindVisualChild<WrapPanel>(AssemblyPanel);

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found) return found;
            var nested = FindVisualChild<T>(child);
            if (nested != null) return nested;
        }
        return null;
    }

    // ─── Insert indicator adorner class ─────────────────────────────────────

    private sealed class InsertAdorner : Adorner
    {
        private double _x, _y, _h;

        private static readonly Pen LinePen;
        private static readonly SolidColorBrush DotBrush;

        static InsertAdorner()
        {
            DotBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243));
            DotBrush.Freeze();
            LinePen = new Pen(DotBrush, 2.5) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            LinePen.Freeze();
        }

        internal InsertAdorner(UIElement element) : base(element) { IsHitTestVisible = false; }

        internal void SetLine(double x, double y, double h)
        {
            _x = x; _y = y; _h = h;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawLine(LinePen, new Point(_x, _y - 4), new Point(_x, _y + _h + 4));
            dc.DrawEllipse(DotBrush, null, new Point(_x, _y - 4), 4, 4);
            dc.DrawEllipse(DotBrush, null, new Point(_x, _y + _h + 4), 4, 4);
        }
    }
}

public class PageDragItem
{
    public string FilePath { get; set; } = string.Empty;
    public int    PageIndex { get; set; }
}
