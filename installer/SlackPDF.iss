#define AppName "SlackPDF"
#define AppVersion "1.1.0"
#define AppPublisher "kichnap"
#define AppURL "https://github.com/kichnap/slackpdf"
#define AppExeName "SlackPDF.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputDir=Output
OutputBaseFilename=SlackPDF-Setup-{#AppVersion}
SetupIconFile=..\src\SlackPDF\Assets\icon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
LicenseFile=..\LICENSE
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian";  MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon";    Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "installprinter"; Description: "Установить виртуальный принтер PDF «SlackPDF»"; GroupDescription: "Компоненты:"; Flags: checkedonce

[Files]
; Main WPF application
Source: "..\publish\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
; Printer driver files (PS driver inf)
Source: "..\driver\*";                      DestDir: "{app}\driver";      Flags: ignoreversion recursesubdirs
; PrintService Windows Service
Source: "..\publish\service\*";             DestDir: "{app}\service";     Flags: ignoreversion recursesubdirs
; PrinterUI settings window
Source: "..\publish\printerui\*";           DestDir: "{app}\printerui";   Flags: ignoreversion recursesubdirs
; PrinterInstaller CLI
Source: "..\publish\installer\SlackPDF.PrinterInstaller.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}";       Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\SlackPDF.PrinterInstaller.exe"; Parameters: "download-ghostscript ""{app}"""; StatusMsg: "Загрузка Ghostscript..."; Flags: waituntilterminated; Tasks: installprinter
Filename: "{app}\SlackPDF.PrinterInstaller.exe"; Parameters: "install ""{app}"""; StatusMsg: "Установка принтера SlackPDF..."; Flags: waituntilterminated; Tasks: installprinter
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\SlackPDF.PrinterInstaller.exe"; Parameters: "uninstall"; Flags: waituntilterminated; RunOnceId: "UninstallPrinter"
