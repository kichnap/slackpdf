using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using SlackPDF.Core.Models;
using System.Diagnostics;
using ModelPageRange = SlackPDF.Core.Models.PageRange;

namespace SlackPDF.Core.Engines;

public class ITextEngine : IPdfEngine
{
    public string Name => "iText";

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
                EnsureDirectory(outputPath);
                using var writer = new PdfWriter(outputPath);
                var outputDoc = new PdfDocument(writer);
                try
                {
                    var merger = new PdfMerger(outputDoc);
                    var inputList = inputs.ToList();
                    for (int idx = 0; idx < inputList.Count; idx++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var (filePath, pages) = inputList[idx];
                        var reader = new PdfReader(filePath);
                        var srcDoc = new PdfDocument(reader);
                        try
                        {
                            var pageNums = pages.Resolve(srcDoc.GetNumberOfPages()).ToList();
                            merger.Merge(srcDoc, pageNums);
                        }
                        finally
                        {
                            srcDoc.Close();
                            reader.Close();
                        }
                        progress?.Report((idx + 1) * 100 / inputList.Count);
                    }
                }
                finally
                {
                    outputDoc.Close();
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
                Directory.CreateDirectory(outputDir);
                var reader = new PdfReader(inputPath);
                var srcDoc = new PdfDocument(reader);
                try
                {
                    int totalPages = srcDoc.GetNumberOfPages();
                    string prefix = string.IsNullOrWhiteSpace(options.FileNamePrefix)
                        ? Path.GetFileNameWithoutExtension(inputPath)
                        : options.FileNamePrefix;

                    var groups = BuildSplitGroups(totalPages, options);
                    for (int gi = 0; gi < groups.Count; gi++)
                    {
                        ct.ThrowIfCancellationRequested();
                        string outFile = Path.Combine(outputDir, $"{prefix}_{gi + 1:D4}.pdf");
                        using var w = new PdfWriter(outFile);
                        var outDoc = new PdfDocument(w);
                        try
                        {
                            var merger = new PdfMerger(outDoc);
                            merger.Merge(srcDoc, groups[gi]);
                        }
                        finally { outDoc.Close(); }
                        progress?.Report((gi + 1) * 100 / groups.Count);
                    }
                }
                finally
                {
                    srcDoc.Close();
                    reader.Close();
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
                EnsureDirectory(outputPath);
                var sources = inputs.Select(i =>
                {
                    var r = new PdfReader(i.FilePath);
                    var d = new PdfDocument(r);
                    return (Doc: d, i.Reverse, Reader: r);
                }).ToList();

                try
                {
                    using var writer = new PdfWriter(outputPath);
                    var outDoc = new PdfDocument(writer);
                    try
                    {
                        int maxPages = sources.Max(s => s.Doc.GetNumberOfPages());

                        for (int i = 0; i < maxPages; i++)
                        {
                            ct.ThrowIfCancellationRequested();
                            foreach (var (doc, reverse, _) in sources)
                            {
                                int total = doc.GetNumberOfPages();
                                int idx = reverse ? total - i : i + 1;
                                if (idx >= 1 && idx <= total)
                                    outDoc.AddPage(doc.GetPage(idx).CopyTo(outDoc));
                            }
                            progress?.Report((i + 1) * 100 / maxPages);
                        }
                    }
                    finally { outDoc.Close(); }
                }
                finally
                {
                    foreach (var (doc, _, reader) in sources)
                    {
                        doc.Close();
                        reader.Close();
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
                EnsureDirectory(outputPath);
                var reader = new PdfReader(inputPath);
                var writer = new PdfWriter(outputPath);
                var doc = new PdfDocument(reader, writer);
                try
                {
                    int total = doc.GetNumberOfPages();
                    for (int i = 1; i <= total; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (pages.Contains(i))
                        {
                            var page = doc.GetPage(i);
                            int current = page.GetRotation();
                            page.SetRotation((current + angleDegrees) % 360);
                        }
                        progress?.Report(i * 100 / total);
                    }
                }
                finally { doc.Close(); }
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
                var reader = new PdfReader(inputPath);
                var srcDoc = new PdfDocument(reader);
                try
                {
                    var pageNums = pages.Resolve(srcDoc.GetNumberOfPages()).ToList();

                    switch (mode)
                    {
                        case ExtractMode.SingleFile:
                        {
                            EnsureDirectory(outputPath);
                            using var writer = new PdfWriter(outputPath);
                            var outDoc = new PdfDocument(writer);
                            try
                            {
                                var merger = new PdfMerger(outDoc);
                                merger.Merge(srcDoc, pageNums);
                            }
                            finally { outDoc.Close(); }
                            progress?.Report(100);
                            break;
                        }
                        case ExtractMode.OneFilePerPage:
                        {
                            Directory.CreateDirectory(outputPath);
                            string baseName = Path.GetFileNameWithoutExtension(inputPath);
                            for (int i = 0; i < pageNums.Count; i++)
                            {
                                ct.ThrowIfCancellationRequested();
                                string outFile = Path.Combine(outputPath, $"{baseName}_p{pageNums[i]:D4}.pdf");
                                using var writer = new PdfWriter(outFile);
                                var outDoc = new PdfDocument(writer);
                                try
                                {
                                    var merger = new PdfMerger(outDoc);
                                    merger.Merge(srcDoc, [pageNums[i]]);
                                }
                                finally { outDoc.Close(); }
                                progress?.Report((i + 1) * 100 / pageNums.Count);
                            }
                            break;
                        }
                        case ExtractMode.OneFilePerRange:
                        {
                            Directory.CreateDirectory(outputPath);
                            string baseName = Path.GetFileNameWithoutExtension(inputPath);
                            var rangeList = pages.SelectAll
                                ? new List<ModelPageRange> { new(1, srcDoc.GetNumberOfPages()) }
                                : pages.Ranges.ToList();

                            for (int ri = 0; ri < rangeList.Count; ri++)
                            {
                                ct.ThrowIfCancellationRequested();
                                var range = rangeList[ri];
                                var rangePages = pageNums
                                    .Where(p => p >= range.From && (range.To == null || p <= range.To))
                                    .ToList();
                                if (rangePages.Count == 0) continue;
                                string outFile = Path.Combine(outputPath, $"{baseName}_range{ri + 1:D2}.pdf");
                                using var writer = new PdfWriter(outFile);
                                var outDoc = new PdfDocument(writer);
                                try
                                {
                                    var merger = new PdfMerger(outDoc);
                                    merger.Merge(srcDoc, rangePages);
                                }
                                finally { outDoc.Close(); }
                                progress?.Report((ri + 1) * 100 / rangeList.Count);
                            }
                            break;
                        }
                    }
                }
                finally
                {
                    srcDoc.Close();
                    reader.Close();
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
                EnsureDirectory(outputPath);
                var baseReader   = new PdfReader(baseFilePath);
                var baseDoc      = new PdfDocument(baseReader);
                var insertReader = new PdfReader(insertFilePath);
                var insertDoc    = new PdfDocument(insertReader);
                try
                {
                    var insertPageNums = insertPages.Resolve(insertDoc.GetNumberOfPages()).ToList();

                    using var writer = new PdfWriter(outputPath);
                    var outDoc = new PdfDocument(writer);
                    try
                    {
                        int baseTotal = baseDoc.GetNumberOfPages();

                        switch (options.Mode)
                        {
                            case InsertMode.AtPosition:
                            {
                                int insertAt = Math.Max(1, Math.Min(options.Position, baseTotal + 1));
                                for (int i = 1; i <= baseTotal; i++)
                                {
                                    ct.ThrowIfCancellationRequested();
                                    if (i == insertAt)
                                        foreach (int pn in insertPageNums)
                                            outDoc.AddPage(insertDoc.GetPage(pn).CopyTo(outDoc));
                                    outDoc.AddPage(baseDoc.GetPage(i).CopyTo(outDoc));
                                }
                                if (insertAt > baseTotal)
                                    foreach (int pn in insertPageNums)
                                        outDoc.AddPage(insertDoc.GetPage(pn).CopyTo(outDoc));
                                break;
                            }
                            case InsertMode.EveryNPages:
                            {
                                int n = Math.Max(1, options.Position);
                                for (int i = 1; i <= baseTotal; i++)
                                {
                                    ct.ThrowIfCancellationRequested();
                                    outDoc.AddPage(baseDoc.GetPage(i).CopyTo(outDoc));
                                    if (i % n == 0)
                                        foreach (int pn in insertPageNums)
                                            outDoc.AddPage(insertDoc.GetPage(pn).CopyTo(outDoc));
                                }
                                break;
                            }
                            case InsertMode.AfterEveryPage:
                            {
                                for (int i = 1; i <= baseTotal; i++)
                                {
                                    ct.ThrowIfCancellationRequested();
                                    outDoc.AddPage(baseDoc.GetPage(i).CopyTo(outDoc));
                                    foreach (int pn in insertPageNums)
                                        outDoc.AddPage(insertDoc.GetPage(pn).CopyTo(outDoc));
                                }
                                break;
                            }
                        }
                        progress?.Report(100);
                    }
                    finally { outDoc.Close(); }
                }
                finally
                {
                    baseDoc.Close();   baseReader.Close();
                    insertDoc.Close(); insertReader.Close();
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
                EnsureDirectory(outputPath);
                var sequence = pageSequence.ToList();
                var openDocs = new Dictionary<string, (PdfDocument Doc, PdfReader Reader)>();

                try
                {
                    using var writer = new PdfWriter(outputPath);
                    var outDoc = new PdfDocument(writer);
                    try
                    {
                        for (int i = 0; i < sequence.Count; i++)
                        {
                            ct.ThrowIfCancellationRequested();
                            var (filePath, pageIndex) = sequence[i];

                            if (!openDocs.TryGetValue(filePath, out var entry))
                            {
                                var r = new PdfReader(filePath);
                                var d = new PdfDocument(r);
                                entry = (d, r);
                                openDocs[filePath] = entry;
                            }

                            outDoc.AddPage(entry.Doc.GetPage(pageIndex + 1).CopyTo(outDoc));
                            progress?.Report((i + 1) * 100 / sequence.Count);
                        }
                    }
                    finally { outDoc.Close(); }
                }
                finally
                {
                    foreach (var (doc, reader) in openDocs.Values)
                    {
                        doc.Close();
                        reader.Close();
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

    private static List<List<int>> BuildSplitGroups(int totalPages, SplitOptions options)
    {
        var groups = new List<List<int>>();
        switch (options.Mode)
        {
            case SplitMode.EveryPage:
                for (int i = 1; i <= totalPages; i++) groups.Add([i]);
                break;
            case SplitMode.EveryNPages:
            {
                int n = Math.Max(1, options.NPages ?? 1);
                for (int start = 1; start <= totalPages; start += n)
                {
                    var group = new List<int>();
                    for (int p = start; p < start + n && p <= totalPages; p++) group.Add(p);
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
                    for (int p = prev; p < cut && p <= totalPages; p++) group.Add(p);
                    if (group.Count > 0) groups.Add(group);
                    prev = cut;
                }
                var last = new List<int>();
                for (int p = prev; p <= totalPages; p++) last.Add(p);
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
                for (int i = 1; i <= totalPages; i++) groups.Add([i]);
                break;
        }
        return groups;
    }

    private static void EnsureDirectory(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    }
}
