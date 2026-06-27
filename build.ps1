# Build script: publish + create installer
# Usage: .\build.ps1 [-SkipPublish] [-SkipInstaller]
param(
    [switch]$SkipPublish,
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"
$iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

if (-not $SkipPublish) {
    Write-Host "Publishing..." -ForegroundColor Cyan
    dotnet publish src/SlackPDF/SlackPDF.csproj -c Release -o publish
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "Published to ./publish" -ForegroundColor Green
}

if (-not $SkipInstaller) {
    if (-not (Test-Path $iscc)) {
        Write-Error "Inno Setup not found at: $iscc`nInstall it from https://jrsoftware.org/isinfo.php"
        exit 1
    }
    Write-Host "Building installer..." -ForegroundColor Cyan
    & $iscc installer\SlackPDF.iss
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "Installer built: installer\Output\SlackPDF-Setup-*.exe" -ForegroundColor Green
}
