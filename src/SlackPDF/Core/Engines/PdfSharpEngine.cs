using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SlackPDF.Core.Models;
using System.Diagnostics;

namespace SlackPDF.Core.Engines;

public class PdfSharpEngine : IPdfEngine
{
    public string Name => "PDFsharp";

    public async Task<OperationResult> MergeAsync(
        IEnumerable<(string FilePath, PageSelection Pages)> inputs,
        string outputPath,
        MergeOptions options,
        IProgress<int> progress,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await Task.Run(() =>
            {
                using var output = new PdfDocument();
                var inputList = inputs.ToList();
                int done = 0;

                foreach (var (filePath, pages) in inputList)
                {
                    ct.ThrowIfCancellationRequested();
                    using var src = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);

                    if (options.Bookmarks == BookmarkBehavior.OneEntryPerFile)
                    {
                        var outline = output.Outlines.Add(
                            Path.GetFileNameWithoutExtension(filePath),
                            output.Pages.Count > 0 ? output.Pages[output.Pages.Count - 1] : null);
                    }

                    var pageNumbers = pages.Resolve(src.PageCount).ToList();
                    foreach (int pn in pageNumbers)
                    {
                        ct.ThrowIfCancellationRequested();
                        output.AddPage(src.Pages[pn - 1]);
                    }

                    done++;
                    progress?.Report(done * 100 / inputList.Count);
                }

                EnsureDirectory(outputPath);
                output.Save(outputPath);
            }, ct);

