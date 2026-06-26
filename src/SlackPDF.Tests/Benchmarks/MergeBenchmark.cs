using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using PdfSharp.Pdf;
using SlackPDF.Core.Engines;
using SlackPDF.Core.Models;

namespace SlackPDF.Tests.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class MergeBenchmark : IDisposable
{
    private readonly string _tempDir;
    private readonly string _file1;
    private readonly string _file2;
    private readonly string _file3;
    private readonly PdfSharpEngine _pdfSharp = new();
    private readonly ITextEngine    _iText    = new();
    private readonly MergeOptions   _options  = new(BookmarkBehavior.Discard, AcroFormBehavior.Discard, false);

    public MergeBenchmark()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SlackPDFBench_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        _file1 = CreateTestPdf(10);
        _file2 = CreateTestPdf(10);
        _file3 = CreateTestPdf(10);
    }

    private string CreateTestPdf(int pages)
    {
        var path = Path.Combine(_tempDir, $"bench_{Guid.NewGuid():N}.pdf");
        using var doc = new PdfDocument();
        for (int i = 0; i < pages; i++) doc.AddPage();
        doc.Save(path);
        return path;
    }

    [Benchmark]
    public async Task PdfSharpMerge()
    {
        var output = Path.Combine(_tempDir, $"out_{Guid.NewGuid():N}.pdf");
        var inputs = new[] { (_file1, PageSelection.All), (_file2, PageSelection.All), (_file3, PageSelection.All) };
        await _pdfSharp.MergeAsync(inputs, output, _options, null!, CancellationToken.None);
    }

    [Benchmark]
    public async Task ITextMerge()
    {
        var output = Path.Combine(_tempDir, $"out_{Guid.NewGuid():N}.pdf");
        var inputs = new[] { (_file1, PageSelection.All), (_file2, PageSelection.All), (_file3, PageSelection.All) };
        await _iText.MergeAsync(inputs, output, _options, null!, CancellationToken.None);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
