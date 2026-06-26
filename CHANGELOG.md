# Changelog

All notable changes to SlackPDF will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2025-01-01

### Added
- Merge PDF files with per-file page selection, bookmark and AcroForm handling
- Split PDF by page, every N pages, at specific pages, by size, by bookmarks, even/odd
- Mix: interleave pages from two or more PDFs (straight/reverse order)
- Rotate pages (all, even, odd, or custom selection) by 90/180/270°
- Extract pages to single file, one file per page, or one file per range
- Insert PDF pages into another document at position, every N pages, or after every page
- Visual Composer: drag-and-drop page thumbnail assembler
- Dual PDF engine support: PDFsharp and iText (configurable in Settings)
- Russian and English UI localization with runtime language switching
- Light and Dark theme support via Material Design
- Self-contained Windows installer (Inno Setup)
- GitHub Actions CI/CD pipeline
