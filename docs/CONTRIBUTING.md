# Contributing to SlackPDF

Thank you for your interest in contributing!

## How to contribute

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Make your changes
4. Run tests: `dotnet test`
5. Commit your changes: `git commit -m 'Add some feature'`
6. Push to the branch: `git push origin feature/my-feature`
7. Open a Pull Request

## Development setup

Requirements:
- Windows 10/11
- .NET 9 SDK
- Visual Studio 2022+ or VS Code with C# extension

```bash
git clone https://github.com/<owner>/slackpdf.git
cd slackpdf
dotnet restore
dotnet build
dotnet run --project src/SlackPDF
```

## Code style

- Follow the `.editorconfig` rules (4 spaces, CRLF)
- Use MVVM pattern — no business logic in code-behind
- All UI strings must use `{DynamicResource}` from localization files
- Write unit tests for new engine operations

## Reporting bugs

Use the [bug report template](.github/ISSUE_TEMPLATE/bug_report.md).

## License

By contributing, you agree that your contributions will be licensed under GNU GPL v3.
