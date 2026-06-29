using PdfSharp.Pdf.IO;

namespace SlackPDF.PrintService;

/// <summary>
/// Sets /Rotate on PDF pages to fix landscape orientation from PSCRIPT5.DLL output.
///
/// PSCRIPT5 has two landscape encoding strategies:
///   A) Standard named sizes (A3, A4, …): portrait media box + 90° content rotation in stream.
///      GS produces a portrait PDF → we must add /Rotate 90.
///   B) Custom/elongated sizes: landscape dimensions used directly, no content rotation.
///      GS produces a landscape PDF → /Rotate must NOT be added (would flip it to portrait).
///
/// The correct heuristic: compare the PDF's actual page dimensions to the intended
/// output dimensions from the hint. Rotate only when the PDF is portrait but should be landscape.
/// </summary>
internal static class PdfPageRotator
{
    /// <summary>
    /// Returns true if the first page of the PDF has height > width
    /// (i.e. GS produced a portrait page, regardless of content rotation in the stream).
    /// </summary>
    internal static bool IsPortrait(string pdfPath)
    {
        using var document = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Import);
        if (document.PageCount == 0) return true;
        var page = document.Pages[0];
        return page.Height.Point > page.Width.Point;
    }

    internal static void SetRotation(string pdfPath, int degrees)
    {
        using var document = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Modify);
        foreach (var page in document.Pages)
            page.Rotate = degrees;
        document.Save(pdfPath);
    }
}
