using Ghostscript.NET.Processor;
using Ghostscript.NET;
using SlackPDF.PrinterShared;

namespace SlackPDF.PrintService;

public class GhostscriptConverter
{
    private readonly string _gsDllPath;

    public GhostscriptConverter()
    {
        _gsDllPath = FindGhostscriptDll();
    }

    public async Task<string> ConvertAsync(
        string psInputPath,
        PdfQuality quality,
        CancellationToken ct)
    {
        var outputPath = Path.ChangeExtension(psInputPath, ".pdf");

        // PSCRIPT5.DLL encodes landscape as portrait media box + 90° content rotation.
        // We let GS use the PS's own media box (portrait), producing a portrait PDF
        // with correctly positioned landscape content. Landscape detection and page
        // rotation (/Rotate 90) is applied after conversion via PDFsharp.
        var gsArgs = new[]
        {
            "-dBATCH",
            "-dNOPAUSE",
            "-dNOSAFER",
            "-sDEVICE=pdfwrite",
            $"-dPDFSETTINGS={QualityToGsFlag(quality)}",
            "-dCompatibilityLevel=1.7",
            "-dEmbedAllFonts=true",
            "-dSubsetFonts=true",
            $"-sOutputFile={outputPath}",
            psInputPath
        };

        await Task.Run(() =>
        {
            var version = new GhostscriptVersionInfo(_gsDllPath);
            using var processor = new GhostscriptProcessor(version);
            processor.StartProcessing(gsArgs, null);
        }, ct);

        return outputPath;
    }

    private static string QualityToGsFlag(PdfQuality q) => q switch
    {
        PdfQuality.Screen   => "/screen",
        PdfQuality.Ebook    => "/ebook",
        PdfQuality.High     => "/printer",
        PdfQuality.Prepress => "/prepress",
        _                   => "/printer"
    };

    private static string FindGhostscriptDll()
    {
        var candidates = new[]
        {
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "SlackPDF", "ghostscript", "bin", "gsdll64.dll"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "gs", "gs10.03.1", "bin", "gsdll64.dll"),
        };

        foreach (var path in candidates)
            if (File.Exists(path)) return path;

        return GhostscriptVersionInfo.GetLastInstalledVersion().DllPath;
    }
}
