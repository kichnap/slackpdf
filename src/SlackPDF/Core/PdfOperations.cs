using SlackPDF.Core.Engines;
using SlackPDF.Core.Models;

namespace SlackPDF.Core;

public class PdfOperations
{
    private IPdfEngine _engine;

    public PdfOperations(IPdfEngine engine)
    {
        _engine = engine;
    }

    public IPdfEngine Engine => _engine;

    public void SetEngine(IPdfEngine engine) => _engine = engine;

    public Task<OperationResult> MergeAsync(
        IEnumerable<(string FilePath, PageSelection Pages)> inputs,
        string outputPath, MergeOptions options,
        IProgress<int> progress, CancellationToken ct)
        => _engine.MergeAsync(inputs, outputPath, options, progress, ct);

    public Task<OperationResult> SplitAsync(
        string inputPath, string outputDir, SplitOptions options,
        IProgress<int> progress, CancellationToken ct)
        => _engine.SplitAsync(inputPath, outputDir, options, progress, ct);

    public Task<OperationResult> MixAsync(
        IEnumerable<(string FilePath, bool Reverse)> inputs,
        string outputPath, IProgress<int> progress, CancellationToken ct)
        => _engine.MixAsync(inputs, outputPath, progress, ct);

    public Task<OperationResult> RotateAsync(
        string inputPath, string outputPath, int angleDegrees, PageSelection pages,
        IProgress<int> progress, CancellationToken ct)
        => _engine.RotateAsync(inputPath, outputPath, angleDegrees, pages, progress, ct);

    public Task<OperationResult> ExtractAsync(
        string inputPath, string outputPath, PageSelection pages, ExtractMode mode,
        IProgress<int> progress, CancellationToken ct)
        => _engine.ExtractAsync(inputPath, outputPath, pages, mode, progress, ct);

    public Task<OperationResult> InsertAsync(
        string baseFilePath, string insertFilePath, PageSelection insertPages,
        InsertOptions options, string outputPath,
        IProgress<int> progress, CancellationToken ct)
        => _engine.InsertAsync(baseFilePath, insertFilePath, insertPages, options, outputPath, progress, ct);

    public Task<OperationResult> ComposeAsync(
        IEnumerable<(string FilePath, int PageIndex)> pageSequence,
        string outputPath, IProgress<int> progress, CancellationToken ct)
        => _engine.ComposeAsync(pageSequence, outputPath, progress, ct);
}
