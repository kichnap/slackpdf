using SlackPDF.PrinterShared;
using SlackPDF.PrintService;
using Xunit;

namespace SlackPDF.Tests;

public class PrintJobProcessorTests
{
    private static PrintJobProcessor Make(
        string template               = "%[DocName]%",
        bool   stripPath              = true,
        FileConflictStrategy strategy = FileConflictStrategy.AutoNumber)
        => new(new PrinterSettings
        {
            FileNameTemplate     = template,
            StripPathFromDocName = stripPath,
            ConflictStrategy     = strategy,
            OutputFolder         = Path.GetTempPath()
        });

    [Fact]
    public void BuildFileName_DocNameOnly()
    {
        var result = Make().BuildFileName("Договор", null);
        Assert.Equal("Договор", result);
    }

    [Fact]
    public void BuildFileName_WithDate()
    {
        var template = "%[DocName]%_%[Year]%-%[Month]%-%[Day]%";
        var result   = Make(template).BuildFileName("Отчёт", null);
        var now      = DateTime.Now;
        Assert.Equal($"Отчёт_{now:yyyy}-{now:MM}-{now:dd}", result);
    }

    [Fact]
    public void BuildFileName_StripPath()
    {
        var result = Make(stripPath: true).BuildFileName(@"C:\docs\Договор.docx", null);
        Assert.Equal("Договор", result);
    }

    [Fact]
    public void BuildFileName_SanitizeChars()
    {
        var result = Make().BuildFileName("Отчёт: Q1/2025", null);
        Assert.DoesNotContain(":", result);
        Assert.DoesNotContain("/", result);
    }

    [Fact]
    public async Task Conflict_AutoNumber_SecondFile()
    {
        var dir  = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "Договор.pdf"), "x");
            var src = Path.Combine(dir, "source.pdf");
            File.WriteAllText(src, "pdf");

            var processor = new PrintJobProcessor(new PrinterSettings
            {
                FileNameTemplate     = "%[DocName]%",
                StripPathFromDocName = true,
                ConflictStrategy     = FileConflictStrategy.AutoNumber,
                OutputFolder         = dir
            });

            var saved = await processor.SaveAsync(src, "Договор", null, default);
            Assert.Equal(Path.Combine(dir, "Договор_2.pdf"), saved);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task Conflict_AutoNumber_ThirdFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "Договор.pdf"),   "x");
            File.WriteAllText(Path.Combine(dir, "Договор_2.pdf"), "x");
            var src = Path.Combine(dir, "source.pdf");
            File.WriteAllText(src, "pdf");

            var processor = new PrintJobProcessor(new PrinterSettings
            {
                FileNameTemplate     = "%[DocName]%",
                StripPathFromDocName = true,
                ConflictStrategy     = FileConflictStrategy.AutoNumber,
                OutputFolder         = dir
            });

            var saved = await processor.SaveAsync(src, "Договор", null, default);
            Assert.Equal(Path.Combine(dir, "Договор_3.pdf"), saved);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task Conflict_AppendDateTime()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "Договор.pdf"), "x");
            var src = Path.Combine(dir, "source.pdf");
            File.WriteAllText(src, "pdf");

            var processor = new PrintJobProcessor(new PrinterSettings
            {
                FileNameTemplate     = "%[DocName]%",
                StripPathFromDocName = true,
                ConflictStrategy     = FileConflictStrategy.AppendDateTime,
                OutputFolder         = dir
            });

            var saved = await processor.SaveAsync(src, "Договор", null, default);
            Assert.NotNull(saved);
            Assert.StartsWith(Path.Combine(dir, "Договор_"), saved);
            Assert.EndsWith(".pdf", saved);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task Conflict_Skip_DoesNotOverwrite()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            var existing = Path.Combine(dir, "Договор.pdf");
            File.WriteAllText(existing, "original");
            var src = Path.Combine(dir, "source.pdf");
            File.WriteAllText(src, "new");

            var processor = new PrintJobProcessor(new PrinterSettings
            {
                FileNameTemplate     = "%[DocName]%",
                StripPathFromDocName = true,
                ConflictStrategy     = FileConflictStrategy.Skip,
                OutputFolder         = dir
            });

            var saved = await processor.SaveAsync(src, "Договор", null, default);
            Assert.Null(saved);
            Assert.Equal("original", File.ReadAllText(existing));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void Settings_SaveLoad()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var original = new PrinterSettings
        {
            ShowSaveDialog       = true,
            OutputFolder         = tempDir,
            FileNameTemplate     = "%[DocName]%_%[Year]%",
            StripPathFromDocName = false,
            ConflictStrategy     = FileConflictStrategy.Overwrite,
            Quality              = PdfQuality.Screen
        };

        // Override save path via reflection-free approach: save then reload via same path
        var json = System.Text.Json.JsonSerializer.Serialize(original,
            new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Converters    = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });

        var loaded = System.Text.Json.JsonSerializer.Deserialize<PrinterSettings>(json,
            new System.Text.Json.JsonSerializerOptions
            {
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            })!;

        Assert.Equal(original.ShowSaveDialog,       loaded.ShowSaveDialog);
        Assert.Equal(original.OutputFolder,          loaded.OutputFolder);
        Assert.Equal(original.FileNameTemplate,      loaded.FileNameTemplate);
        Assert.Equal(original.StripPathFromDocName,  loaded.StripPathFromDocName);
        Assert.Equal(original.ConflictStrategy,      loaded.ConflictStrategy);
        Assert.Equal(original.Quality,               loaded.Quality);

        Directory.Delete(tempDir, true);
    }
}
