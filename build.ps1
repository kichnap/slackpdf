# Build script: publish all components + create Inno Setup installer
# Usage:
#   .\build.ps1                  # full build
#   .\build.ps1 -SkipPublish     # only run Inno Setup (reuse existing publish/)
#   .\build.ps1 -SkipInstaller   # publish only, skip ISCC
param(
    [switch]$SkipPublish,
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"
$iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
$rid  = "win-x64"
$cfg  = "Release"

function Publish([string]$proj, [string]$out, [bool]$singleFile = $true) {
    $extra = if ($singleFile) { "-p:PublishSingleFile=true" } else { "" }
    dotnet publish $proj -c $cfg -r $rid --self-contained true $extra -o $out
    if ($LASTEXITCODE -ne 0) { throw "Publish failed: $proj" }
}

if (-not $SkipPublish) {
    Write-Host "Publishing..." -ForegroundColor Cyan

    # Main WPF application
    Publish "src/SlackPDF/SlackPDF.csproj"                                        "publish"              $false

    # Windows Service (single exe — embeds Ghostscript.NET + runtime)
    Publish "src/SlackPDF.PrintService/SlackPDF.PrintService.csproj"              "publish/service"      $true

    # Printer settings UI (launched from "Printing Preferences")
    Publish "src/SlackPDF.PrinterUI/SlackPDF.PrinterUI.csproj"                    "publish/printerui"    $true

    # CLI installer — runs during Inno Setup [Run] section (needs elevation anyway)
    Publish "src/SlackPDF.PrinterInstaller/SlackPDF.PrinterInstaller.csproj"      "publish/installer"    $true

    Write-Host "Published to ./publish" -ForegroundColor Green
}

if (-not $SkipInstaller) {
    if (-not (Test-Path $iscc)) {
        Write-Error "Inno Setup not found at: $iscc`nInstall from https://jrsoftware.org/isinfo.php"
        exit 1
    }
    Write-Host "Building installer..." -ForegroundColor Cyan
    & $iscc installer\SlackPDF.iss
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "Installer: installer\Output\SlackPDF-Setup-*.exe" -ForegroundColor Green
}
