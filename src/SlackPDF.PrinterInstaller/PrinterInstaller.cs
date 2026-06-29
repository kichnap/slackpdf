using System.Runtime.InteropServices;

namespace SlackPDF.PrinterInstaller;

public static class PrinterInstaller
{
    private const string PrinterName  = "SlackPDF";
    private const string DriverName   = "SlackPDF PS";
    private const string ServiceName  = "SlackPDFPrintService";

    // File-based port registered with the built-in "Local Port" monitor (localspl.dll).
    // localspl writes raw PostScript to this path; our service watches for it via FileSystemWatcher.
    // Named-pipe ports (\\.\pipe\...) appear to be unsupported by localspl — it treats \\ as UNC.
    private static readonly string PortName = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "SlackPDF", "spool", "input.ps");

    // Registry key where Local Port monitor stores its port list
    private const string LocalPortsKey =
        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Ports";

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "SlackPDF", "install.log");

    private static void Log(string message)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
        var line = $"{DateTime.Now:HH:mm:ss} {message}";
        File.AppendAllText(LogPath, line + Environment.NewLine);
        Console.WriteLine(line);
    }

    // -------------------------------------------------------------------------
    // Install
    // -------------------------------------------------------------------------

    public static void Install(string installDir)
    {
        File.WriteAllText(LogPath, $"=== SlackPDF Install {DateTime.Now} ==={Environment.NewLine}");
        Log($"installDir = {installDir}");

        // Register pipe port with the built-in Local Port monitor
        Log("Adding port to Local Port monitor...");
        AddPrintPort();

        // Write PS driver info to registry (pscript5.dll from driver store)
        Log("Installing PS driver...");
        InstallPsDriver(installDir);

        // Spooler restart picks up both the new port and the new driver
        Log("Restarting Spooler...");
        RestartSpooler();

        // Remove existing printer before recreating (idempotent reinstall).
        // AddPrinter fails with ERROR_PRINTER_ALREADY_EXISTS (1802) otherwise.
        Log("Removing existing printer (if any)...");
        DeletePrinterByName();

        Log("Creating printer...");
        AddPrinter();

        Log("Registering PrinterUI...");
        RegisterPrinterUI(installDir);

        Log("Registering Toast AUMID...");
        RegisterToastAumid(installDir);

        // Stop & delete existing service so sc.exe create succeeds on reinstall.
        Log("Removing existing service (if any)...");
        StopAndDeleteService();

        Log("Registering Windows Service...");
        RegisterService(installDir);

        Log("Installation complete.");
    }

    // -------------------------------------------------------------------------
    // Uninstall
    // -------------------------------------------------------------------------

    public static void Uninstall()
    {
        Console.WriteLine("Stopping and deleting service...");
        StopAndDeleteService();

        Console.WriteLine("Deleting printer...");
        DeletePrinterByName();

        Console.WriteLine("Deleting driver registry...");
        try { Microsoft.Win32.Registry.LocalMachine.DeleteSubKeyTree(
            DriverRegBase + DriverName, throwOnMissingSubKey: false); }
        catch { }

        Console.WriteLine("Removing PPD from driver store...");
        try { File.Delete(Path.Combine(Environment.SystemDirectory,
            "spool", "DRIVERS", "x64", "3", "SlackPDF.ppd")); }
        catch { }

        Console.WriteLine("Removing port from Local Port monitor...");
        RemovePrintPort();

        Console.WriteLine("Uninstall complete.");
    }

    // -------------------------------------------------------------------------
    // Port — stored in the Local Port monitor's port list (same as PDF24)
    // -------------------------------------------------------------------------

    private static void AddPrintPort()
    {
        using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
            LocalPortsKey, writable: true);
        if (key == null) throw new InvalidOperationException($"Registry key not found: {LocalPortsKey}");
        key.SetValue(PortName, "", Microsoft.Win32.RegistryValueKind.String);
        Log($"  Port: HKLM\\{LocalPortsKey}\\{PortName} = \"\"");
    }

    private static void RemovePrintPort()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                LocalPortsKey, writable: true);
            key?.DeleteValue(PortName, throwOnMissingValue: false);
        }
        catch { }
    }

    // -------------------------------------------------------------------------
    // PS Driver — write directly to registry, same value names as PDF24.
    // Spooler reads these on startup; no AddPrinterDriver / pnputil needed.
    // -------------------------------------------------------------------------

    private const string DriverRegBase =
        @"SYSTEM\CurrentControlSet\Control\Print\Environments\Windows x64\Drivers\Version-3\";

    private static void InstallPsDriver(string installDir)
    {
        var driverStore = Path.Combine(Environment.SystemDirectory, "spool", "DRIVERS", "x64", "3");
        var ppdSrc      = Path.Combine(installDir, "driver", "SlackPDF.ppd");
        var ppdDst      = Path.Combine(driverStore, "SlackPDF.ppd");
        File.Copy(ppdSrc, ppdDst, overwrite: true);
        Log($"  PPD → {ppdDst}");

        var driverKey = DriverRegBase + DriverName;
        using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(driverKey, writable: true);
        key.SetValue("Driver",             "PSCRIPT5.DLL",  Microsoft.Win32.RegistryValueKind.String);
        key.SetValue("Configuration File", "PS5UI.DLL",     Microsoft.Win32.RegistryValueKind.String);
        key.SetValue("Data File",          "SlackPDF.ppd",  Microsoft.Win32.RegistryValueKind.String);
        key.SetValue("Help File",          "PSCRIPT.HLP",   Microsoft.Win32.RegistryValueKind.String);
        key.SetValue("Dependent Files",    new[] { "PSCRIPT.NTF" }, Microsoft.Win32.RegistryValueKind.MultiString);
        key.SetValue("Monitor",            "",              Microsoft.Win32.RegistryValueKind.String);
        key.SetValue("Datatype",           "RAW",           Microsoft.Win32.RegistryValueKind.String);
        key.SetValue("Version",            3,               Microsoft.Win32.RegistryValueKind.DWord);
        key.SetValue("Attributes",         2,               Microsoft.Win32.RegistryValueKind.DWord);
        Log($"  Driver registry: HKLM\\{driverKey}");
    }

    // -------------------------------------------------------------------------
    // Spooler restart
    // -------------------------------------------------------------------------

    private static void RestartSpooler()
    {
        foreach (var cmd in new[] { "stop", "start" })
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName               = "net.exe",
                Arguments              = $"{cmd} spooler",
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            };
            using var p = System.Diagnostics.Process.Start(psi)!;
            p.WaitForExit();
            Log($"  net {cmd} spooler: exit={p.ExitCode}");
        }
        System.Threading.Thread.Sleep(3000);
    }

    // -------------------------------------------------------------------------
    // Printer
    // -------------------------------------------------------------------------

    private static void AddPrinter()
    {
        var info = new PRINTER_INFO_2
        {
            pPrinterName    = PrinterName,
            pPortName       = PortName,
            pDriverName     = DriverName,
            pPrintProcessor = "WinPrint",
            pDatatype       = "RAW",
            Attributes      = 0
        };

        var handle = NativeMethods.AddPrinter(null, 2, ref info);
        var err    = Marshal.GetLastWin32Error();
        Log($"  AddPrinter: {(handle != IntPtr.Zero ? "OK" : $"FAILED (error {err})")}");
        if (handle != IntPtr.Zero)
            NativeMethods.ClosePrinter(handle);
        else if (err != 0xB7) // ERROR_ALREADY_EXISTS
            throw new InvalidOperationException($"AddPrinter failed: {err}");
    }

    private static void DeletePrinterByName()
    {
        var di = new PRINTER_DEFAULTS { DesiredAccess = 0x000F000C };
        if (NativeMethods.OpenPrinter(PrinterName, out var handle, ref di))
        {
            NativeMethods.DeletePrinter(handle);
            NativeMethods.ClosePrinter(handle);
        }
    }

    // -------------------------------------------------------------------------
    // PrinterUI registration
    // -------------------------------------------------------------------------

    private static void RegisterPrinterUI(string installDir)
    {
        var uiPath = Path.Combine(installDir, "printerui", "SlackPDF.PrinterUI.exe");
        using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
            $@"SYSTEM\CurrentControlSet\Control\Print\Printers\{PrinterName}", writable: true);
        key?.CreateSubKey("PrinterDriverData")
           .SetValue("UIPath", uiPath, Microsoft.Win32.RegistryValueKind.String);
    }

    // -------------------------------------------------------------------------
    // Toast AUMID — required for Windows 10 Toast Notifications from a service.
    // Registers under HKLM so the notification routes to any active user session.
    // -------------------------------------------------------------------------

    private const string ToastAumid = "SlackPDF.VirtualPrinter";

    private static void RegisterToastAumid(string installDir)
    {
        var exePath = Path.Combine(installDir, "SlackPDF.exe");
        var keyPath = $@"SOFTWARE\Classes\AppUserModelId\{ToastAumid}";
        using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(keyPath, writable: true);
        key.SetValue("DisplayName",  "SlackPDF Virtual Printer", Microsoft.Win32.RegistryValueKind.String);
        key.SetValue("IconUri",      $"{exePath},0",             Microsoft.Win32.RegistryValueKind.String);
        key.SetValue("ShowInSettings", 0,                        Microsoft.Win32.RegistryValueKind.DWord);
        Log($"  AUMID: HKLM\\{keyPath}");
    }

    // -------------------------------------------------------------------------
    // Windows Service
    // -------------------------------------------------------------------------

    private static void RegisterService(string installDir)
    {
        var exePath = Path.Combine(installDir, "service", "SlackPDF.PrintService.exe");
        Log($"  Service exe: {exePath}  exists={File.Exists(exePath)}");

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName        = "sc.exe",
            Arguments       = $"create {ServiceName} binPath= \"{exePath}\" start= auto DisplayName= \"SlackPDF Print Service\"",
            UseShellExecute = false,
            CreateNoWindow  = true
        };
        using var proc = System.Diagnostics.Process.Start(psi)!;
        proc.WaitForExit();

        var psi2 = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "sc.exe",
            Arguments              = $"start {ServiceName}",
            UseShellExecute        = false,
            CreateNoWindow         = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true
        };
        using var proc2 = System.Diagnostics.Process.Start(psi2)!;
        proc2.WaitForExit();
        Log($"  sc start exit={proc2.ExitCode}");
    }

    private static void StopAndDeleteService()
    {
        foreach (var verb in new[] { "stop", "delete" })
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName        = "sc.exe",
                Arguments       = $"{verb} {ServiceName}",
                UseShellExecute = false,
                CreateNoWindow  = true
            };
            using var proc = System.Diagnostics.Process.Start(psi)!;
            proc.WaitForExit();
        }
    }

    // -------------------------------------------------------------------------
    // P/Invoke
    // -------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct PRINTER_INFO_2
    {
        public string? pServerName;
        public string  pPrinterName;
        public string? pShareName;
        public string  pPortName;
        public string  pDriverName;
        public string? pComment;
        public string? pLocation;
        public IntPtr  pDevMode;
        public string? pSepFile;
        public string  pPrintProcessor;
        public string  pDatatype;
        public string? pParameters;
        public IntPtr  pSecurityDescriptor;
        public uint    Attributes;
        public uint    Priority;
        public uint    DefaultPriority;
        public uint    StartTime;
        public uint    UntilTime;
        public uint    Status;
        public uint    cJobs;
        public uint    AveragePPM;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct PRINTER_DEFAULTS
    {
        public IntPtr pDatatype;
        public IntPtr pDevMode;
        public uint   DesiredAccess;
    }

    private static class NativeMethods
    {
        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr AddPrinter(string? pName, uint Level, ref PRINTER_INFO_2 pPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, ref PRINTER_DEFAULTS pDefault);

        [DllImport("winspool.drv", SetLastError = true)]
        public static extern bool DeletePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        public static extern bool ClosePrinter(IntPtr hPrinter);
    }
}
