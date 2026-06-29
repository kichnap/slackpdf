using System.Runtime.InteropServices;

namespace SlackPDF.PrintService;

/// <summary>
/// Launches a process in the active console user's Windows session.
/// Needed because the print service runs as SYSTEM in Session 0, which has UI isolation —
/// processes started normally cannot show windows on the user's desktop.
/// Uses WTSQueryUserToken + CreateProcessAsUser (standard Windows service pattern).
/// </summary>
internal static class UserSessionLauncher
{
    [DllImport("kernel32.dll")]
    private static extern uint WTSGetActiveConsoleSessionId();

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSQueryUserToken(uint SessionId, out IntPtr phToken);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcessAsUser(
        IntPtr hToken,
        string? lpApplicationName,
        string? lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string? lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("userenv.dll", SetLastError = true)]
    private static extern bool CreateEnvironmentBlock(
        out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

    [DllImport("userenv.dll")]
    private static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int    cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public uint   dwX, dwY, dwXSize, dwYSize;
        public uint   dwXCountChars, dwYCountChars;
        public uint   dwFillAttribute, dwFlags;
        public ushort wShowWindow, cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess, hThread;
        public uint   dwProcessId, dwThreadId;
    }

    private const uint NORMAL_PRIORITY_CLASS      = 0x00000020;
    private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    private const uint INFINITE                   = 0xFFFFFFFF;

    /// <summary>
    /// Launches <paramref name="exePath"/> in the active console session
    /// and waits up to <paramref name="timeout"/> for it to exit.
    /// Throws <see cref="InvalidOperationException"/> if no user is logged in or launch fails.
    /// </summary>
    public static async Task LaunchAsync(
        string exePath, string arguments, TimeSpan timeout, CancellationToken ct)
    {
        var sessionId = WTSGetActiveConsoleSessionId();
        if (sessionId == 0xFFFFFFFF)
            throw new InvalidOperationException("No active console session — no user is logged in.");

        if (!WTSQueryUserToken(sessionId, out var userToken))
            throw new InvalidOperationException(
                $"WTSQueryUserToken failed (error {Marshal.GetLastWin32Error()}). " +
                "Ensure the service has SE_TCB_PRIVILEGE (LocalSystem).");

        try
        {
            CreateEnvironmentBlock(out var envBlock, userToken, false);
            try
            {
                var si = new STARTUPINFO
                {
                    cb        = Marshal.SizeOf<STARTUPINFO>(),
                    lpDesktop = "winsta0\\default"   // user's interactive desktop
                };

                uint flags = NORMAL_PRIORITY_CLASS;
                if (envBlock != IntPtr.Zero) flags |= CREATE_UNICODE_ENVIRONMENT;

                var cmdLine = $"\"{exePath}\" {arguments}";

                if (!CreateProcessAsUser(
                        userToken, null, cmdLine,
                        IntPtr.Zero, IntPtr.Zero,
                        bInheritHandles: false, flags,
                        envBlock, null,
                        ref si, out var pi))
                    throw new InvalidOperationException(
                        $"CreateProcessAsUser failed (error {Marshal.GetLastWin32Error()}).");

                CloseHandle(pi.hThread);

                try
                {
                    var ms = (uint)Math.Min(timeout.TotalMilliseconds, INFINITE);
                    await Task.Run(() => WaitForSingleObject(pi.hProcess, ms), ct);
                }
                finally
                {
                    CloseHandle(pi.hProcess);
                }
            }
            finally
            {
                if (envBlock != IntPtr.Zero) DestroyEnvironmentBlock(envBlock);
            }
        }
        finally
        {
            CloseHandle(userToken);
        }
    }
}
