# SlackPDF User Guide

SlackPDF is a free PDF toolkit for Windows. It requires no .NET installation and never sends your files anywhere.

## Table of Contents

- [Interface](#interface)
- [Merge](#merge)
- [Split](#split)
- [Mix](#mix)
- [Rotate](#rotate)
- [Extract](#extract)
- [Insert](#insert)
- [Visual Composer](#visual-composer)
- [Settings](#settings)

---

## Interface

The main window has a sidebar for navigation and a work area on the right. Switching between modules is instant — each module remembers its state until the app is closed.

<!-- screenshot: main window -->

A **loading overlay** appears during processing. The **Cancel** button stops the operation at any point.

The **status bar** at the bottom of each module shows the result of the last operation or an error message.

---

## Merge

Combines multiple PDF files into one. Supports per-file page range selection.

<!-- screenshot: Merge module -->

### How to use

1. Click **Add files** or drag PDF files directly into the table
2. Reorder files using the ↑↓ arrows in the rightmost column
3. In the **Page selection** column, specify which pages to include (leave empty for all pages):
   - `1-5` — pages 1 through 5
   - `1,3,7` — specific pages
   - `2-` — from page 2 to the end
   - `1-3,7,10-` — combinations
4. Choose bookmark and AcroForm options
5. Set the output file and click **Run**

### Options

| Option | Description |
|---|---|
| Bookmarks | Merge bookmarks from all files / discard / create one entry per file |
| AcroForms | Discard form fields / merge / merge with renamed fields |
| Table of contents | Prepend a TOC page with file names and page numbers |

---

## Split

Splits a single PDF into multiple parts.

<!-- screenshot: Split module -->

### How to use

1. Choose the input file via **Browse** or drag it in
2. Select a split mode
3. Set the output folder (if left empty, files are saved next to the source)
4. Click **Run**

### Split modes

| Mode | Description |
|---|---|
| Every page | Each page becomes a separate file |
| Every N pages | Groups of N pages |
| At page numbers | Split at specified positions, e.g. `3,7,12` |
| By file size | Parts stay within the specified size limit (MB) |
| By bookmarks | Split at each bookmark of the selected level |

---

## Mix

Interleaves pages from two or more PDF files. Perfect for duplex documents scanned on a single-sided scanner — odd pages in one file, even pages in another.

<!-- screenshot: Mix module -->

### How to use

1. Add files via **Add files** or drag them in
2. The order in the table controls the interleave pattern: page 1 of file 1, page 1 of file 2, page 2 of file 1, and so on
3. Set the output file and click **Run**

> **Tip for duplex scanning:** scan odd pages into file A, scan even pages in reverse order into file B, then add B with the "Reverse order" option enabled.

---

## Rotate

Rotates pages in a PDF by 90°, 180°, or 270°.

<!-- screenshot: Rotate module -->

### How to use

1. Choose the input file
2. Select the rotation angle: 90° / 180° / 270°
3. Choose which pages to rotate:
   - **All pages**
   - **Even** / **Odd** pages
   - **Page selection** — type a range manually or click **Choose pages…**
4. Set the output file (or enable "Overwrite original")
5. Click **Run**

### Visual page picker

The **Choose pages…** button opens a panel showing thumbnails of all pages.

<!-- screenshot: page picker panel -->

- Click a thumbnail to select it (highlighted in blue)
- **Select all** / **Clear** for quick bulk selection
- **Apply** transfers the selected pages to the input field and closes the panel

---

## Extract

Extracts selected pages from a PDF into a new file.

<!-- screenshot: Extract module -->

### How to use

1. Choose the input file
2. In the thumbnail panel, select the pages you want (click, Ctrl+click, Shift+click for ranges)
3. Choose the output mode:
   - **Single file** — all selected pages in one PDF
   - **One file per page** — each page as a separate file
4. Set the output file and click **Run**

---

## Insert

Inserts one PDF into another.

<!-- screenshot: Insert module -->

### How to use

1. Choose the **base document** — the one to insert into
2. Choose the **document to insert**
3. Set the insertion position:
   - **After page N** — insert after a specific page
   - **Periodically every N pages** — repeat the insert at regular intervals
4. Set the output file and click **Run**

---

## Visual Composer

The most flexible tool: assemble a new PDF from arbitrary pages of multiple documents in any order by dragging thumbnails.

<!-- screenshot: Visual Composer -->

### How to use

**Source panel (left)**

1. Click **+** or drag PDF files into the left panel
2. Each document gets a colour-coded label (A, B, C…)
3. Switch between documents by clicking their tabs
4. Hover over a tab to see a tooltip with the file name, page count, and file size

**Assembly area (right)**

5. Select pages in the left panel (click, Ctrl+click, Shift+click for a range)
6. Drag the selected pages into the assembly area on the right
7. Reorder pages in the assembly area by dragging
8. Right-click a thumbnail in the assembly for a context menu (remove, move to first, move to last, duplicate)

**Toolbar**

| Button | Action |
|---|---|
| Auto-order | Sort by document label then page number |
| Clear all | Remove all pages from the assembly |
| Save PDF | Compile and save the output file |

<!-- screenshot: assembly with multiple documents -->

---

## Settings

<!-- screenshot: Settings -->

| Option | Description |
|---|---|
| Language | Russian / English — applied immediately, no restart needed |
| Theme | Light / Dark |
| After saving | Do nothing / Open folder / Open file |
| Clear cache | Deletes cached page thumbnails |
