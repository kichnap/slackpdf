using System.Windows;
using System.Windows.Controls;
using SlackPDF.ViewModels;

namespace SlackPDF.Views;

public partial class SplitView : UserControl
{
    public SplitView() => InitializeComponent();

    private void InputFile_DragOver(object sender, DragEventArgs e)
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

    private void InputFile_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files) return;
        if (DataContext is not SplitViewModel vm) return;
        var pdf = files.FirstOrDefault(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
        if (pdf != null) vm.SetInputFile(pdf);
        e.Handled = true;
    }
}
