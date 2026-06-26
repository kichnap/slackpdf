using SlackPDF.Core.Models;

namespace SlackPDF.Core.Engines;

public interface IPdfEngine
{
    string Name { get; }

    Task<OperationResult> MergeAsync(
        IEnumerable<(string FilePath, PageSelection Pages)> inputs,
        string outputPath,
        MergeOptions options,
        IProgress<int> progress,
        CancellationToken ct);

    Task<OperationResult> SplitAsync(
        string inputPath,
        string outputDir,
        SplitOptions options,
        IProgress<int> progress,
        CancellationToken ct);

    Task<OperationResult> MixAsync(
        IEnumerable<(string FilePath, bool Reverse)> inputs,
        string outputPath,
        IProgress<int> progress,
        CancellationToken ct);

    Task<OperationResult> RotateAsync(
        string inputPath,
        string outputPath,
        int angleDegrees,
        PageSelection pages,
        IProgress<int> progress,
        CancellationToken ct);

    Task<OperationResult> ExtractAsync(
        string inputPath,
        string outputPath,
        PageSelection pages,
        ExtractMode mode,
        IProgress<int> progress,
        CancellationToken ct);

    Task<OperationResult> InsertAsync(
        string baseFilePath,
        string insertFilePath,
        PageSelection insertPages,
        InsertOptions options,
        string outputPath,
        IProgress<int> progress,
        CancellationToken ct);

    Task<OperationResult> ComposeAsync(
        IEnumerable<(string FilePath, int PageIndex)> pageSequence,
        string outputPath,
        IProgress<int> progress,
        CancellationToken ct);
}
