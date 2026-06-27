using SlackPDF.Services;
using SlackPDF.ViewModels;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SlackPDF;

public partial class MainWindow : Window
{
    [DllImport("dwmapi.dll", PreserveSig = false)]
    private static extern void DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public MainWindow()
    {
        InitializeComponent();

        // SourceInitialized fires after the HWND is created but before the window is shown —
        // this ensures the title bar is dark on first paint with no flash
        SourceInitialized += (_, _) =>
        {
            bool isDark = SettingsService.Load().Theme == "Dark";
            ApplyDarkTitleBar(isDark);
        };

        SettingsViewModel.ThemeChanged += (_, isDark) => ApplyDarkTitleBar(isDark);
    }

    private void ApplyDarkTitleBar(bool dark)
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int value = dark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
        }
        catch { }
    }
}
