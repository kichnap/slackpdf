<div align="center">
  <img src="docs/assets/logo.png" width="96" alt="SlackPDF"/>
  <h1>SlackPDF</h1>
  <p>Fast, free, open-source PDF toolkit for Windows</p>

  [![Build](https://github.com/kichnap/slackpdf/actions/workflows/build.yml/badge.svg)](https://github.com/kichnap/slackpdf/actions)
  [![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)
  [![Release](https://img.shields.io/github/v/release/kichnap/slackpdf)](https://github.com/kichnap/slackpdf/releases/latest)
  [![Downloads](https://img.shields.io/github/downloads/kichnap/slackpdf/total)](https://github.com/kichnap/slackpdf/releases)

  **[🇷🇺 Читать на русском](README.md)**
</div>

---

## Features

| Module | Description |
|---|---|
| **Merge** | Combine PDFs with per-file page ranges, bookmark and AcroForm handling, table of contents |
| **Split** | Split by page, every N pages, at specific pages, by file size, or by bookmark level |
| **Mix** | Interleave pages from two or more PDFs — perfect for single-sided scans |
| **Rotate** | Rotate all, even, odd, or selected pages by 90 / 180 / 270° |
| **Extract** | Extract single pages or ranges into a new PDF |
| **Insert** | Insert a PDF (or its pages) into another at any position or periodically |
| **Visual Composer** ⭐ | Drag page thumbnails from multiple documents and arrange them freely into a new PDF |

## Download

👉 **[Latest release](https://github.com/kichnap/slackpdf/releases/latest)** — download `SlackPDF-Setup-x.x.x.exe`

Windows 10 / 11 x64. No .NET installation required (self-contained).

## Build from source

**Requirements:** [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0), Windows 10/11 x64

```bash
git clone https://github.com/kichnap/slackpdf.git
cd slackpdf
dotnet restore
```

### Quick run (development)

Starts the app directly without packaging. The fastest way to test changes.

```bash
dotnet run --project src/SlackPDF
```

### Build (debug)

Compiles to `src/SlackPDF/bin/Debug/net9.0-windows/win-x64/`. Self-contained — no .NET installation required.

```bash
dotnet build
```

### Build (release)

Compiles to `src/SlackPDF/bin/Release/net9.0-windows/win-x64/`. Self-contained, with optimizations.

```bash
dotnet build -c Release
```

### Publish — for distribution

Produces one standalone `SlackPDF.exe` in `publish/`. Used to build the installer.

```bash
dotnet publish src/SlackPDF/SlackPDF.csproj -c Release -o publish/
```

> All parameters (`self-contained`, `win-x64`, `PublishSingleFile`) are already set in `.csproj` — no need to repeat them on the command line.

### Run tests

```bash
dotnet test
```

### Build installer (requires [Inno Setup 6](https://jrsoftware.org/isinfo.php))

First publish (see above), then:

```bash
# If Inno Setup is on PATH:
iscc installer/SlackPDF.iss

# Or via full path:
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\SlackPDF.iss

# Output: installer/Output/SlackPDF-Setup-1.0.0.exe
```

Or use the script that does publish + installer in one step:

```powershell
.\build.ps1
```

## Why SlackPDF?

- **Free forever** — GPL v3, no ads, no telemetry, no cloud
- **Visual Composer** — unique drag-and-drop page assembler not found in PDFsam
- **Fast** — page operations copy PDF streams as-is, no re-encoding of graphics
- **Lightweight** — minimal dependencies, single executable

## Open-source components

All components are released under the MIT license, compatible with GPL v3.

| Component | Purpose | License |
|---|---|---|
| [PDFsharp](https://github.com/empira/PDFsharp) | PDF reading and writing | MIT |
| [PDFtoImage](https://github.com/sungaila/PDFtoImage) | Rendering pages to thumbnails | MIT |
| [SkiaSharp](https://github.com/mono/SkiaSharp) | Graphics rendering | MIT |
| [MaterialDesignThemes](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) | UI components and theme | MIT |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | MVVM infrastructure | MIT |

## Documentation

📖 [User Guide](docs/user-guide.en.md) — all modules explained with examples

## Contributing

PRs and issues are welcome! See [CONTRIBUTING.md](docs/CONTRIBUTING.md).

## License

[GNU GPL v3](LICENSE) © SlackPDF Contributors
