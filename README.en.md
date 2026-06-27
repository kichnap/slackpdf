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

### Run in development mode

Starts the app directly without packaging. Fast iteration — no installer needed.

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

### Publish (self-contained single file) — for distribution

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

### Build installer (requires [Inno Setup](https://jrsoftware.org/isinfo.php))

First publish the self-contained build (see above), then:

```bash
iscc installer/SlackPDF.iss
# Output: installer/Output/SlackPDF-Setup-1.0.0.exe
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

## Contributing

PRs and issues are welcome! See [CONTRIBUTING.md](docs/CONTRIBUTING.md).

## License

[GNU GPL v3](LICENSE) © SlackPDF Contributors
