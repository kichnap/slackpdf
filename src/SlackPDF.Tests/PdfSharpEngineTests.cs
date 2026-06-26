using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SlackPDF.Core.Engines;
using SlackPDF.Core.Models;
using Xunit;

namespace SlackPDF.Tests;

public class PdfSharpEngineTests : IDisposable
{
    private readonly PdfSharpEngine _engine = new();
    private readonly string _tempDir;
    private readonly List<string> _tempFiles = [];

    public PdfSharpEngineTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SlackPDFTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    private string CreateTestPdf(int pages, string? name = null)
    {
        var path = Path.Combine(_tempDir, name ?? $"test_{Guid.NewGuid():N}.pdf");
        using var doc = new PdfDocument();
        for (int i = 0; i < pages; i++)
            doc.AddPage();
        doc.Save(path);
        _tempFiles.Add(path);
        return path;
    }

    private int CountPages(string path)
    {
        using var doc = PdfReader.Open(path, PdfDocumentOpenMode.InformationOnly);
        return doc.PageCount;
    }

    [Fact]
    public async Task Merge_ThreeFilesAllPages_OutputHasCorrectPageCount()
    {
        var f1 = CreateTestPdf(5);
        var f2 = CreateTestPdf(5);
        var f3 = CreateTestPdf(5);
        var output = Path.Combine(_tempDir, "merged.pdf");

        var inputs = new[] { (f1, PageSelection.All), (f2, PageSelection.All), (f3, PageSelection.All) };
        var options = new MergeOptions(BookmarkBehavior.Discard, AcroFormBehavior.Discard, false);
        var result = await _engine.MergeAsync(inputs, output, options, null!, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(15, CountPages(output));
    }

    [Fact]
    public async Task Merge_WithPageSelection_OutputHasCorrectPageCount()
    {
        var f1 = CreateTestPdf(5);
        var f2 = CreateTestPdf(5);
        var output = Path.Combine(_tempDir, "merged_sel.pdf");

        var inputs = new[]
        {
            (f1, PageSelection.Parse("1-3")),
            (f2, PageSelection.Parse("2-4"))
        };
        var options = new MergeOptions(BookmarkBehavior.Discard, AcroFormBehavior.Discard, false);
        var result = await _engine.MergeAsync(inputs, output, options, null!, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(6, CountPages(output));
    }

    [Fact]
    public async Task Split_EveryPage_ProducesOneFilePerPage()
    {
        var input = CreateTestPdf(10);
        var outDir = Path.Combine(_tempDir, "split_every");
        var options = new SplitOptions(SplitMode.EveryPage, null, null, null, null, "page");

        var result = await _engine.SplitAsync(input, outDir, options, null!, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        var files = Directory.GetFiles(outDir, "*.pdf");
        Assert.Equal(10, files.Length);
        foreach (var f in files)
            Assert.Equal(1, CountPages(f));
    }

    [Fact]
    public async Task Split_EveryThreePages_ProducesFourFiles()
    {
        var input = CreateTestPdf(10);
        var outDir = Path.Combine(_tempDir, "split_n3");
        var options = new SplitOptions(SplitMode.EveryNPages, 3, null, null, null, "chunk");

        var result = await _engine.SplitAsync(input, outDir, options, null!, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        var files = Directory.GetFiles(outDir, "*.pdf").OrderBy(f => f).ToList();
        Assert.Equal(4, files.Count);
        Assert.Equal(3, CountPages(files[0]));
        Assert.Equal(3, CountPages(files[1]));
        Assert.Equal(3, CountPages(files[2]));
        Assert.Equal(1, CountPages(files[3]));
    }

    [Fact]
    public async Task Split_AtPages_ProducesCorrectGroups()
    {
        var input = CreateTestPdf(10);
        var outDir = Path.Combine(_tempDir, "split_at");
        var options = new SplitOptions(SplitMode.AtPages, null, [3, 7], null, null, "part");

        var result = await _engine.SplitAsync(input, outDir, options, null!, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        var files = Directory.GetFiles(outDir, "*.pdf").OrderBy(f => f).ToList();
        Assert.Equal(3, files.Count);
        Assert.Equal(2, CountPages(files[0]));
        Assert.Equal(4, CountPages(files[1]));
        Assert.Equal(4, CountPages(files[2]));
    }

    [Fact]
    public async Task Rotate_AllPages_CompletesSuccessfully()
    {
        var input = CreateTestPdf(5);
        var output = Path.Combine(_tempDir, "rotated.pdf");

        var result = await _engine.RotateAsync(input, output, 90, PageSelection.All, null!, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(5, CountPages(output));
    }

    [Fact]
    public async Task Extract_SpecificPages_OutputHasCorrectPageCount()
    {
        var input = CreateTestPdf(10);
        var output = Path.Combine(_tempDir, "extracted.pdf");

        var result = await _engine.ExtractAsync(input, output,
            PageSelection.Parse("1,3,5"), ExtractMode.SingleFile, null!, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(3, CountPages(output));
    }

    [Fact]
    public async Task Mix_TwoFilesStraight_InterleavesPagesCorrectly()
    {
        var a = CreateTestPdf(3, "A.pdf");
        var b = CreateTestPdf(3, "B.pdf");
        var output = Path.Combine(_tempDir, "mixed.pdf");

        var inputs = new[] { (a, false), (b, false) };
        var result = await _engine.MixAsync(inputs, output, null!, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(6, CountPages(output));
    }

    [Fact]
    public async Task Mix_TwoFilesReverse_OutputHasCorrectPageCount()
    {
        var a = CreateTestPdf(3, "A2.pdf");
        var b = CreateTestPdf(3, "B2.pdf");
        var output = Path.Combine(_tempDir, "mixed_rev.pdf");

        var inputs = new[] { (a, false), (b, true) };
        var result = await _engine.MixAsync(inputs, output, null!, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(6, CountPages(output));
    }

    [Fact]
    public async Task Insert_EveryTwoPages_OutputHasCorrectPageCount()
    {
        var baseDoc   = CreateTestPdf(6);
        var insertDoc = CreateTestPdf(2);
        var output    = Path.Combine(_tempDir, "inserted.pdf");

        var options = new InsertOptions(InsertMode.EveryNPages, 2);
        var result  = await _engine.InsertAsync(baseDoc, insertDoc, PageSelection.All,
            options, output, null!, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(6 + 3 * 2, CountPages(output));
    }

    [Fact]
    public async Task Compose_MixThreeDocs_OutputHasCorrectPageCount()
    {
        var a = CreateTestPdf(5, "cA.pdf");
        var b = CreateTestPdf(5, "cB.pdf");

        var sequence = new[]
        {
            (a, 0), (a, 1),
            (b, 2),
            (a, 4)
        };
        var output = Path.Combine(_tempDir, "composed.pdf");
        var result = await _engine.ComposeAsync(sequence, output, null!, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(4, CountPages(output));
    }

    [Fact]
    public async Task Compose_DuplicatePage_OutputHasBothCopies()
    {
        var a = CreateTestPdf(3, "dup.pdf");
        var sequence = new[] { (a, 0), (a, 0) };
        var output = Path.Combine(_tempDir, "dup_out.pdf");

        var result = await _engine.ComposeAsync(sequence, output, null!, CancellationToken.None);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(2, CountPages(output));
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
