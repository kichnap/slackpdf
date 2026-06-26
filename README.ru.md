<div align="center">
  <h1>SlackPDF</h1>
  <p>Быстрый, бесплатный, open-source инструмент для работы с PDF на Windows</p>

  [![Build](https://github.com/<owner>/slackpdf/actions/workflows/build.yml/badge.svg)](https://github.com/<owner>/slackpdf/actions)
  [![Лицензия: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)
  [![Релиз](https://img.shields.io/github/v/release/<owner>/slackpdf)](https://github.com/<owner>/slackpdf/releases/latest)
  [![Скачивания](https://img.shields.io/github/downloads/<owner>/slackpdf/total)](https://github.com/<owner>/slackpdf/releases)

  **[🇬🇧 Read in English](README.md)**
</div>

---

## Возможности

| Модуль | Описание |
|---|---|
| **Объединить** | Склеить PDF с выбором диапазонов страниц, обработкой закладок, AcroForms и оглавлением |
| **Разделить** | Разбить по страницам, каждые N страниц, по номерам, по размеру файла или по закладкам |
| **Чередование** | Перемежать страницы из нескольких PDF — идеально для односторонних сканов |
| **Поворот** | Повернуть все, чётные, нечётные или выбранные страницы на 90 / 180 / 270° |
| **Извлечь** | Извлечь отдельные страницы или диапазоны в новый PDF |
| **Вставить** | Вставить один PDF внутрь другого в заданную позицию или с заданной периодичностью |
| **Визуальный сборщик** ⭐ | Перетаскивай миниатюры страниц из нескольких документов и собирай новый PDF в произвольном порядке |

## Скачать

👉 **[Последний релиз](https://github.com/<owner>/slackpdf/releases/latest)** — скачать `SlackPDF-Setup-x.x.x.exe`

Windows 10 / 11 x64. Установка .NET не требуется (self-contained).

## Сборка из исходников

```bash
git clone https://github.com/<owner>/slackpdf.git
cd slackpdf
dotnet build
dotnet run --project src/SlackPDF
```

Требования: [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Зачем SlackPDF?

- **Бесплатно навсегда** — GPL v3, без рекламы, без телеметрии, без облака
- **Визуальный сборщик** — уникальная сборка документа перетаскиванием миниатюр, которой нет в PDFsam
- **Быстро** — операции над страницами копируют потоки PDF как есть, без перекодирования графики
- **Легко** — минимум зависимостей, один исполняемый файл

## Участие в разработке

PR и issues приветствуются! См. [CONTRIBUTING.md](docs/CONTRIBUTING.md).

## Лицензия

[GNU GPL v3](LICENSE) © SlackPDF Contributors