            return OperationResult.Ok(outputPath, sw.Elapsed);
        }
        catch (OperationCanceledException)
        {
            return OperationResult.Fail("Operation cancelled.", sw.Elapsed);
        }
        catch (Exception ex)
        {
            return OperationResult.Fail(ex.Message, sw.Elapsed);
        }
    }

    public async Task<OperationResult> SplitAsync(
        string inputPath,
        string outputDir,
        SplitOptions options,
        IProgress<int> progress,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await Task.Run(() =>
            {
                using var src = PdfReader.Open(inputPath, PdfDocumentOpenMode.Import);
                Directory.CreateDirectory(outputDir);
                string prefix = string.IsNullOrWhiteSpace(options.FileNamePrefix)
                    ? Path.GetFileNameWithoutExtension(inputPath)
                    : options.FileNamePrefix;

                var groups = BuildSplitGroups(src.PageCount, options);
                for (int gi = 0; gi < groups.Count; gi++)
                {
                    ct.ThrowIfCancellationRequested();
                    var pageNums = groups[gi];
                    using var doc = new PdfDocument();
                    foreach (int pn in pageNums)
                        doc.AddPage(src.Pages[pn - 1]);

                    string outFile = Path.Combine(outputDir, $"{prefix}_{gi + 1:D4}.pdf");
                    doc.Save(outFile);
                    progress?.Report((gi + 1) * 100 / groups.Count);
                }
            }, ct);

            return OperationResult.Ok(outputDir, sw.Elapsed);
        }
        catch (OperationCanceledException)
        {
            return OperationResult.Fail("Operation cancelled.", sw.Elapsed);
        }
        catch (Exception ex)
        {
            return OperationResult.Fail(ex.Message, sw.Elapsed);
        }
    }

    public async Task<OperationResult> MixAsync(
        IEnumerable<(string FilePath, bool Reverse)> inputs,
        string outputPath,
        IProgress<int> progress,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await Task.Run(() =>
            {
                var docs = inputs.Select(i =>
                {
                    var doc = PdfReader.Open(i.FilePath, PdfDocumentOpenMode.Import);
                    return (Doc: doc, i.Reverse);
                }).ToList();

                try
                {
                    using var output = new PdfDocument();
                    int maxPages = docs.Max(d => d.Doc.PageCount);

                    for (int i = 0; i < maxPages; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        foreach (var (doc, reverse) in docs)
                        {
                            int idx = reverse ? doc.PageCount - 1 - i : i;
                            if (idx >= 0 && idx < doc.PageCount)
                                output.AddPage(doc.Pages[idx]);
                        }
                        progress?.Report((i + 1) * 100 / maxPages);
                    }

                    EnsureDirectory(outputPath);
                    output.Save(outputPath);
                }
                finally
                {
                    foreach (var (doc, _) in docs)
                        doc.Dispose();
                }
            }, ct);

            return OperationResult.Ok(outputPath, sw.Elapsed);
        }
        catch (OperationCanceledException)
        {
            return OperationResult.Fail("Operation cancelled.", sw.Elapsed);
        }
        catch (Exception ex)
        {
            return OperationResult.Fail(ex.Message, sw.Elapsed);
        }
    }

    public async Task<OperationResult> RotateAsync(
        string inputPath,
        string outputPath,
        int angleDegrees,
        PageSelection pages,
        IProgress<int> progress,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await Task.Run(() =>
            {
                using var doc = PdfReader.Open(inputPath, PdfDocumentOpenMode.Modify);
                for (int i = 0; i < doc.PageCount; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    if (pages.Contains(i + 1))
                    {
                        var page = doc.Pages[i];
                        page.Rotate = (page.Rotate + angleDegrees) % 360;
                    }
                    progress?.Report((i + 1) * 100 / doc.PageCount);
                }

                EnsureDirectory(outputPath);
                doc.Save(outputPath);
            }, ct);

            return OperationResult.Ok(outputPath, sw.Elapsed);
        }
        catch (OperationCanceledException)
        {
            return OperationResult.Fail("Operation cancelled.", sw.Elapsed);
        }
        catch (Exception ex)
        {
            return OperationResult.Fail(ex.Message, sw.Elapsed);
        }
    }

    public async Task<OperationResult> ExtractAsync(
        string inputPath,
        string outputPath,
        PageSelection pages,
        ExtractMode mode,
        IProgress<int> progress,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await Task.Run(() =>
            {
                using var src = PdfReader.Open(inputPath, PdfDocumentOpenMode.Import);
                var pageNums = pages.Resolve(src.PageCount).ToList();

                switch (mode)
                {
                    case ExtractMode.SingleFile:
                    {
                        using var doc = new PdfDocument();
                        for (int i = 0; i < pageNums.Count; i++)
                        {
                            ct.ThrowIfCancellationRequested();
                            doc.AddPage(src.Pages[pageNums[i] - 1]);
                            progress?.Report((i + 1) * 100 / pageNums.Count);
                        }
                        EnsureDirectory(outputPath);
                        doc.Save(outputPath);
                        break;
                    }
                    case ExtractMode.OneFilePerPage:
                    {
                        string dir = outputPath;
                        Directory.CreateDirectory(dir);
                        string baseName = Path.GetFileNameWithoutExtension(inputPath);
                        for (int i = 0; i < pageNums.Count; i++)
                        {
                            ct.ThrowIfCancellationRequested();
                            using var doc = new PdfDocument();
                            doc.AddPage(src.Pages[pageNums[i] - 1]);
                            doc.Save(Path.Combine(dir, $"{baseName}_p{pageNums[i]:D4}.pdf"));
                            progress?.Report((i + 1) * 100 / pageNums.Count);
                        }
                        break;
                    }
                    case ExtractMode.OneFilePerRange:
                    {
                        string dir = outputPath;
                        Directory.CreateDirectory(dir);
                        string baseName = Path.GetFileNameWithoutExtension(inputPath);
                        var ranges = pages.SelectAll
                            ? new List<PageRange> { new(1, src.PageCount) }
                            : pages.Ranges.ToList();

                        for (int ri = 0; ri < ranges.Count; ri++)
                        {
                            ct.ThrowIfCancellationRequested();
                            var range = ranges[ri];
                            var rangePages = pages.Resolve(src.PageCount)
                                .Where(p => p >= range.From && (range.To == null || p <= range.To))
                                .ToList();
                            using var doc = new PdfDocument();
                            foreach (int pn in rangePages)
                                doc.AddPage(src.Pages[pn - 1]);
                            doc.Save(Path.Combine(dir, $"{baseName}_range{ri + 1:D2}.pdf"));
                            progress?.Report((ri + 1) * 100 / ranges.Count);
                        }
                        break;
                    }
                }
            }, ct);

            return OperationResult.Ok(outputPath, sw.Elapsed);
        }
        catch (OperationCanceledException)
        {
            return OperationResult.Fail("Operation cancelled.", sw.Elapsed);
        }
        catch (Exception ex)
        {
            return OperationResult.Fail(ex.Message, sw.Elapsed);
        }
    }

    public async Task<OperationResult> InsertAsync(
        string baseFilePath,
        string insertFilePath,
        PageSelection insertPages,
        InsertOptions options,
        string outputPath,
        IProgress<int> progress,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await Task.Run(() =>
            {
                using var baseDoc = PdfReader.Open(baseFilePath, PdfDocumentOpenMode.Import);
                using var insertDoc = PdfReader.Open(insertFilePath, PdfDocumentOpenMode.Import);
                var insertPageNums = insertPages.Resolve(insertDoc.PageCount).ToList();

                using var output = new PdfDocument();
                var basePages = Enumerable.Range(0, baseDoc.PageCount).ToList();

                switch (options.Mode)
                {
                    case InsertMode.AtPosition:
                    {
                        int insertAt = Math.Max(0, Math.Min(options.Position - 1, basePages.Count));
                        for (int i = 0; i < basePages.Count; i++)
                        {
                            ct.ThrowIfCancellationRequested();
                            if (i == insertAt)
                                foreach (int pn in insertPageNums)
                                    output.AddPage(insertDoc.Pages[pn - 1]);
                            output.AddPage(baseDoc.Pages[i]);
                        }
                        if (insertAt >= basePages.Count)
                            foreach (int pn in insertPageNums)
                                output.AddPage(insertDoc.Pages[pn - 1]);
                        break;
                    }
                    case InsertMode.EveryNPages:
                    {
                        int n = Math.Max(1, options.Position);
                        for (int i = 0; i < basePages.Count; i++)
                        {
                            ct.ThrowIfCancellationRequested();
                            output.AddPage(baseDoc.Pages[i]);
                            if ((i + 1) % n == 0)
                                foreach (int pn in insertPageNums)
                                    output.AddPage(insertDoc.Pages[pn - 1]);
                        }
                        break;
                    }
                    case InsertMode.AfterEveryPage:
                    {
                        for (int i = 0; i < basePages.Count; i++)
                        {
                            ct.ThrowIfCancellationRequested();
                            output.AddPage(baseDoc.Pages[i]);
                            foreach (int pn in insertPageNums)
                                output.AddPage(insertDoc.Pages[pn - 1]);
                        }
                        break;
                    }
                }

                EnsureDirectory(outputPath);
                output.Save(outputPath);
                progress?.Report(100);
            }, ct);

            return OperationResult.Ok(outputPath, sw.Elapsed);
        }
        catch (OperationCanceledException)
        {
            return OperationResult.Fail("Operation cancelled.", sw.Elapsed);
        }
        catch (Exception ex)
        {
            return OperationResult.Fail(ex.Message, sw.Elapsed);
        }
    }

    public async Task<OperationResult> ComposeAsync(
        IEnumerable<(string FilePath, int PageIndex)> pageSequence,
        string outputPath,
        IProgress<int> progress,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await Task.Run(() =>
            {
                var sequence = pageSequence.ToList();
                var openDocs = new Dictionary<string, PdfDocument>();

                try
                {
                    using var output = new PdfDocument();
                    for (int i = 0; i < sequence.Count; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var (filePath, pageIndex) = sequence[i];

                        if (!openDocs.TryGetValue(filePath, out var src))
                        {
                            src = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
                            openDocs[filePath] = src;
                        }

                        output.AddPage(src.Pages[pageIndex]);
                        progress?.Report((i + 1) * 100 / sequence.Count);
                    }

                    EnsureDirectory(outputPath);
                    output.Save(outputPath);
                }
                finally
                {
                    foreach (var d in openDocs.Values)
                        d.Dispose();
                }
            }, ct);

            return OperationResult.Ok(outputPath, sw.Elapsed);
        }
        catch (OperationCanceledException)
        {
            return OperationResult.Fail("Operation cancelled.", sw.Elapsed);
        }
        catch (Exception ex)
        {
            return OperationResult.Fail(ex.Message, sw.Elapsed);
        }
    }

    private static List<List<int>> BuildSplitGroups(int totalPages, SplitOptions options)
    {
        var groups = new List<List<int>>();
        switch (options.Mode)
        {
            case SplitMode.EveryPage:
                for (int i = 1; i <= totalPages; i++)
                    groups.Add([i]);
                break;

            case SplitMode.EveryNPages:
            {
                int n = Math.Max(1, options.NPages ?? 1);
                for (int start = 1; start <= totalPages; start += n)
                {
                    var group = new List<int>();
                    for (int p = start; p < start + n && p <= totalPages; p++)
                        group.Add(p);
                    groups.Add(group);
                }
                break;
            }

            case SplitMode.AtPages:
            {
                var cuts = (options.AtPages ?? []).OrderBy(x => x).Distinct().ToList();
                int prev = 1;
                foreach (int cut in cuts)
                {
                    var group = new List<int>();
                    for (int p = prev; p < cut && p <= totalPages; p++)
                        group.Add(p);
                    if (group.Count > 0) groups.Add(group);
                    prev = cut;
                }
                var last = new List<int>();
                for (int p = prev; p <= totalPages; p++)
                    last.Add(p);
                if (last.Count > 0) groups.Add(last);
                break;
            }

            case SplitMode.EvenOdd:
            {
                var odd  = Enumerable.Range(1, totalPages).Where(p => p % 2 != 0).ToList();
                var even = Enumerable.Range(1, totalPages).Where(p => p % 2 == 0).ToList();
                if (odd.Count  > 0) groups.Add(odd);
                if (even.Count > 0) groups.Add(even);
                break;
            }

            default:
                for (int i = 1; i <= totalPages; i++)
                    groups.Add([i]);
                break;
        }
        return groups;
    }

    private static void EnsureDirectory(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }
}
