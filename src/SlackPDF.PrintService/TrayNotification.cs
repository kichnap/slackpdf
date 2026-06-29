using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace SlackPDF.PrintService;

// Sends Windows 10 Toast Notifications via the WinRT API.
// Unlike NotifyIcon balloon tips, these work from a SYSTEM service in Session 0
// because Windows routes them to the active user's notification center.
public class TrayNotification : IDisposable
{
    // App User Model ID — must match what is registered in the installer.
    private const string AppId = "SlackPDF.VirtualPrinter";

    private static readonly string LogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "SlackPDF", "service.log");

    private static void Log(string msg)
    {
        try { File.AppendAllText(LogFile, $"{DateTime.Now:HH:mm:ss.fff} {msg}{Environment.NewLine}"); }
        catch { }
    }

    public void ShowProcessing(string jobName) =>
        Show("SlackPDF", $"Конвертирую: {jobName}", silent: true);

    public void ShowError(string message) =>
        Show("SlackPDF — ошибка", message, silent: false);

    public void ShowDone(string savedPath) =>
        Show("SlackPDF", $"Сохранено: {Path.GetFileName(savedPath)}", silent: true, filePath: savedPath);

    public void Hide() { /* Toasts auto-dismiss; nothing to do. */ }

    private static void Show(string title, string body, bool silent, string? filePath = null)
    {
        try
        {
            // Build launch argument so clicking the toast opens the saved file/folder
            var launch = filePath != null
                ? $"file:///{filePath.Replace('\\', '/')}"
                : string.Empty;

            var silentAttr = silent ? "<audio silent='true'/>" : string.Empty;
            var xml = $"""
                <toast launch="{System.Security.SecurityElement.Escape(launch)}">
                  <visual>
                    <binding template="ToastGeneric">
                      <text>{System.Security.SecurityElement.Escape(title)}</text>
                      <text>{System.Security.SecurityElement.Escape(body)}</text>
                    </binding>
                  </visual>
                  {silentAttr}
                </toast>
                """;

            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var toast = new ToastNotification(doc);
            ToastNotificationManager.CreateToastNotifier(AppId).Show(toast);
        }
        catch (Exception ex)
        {
            // Session 0 isolation may prevent showing — log and continue silently
            Log($"Toast failed ({ex.GetType().Name}): {ex.Message}");
        }
    }

    public void Dispose() { }
}
