namespace SlackPDF.Core.Models;

public record PdfFileInfo(
    string FilePath,
    string FileName,
    int PageCount,
    long FileSizeBytes,
    PageSelection Selection)
{
    public string Label { get; init; } = string.Empty;
    public System.Windows.Media.Color Color { get; init; }

    public string FileSizeFormatted => FileSizeBytes switch
    {
        < 1024 => $"{FileSizeBytes} B",
        < 1024 * 1024 => $"{FileSizeBytes / 1024.0:F1} KB",
        _ => $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB"
    };
}
