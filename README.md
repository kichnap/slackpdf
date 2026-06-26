<div align="center">
  <h1>SlackPDF</h1>
  <p>Fast, free, open-source PDF toolkit for Windows</p>

  [![Build](https://github.com/<owner>/slackpdf/actions/workflows/build.yml/badge.svg)](https://github.com/<owner>/slackpdf/actions)
  [![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)
  [![Release](https://img.shields.io/github/v/release/<owner>/slackpdf)](https://github.com/<owner>/slackpdf/releases/latest)
  [![Downloads](https://img.shields.io/github/downloads/<owner>/slackpdf/total)](https://github.com/<owner>/slackpdf/releases)

  **[🇷🇺 Читать на русском](README.ru.md)**
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

👉 **[Latest release](https://github.com/<owner>/slackpdf/releases/latest)** — download `SlackPDF-Setup-x.x.x.exe`

Windows 10 / 11 x64. No .NET installation required (self-contained).

## Build from source

```bash
git clone https://github.com/<owner>/slackpdf.git
cd slackpdf
dotnet build
dotnet run --project src/SlackPDF
```

Requirements: [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Why SlackPDF?

- **Free forever** — GPL v3, no ads, no telemetry, no cloud
- **Visual Composer** — unique drag-and-drop page assembler not found in PDFsam
- **Fast** — page operations copy PDF streams as-is, no re-encoding of graphics
- **Lightweight** — minimal dependencies, single executable

## Contributing

PRs and issues are welcome! See [CONTRIBUTING.md](docs/CONTRIBUTING.md).

## License

[GNU GPL v3](LICENSE) © SlackPDF Contributors
