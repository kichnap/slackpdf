<div align="center">
  <h1>SlackPDF</h1>
  <p>Fast, free, open-source PDF toolkit for Windows</p>

  [![Build](https://github.com/¬¿ÿ_USERNAME/slackpdf/actions/workflows/build.yml/badge.svg)](https://github.com/¬¿ÿ_USERNAME/slackpdf/actions)
  [![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)
  [![Release](https://img.shields.io/github/v/release/¬¿ÿ_USERNAME/slackpdf)](https://github.com/¬¿ÿ_USERNAME/slackpdf/releases/latest)
  [![Downloads](https://img.shields.io/github/downloads/¬¿ÿ_USERNAME/slackpdf/total)](https://github.com/¬¿ÿ_USERNAME/slackpdf/releases)

  **[üá∑üá∫ –ß–∏—Ç–∞—Ç—å –Ω–∞ —Ä—É—Å—Å–∫–æ–º](README.ru.md)**
</div>

---

## Features

| Module | Description |
|---|---|
| **Merge** | Combine PDFs with per-file page ranges, bookmark and AcroForm handling, table of contents |
| **Split** | Split by page, every N pages, at specific pages, by file size, or by bookmark level |
| **Mix** | Interleave pages from two or more PDFs ‚Äî perfect for single-sided scans |
| **Rotate** | Rotate all, even, odd, or selected pages by 90 / 180 / 270¬∞ |
| **Extract** | Extract single pages or ranges into a new PDF |
| **Insert** | Insert a PDF (or its pages) into another at any position or periodically |
| **Visual Composer** ‚≠ê | Drag page thumbnails from multiple documents and arrange them freely into a new PDF |

## Download

üëâ **[Latest release](https://github.com/¬¿ÿ_USERNAME/slackpdf/releases/latest)** ‚Äî download `SlackPDF-Setup-x.x.x.exe`

Windows 10 / 11 x64. No .NET installation required (self-contained).

## Build from source

**Requirements:** [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0), Windows 10/11 x64

```bash
git clone https://github.com/¬¿ÿ_USERNAME/slackpdf.git
cd slackpdf
dotnet restore
```

### Run in development mode

Starts the app directly without packaging. Fast iteration ‚Äî no installer needed.

```bash
dotnet run --project src/SlackPDF
```

### Build (debug)

Compiles to `src/SlackPDF/bin/Debug/net9.0-windows/`. The `.exe` requires .NET 9 installed on the target machine.

```bash
dotnet build
```

### Build (release, framework-dependent)

Smaller output, but requires .NET 9 to be installed on the target machine.

```bash
dotnet build --configuration Release
```

### Publish (self-contained single file) ‚Äî for distribution

Produces one standalone `SlackPDF.exe` in `publish/` that runs on any Windows 10/11 x64 machine without .NET installed.

```bash
dotnet publish src/SlackPDF/SlackPDF.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -o publish/
```

### Run tests

```bash
dotnet test
```

### Run benchmarks (PDFsharp vs iText)

```bash
dotnet run --project src/SlackPDF.Tests -c Release -- --filter "*Merge*"
```

### Build installer (requires [Inno Setup](https://jrsoftware.org/isinfo.php))

First publish the self-contained build (see above), then:

```bash
iscc installer/SlackPDF.iss
# Output: installer/Output/SlackPDF-Setup-1.0.0.exe
```

## Why SlackPDF?

- **Free forever** ‚Äî GPL v3, no ads, no telemetry, no cloud
- **Visual Composer** ‚Äî unique drag-and-drop page assembler not found in PDFsam
- **Fast** ‚Äî page operations copy PDF streams as-is, no re-encoding of graphics
- **Lightweight** ‚Äî minimal dependencies, single executable

## Contributing

PRs and issues are welcome! See [CONTRIBUTING.md](docs/CONTRIBUTING.md).

## License

[GNU GPL v3](LICENSE) ¬© SlackPDF Contributors
