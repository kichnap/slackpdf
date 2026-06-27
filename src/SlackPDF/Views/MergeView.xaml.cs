using System.Windows;
using System.Windows.Controls;
using SlackPDF.ViewModels;

namespace SlackPDF.Views;

public partial class MergeView : UserControl
{
    public MergeView()
    {
        InitializeComponent();
    }

    private void FileList_DragOver(object sender, DragEventArgs e)
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

    private void FileList_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files) return;
        if (DataContext is not MergeViewModel vm) return;
        foreach (var f in files.Where(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)))
            vm.AddFile(f);
        e.Handled = true;
    }
}
