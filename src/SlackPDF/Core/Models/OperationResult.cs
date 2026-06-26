namespace SlackPDF.Core.Models;

public record OperationResult(
    bool Success,
    string? OutputPath,
    string? ErrorMessage,
    TimeSpan Elapsed,
    long OutputFileSizeBytes)
{
    public static OperationResult Ok(string outputPath, TimeSpan elapsed)
    {
        var size = File.Exists(outputPath) ? new FileInfo(outputPath).Length : 0;
        return new OperationResult(true, outputPath, null, elapsed, size);
    }

    public static OperationResult Fail(string error, TimeSpan elapsed)
        => new(false, null, error, elapsed, 0);
}
