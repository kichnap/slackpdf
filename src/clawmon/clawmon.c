/*
 * clawmon.c -- SlackPDF Print Port Monitor DLL
 *
 * Implements InitializePrintMonitor2 (MONITOR2 interface, Vista+).
 *
 * IPC: writes PostScript stream to ProgramData\SlackPDF\spool\<id>.ps,
 *      then writes a small job descriptor  ProgramData\SlackPDF\spool\<id>.job
 *      containing "psPath|jobName|appName" (UTF-8).  The SlackPDF Print Service
 *      watches that directory with FileSystemWatcher and picks up *.job files.
 *
 * No named pipe, no COM, no external dependencies beyond kernel32/winspool.
 */

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <winspool.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define MAX_PATH_W   (MAX_PATH * 2)
#define PORT_NAME    L"SlackPDF:"
#define MONITOR_NAME L"SlackPDF Port Monitor"
#define PORT_DESC    L"SlackPDF Virtual PDF Port"

/* -----------------------------------------------------------------------
 * Per-port state
 * ----------------------------------------------------------------------- */
typedef struct _PORT_STATE {
    HANDLE hFile;
    WCHAR  psPath[MAX_PATH_W];
    WCHAR  jobName[256];
    WCHAR  appName[256];
} PORT_STATE;

/* -----------------------------------------------------------------------
 * MONITOR2 -- layout must exactly match winsplp.h on x64 Windows.
 * Defined inline to avoid WDK dependency.
 * ----------------------------------------------------------------------- */
typedef struct _MONITOR2 *LPMONITOR2;

typedef struct _MONITOR2 {
    DWORD cbSize;
    BOOL  (WINAPI *pfnEnumPorts)      (HANDLE, LPWSTR, DWORD, LPBYTE, DWORD, LPDWORD, LPDWORD);
    BOOL  (WINAPI *pfnOpenPort)       (HANDLE, LPWSTR, PHANDLE);
    BOOL  (WINAPI *pfnOpenPortEx)     (HANDLE, HANDLE, LPWSTR, LPWSTR, PHANDLE, LPMONITOR2);
    BOOL  (WINAPI *pfnStartDocPort)   (HANDLE, LPWSTR, DWORD, DWORD, LPBYTE);
    BOOL  (WINAPI *pfnWritePort)      (HANDLE, LPBYTE, DWORD, LPDWORD);
    BOOL  (WINAPI *pfnReadPort)       (HANDLE, LPBYTE, DWORD, LPDWORD);
    BOOL  (WINAPI *pfnEndDocPort)     (HANDLE);
    BOOL  (WINAPI *pfnClosePort)      (HANDLE);
    BOOL  (WINAPI *pfnAddPort)        (HANDLE, LPWSTR, HWND, LPWSTR);
    BOOL  (WINAPI *pfnAddPortEx)      (HANDLE, LPWSTR, DWORD, LPBYTE, LPWSTR);
    BOOL  (WINAPI *pfnConfigurePort)  (HANDLE, LPWSTR, HWND, LPWSTR);
    BOOL  (WINAPI *pfnDeletePort)     (HANDLE, LPWSTR, HWND, LPWSTR);
    BOOL  (WINAPI *pfnGetPrinterDataFromPort)(HANDLE, DWORD, LPWSTR, LPWSTR, DWORD, LPWSTR, DWORD, LPDWORD);
    BOOL  (WINAPI *pfnSetPortTimeOuts)(HANDLE, LPCOMMTIMEOUTS, DWORD);
    BOOL  (WINAPI *pfnXcvOpenPort)    (HANDLE, LPCWSTR, ACCESS_MASK, PHANDLE);
    DWORD (WINAPI *pfnXcvDataPort)    (HANDLE, LPCWSTR, PBYTE, DWORD, PBYTE, DWORD, PDWORD);
    BOOL  (WINAPI *pfnXcvClosePort)   (HANDLE);
} MONITOR2;

typedef struct _MONITORINIT {
    DWORD   cbSize;
    HANDLE  hSpooler;
    HKEY    hckRegistryRoot;
    LPVOID  pMonitorReg;
    BOOL    bLocal;
    LPCWSTR pszServerName;
} MONITORINIT, *PMONITORINIT;

/* -----------------------------------------------------------------------
 * Spool directory: %ProgramData%\SlackPDF\spool
 * ----------------------------------------------------------------------- */
static void GetSpoolDir(WCHAR *out, size_t cch)
{
    WCHAR base[MAX_PATH] = L"C:\\ProgramData";
    GetEnvironmentVariableW(L"ProgramData", base, MAX_PATH);
    _snwprintf_s(out, cch, _TRUNCATE, L"%s\\SlackPDF\\spool", base);
}

/* Unique numeric ID from system time XOR thread/process IDs -- no COM needed */
static ULONGLONG NewUniqueId(void)
{
    FILETIME ft;
    GetSystemTimeAsFileTime(&ft);
    ULONGLONG ts = ((ULONGLONG)ft.dwHighDateTime << 32) | ft.dwLowDateTime;
    return ts ^ ((ULONGLONG)GetCurrentThreadId() << 16) ^ GetCurrentProcessId();
}

static void BuildFilePaths(WCHAR *psPath, WCHAR *jobPath, size_t cch)
{
    WCHAR dir[MAX_PATH_W];
    GetSpoolDir(dir, MAX_PATH_W);

    /* Ensure directories exist */
    WCHAR parent[MAX_PATH_W];
    _snwprintf_s(parent, MAX_PATH_W, _TRUNCATE, L"%s\\SlackPDF",
                 dir);   /* approximate -- GetSpoolDir already has SlackPDF */
    /* Create full chain */
    WCHAR base[MAX_PATH] = L"C:\\ProgramData";
    GetEnvironmentVariableW(L"ProgramData", base, MAX_PATH);
    WCHAR slackPdfDir[MAX_PATH_W];
    _snwprintf_s(slackPdfDir, MAX_PATH_W, _TRUNCATE, L"%s\\SlackPDF", base);
    CreateDirectoryW(slackPdfDir, NULL);
    CreateDirectoryW(dir, NULL);

    ULONGLONG id = NewUniqueId();
    _snwprintf_s(psPath,  cch, _TRUNCATE, L"%s\\%016llX.ps",  dir, id);
    _snwprintf_s(jobPath, cch, _TRUNCATE, L"%s\\%016llX.job", dir, id);
}

/* Write UTF-8 job descriptor: psPath|jobName|appName */
static void WriteJobFile(const WCHAR *jobPath,
                         const WCHAR *psPath,
                         const WCHAR *jobName,
                         const WCHAR *appName)
{
    WCHAR  wbuf[MAX_PATH_W + 512];
    char  *utf8;
    int    cbUtf8;
    DWORD  written;
    HANDLE hf;

    _snwprintf_s(wbuf, ARRAYSIZE(wbuf), _TRUNCATE,
                 L"%s|%s|%s", psPath, jobName, appName);

    cbUtf8 = WideCharToMultiByte(CP_UTF8, 0, wbuf, -1, NULL, 0, NULL, NULL);
    utf8   = (char *)malloc(cbUtf8);
    if (!utf8) return;
    WideCharToMultiByte(CP_UTF8, 0, wbuf, -1, utf8, cbUtf8, NULL, NULL);

    hf = CreateFileW(jobPath, GENERIC_WRITE, 0, NULL,
                     CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hf != INVALID_HANDLE_VALUE) {
        WriteFile(hf, utf8, cbUtf8 - 1, &written, NULL);
        CloseHandle(hf);
    }
    free(utf8);
}

/* -----------------------------------------------------------------------
 * EnumPorts -- the spooler calls this to list ports.
 * Without returning "SlackPDF:" here, AddPrinter fails (error 1796).
 * ----------------------------------------------------------------------- */
static BOOL WINAPI Mon2EnumPorts(HANDLE hMon, LPWSTR pName, DWORD Level,
    LPBYTE pPorts, DWORD cbBuf, LPDWORD pcbNeeded, LPDWORD pcReturned)
{
    (void)hMon; (void)pName;
    *pcReturned = 0;

    if (Level == 1) {
        DWORD cbN     = (DWORD)((wcslen(PORT_NAME) + 1) * sizeof(WCHAR));
        DWORD cbTotal = sizeof(PORT_INFO_1W) + cbN;
        *pcbNeeded = cbTotal;
        if (cbBuf < cbTotal) { SetLastError(ERROR_INSUFFICIENT_BUFFER); return FALSE; }
        PORT_INFO_1W *pi = (PORT_INFO_1W *)pPorts;
        LPBYTE ps = pPorts + sizeof(PORT_INFO_1W);
        memcpy(ps, PORT_NAME, cbN);
        pi->pName   = (LPWSTR)ps;
        *pcReturned = 1;
        return TRUE;
    }

    if (Level == 2) {
        DWORD cbN  = (DWORD)((wcslen(PORT_NAME)    + 1) * sizeof(WCHAR));
        DWORD cbM  = (DWORD)((wcslen(MONITOR_NAME) + 1) * sizeof(WCHAR));
        DWORD cbD  = (DWORD)((wcslen(PORT_DESC)    + 1) * sizeof(WCHAR));
        DWORD cbT  = sizeof(PORT_INFO_2W) + cbN + cbM + cbD;
        *pcbNeeded = cbT;
        if (cbBuf < cbT) { SetLastError(ERROR_INSUFFICIENT_BUFFER); return FALSE; }
        PORT_INFO_2W *pi = (PORT_INFO_2W *)pPorts;
        LPBYTE ps = pPorts + sizeof(PORT_INFO_2W);
        memcpy(ps, PORT_NAME,    cbN); pi->pPortName    = (LPWSTR)ps; ps += cbN;
        memcpy(ps, MONITOR_NAME, cbM); pi->pMonitorName = (LPWSTR)ps; ps += cbM;
        memcpy(ps, PORT_DESC,    cbD); pi->pDescription = (LPWSTR)ps;
        pi->fPortType = PORT_TYPE_WRITE;
        pi->Reserved  = 0;
        *pcReturned   = 1;
        return TRUE;
    }

    SetLastError(ERROR_INVALID_LEVEL);
    return FALSE;
}

/* -----------------------------------------------------------------------
 * Port lifecycle
 * ----------------------------------------------------------------------- */
static BOOL WINAPI Mon2OpenPort(HANDLE hMon, LPWSTR pName, PHANDLE pHandle)
{
    (void)hMon; (void)pName;
    PORT_STATE *ps = (PORT_STATE *)calloc(1, sizeof(PORT_STATE));
    if (!ps) return FALSE;
    ps->hFile = INVALID_HANDLE_VALUE;
    *pHandle  = (HANDLE)ps;
    return TRUE;
}

static BOOL WINAPI Mon2OpenPortEx(HANDLE hMon, HANDLE hMonPort,
    LPWSTR pPortName, LPWSTR pPrinterName,
    PHANDLE pHandle, LPMONITOR2 pMon2)
{
    (void)hMonPort; (void)pPrinterName; (void)pMon2;
    return Mon2OpenPort(hMon, pPortName, pHandle);
}

static BOOL WINAPI Mon2StartDocPort(HANDLE hPort, LPWSTR pPrinterName,
    DWORD dwJobId, DWORD dwLevel, LPBYTE pDocInfo)
{
    PORT_STATE  *ps = (PORT_STATE *)hPort;
    DOC_INFO_1W *di;
    WCHAR        jobPath[MAX_PATH_W];

    (void)dwJobId;
    if (!ps) return FALSE;

    if (dwLevel == 1 && pDocInfo) {
        di = (DOC_INFO_1W *)pDocInfo;
        wcsncpy_s(ps->jobName, 256,
                  di->pDocName ? di->pDocName : L"Document", _TRUNCATE);
    } else {
        wcscpy_s(ps->jobName, 256, L"Document");
    }
    wcsncpy_s(ps->appName, 256,
              pPrinterName ? pPrinterName : L"Unknown", _TRUNCATE);

    BuildFilePaths(ps->psPath, jobPath, MAX_PATH_W);

    ps->hFile = CreateFileW(ps->psPath, GENERIC_WRITE, 0, NULL,
                            CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    return (ps->hFile != INVALID_HANDLE_VALUE);
}

static BOOL WINAPI Mon2WritePort(HANDLE hPort, LPBYTE pBuffer,
    DWORD cbBuf, LPDWORD pcbWritten)
{
    PORT_STATE *ps = (PORT_STATE *)hPort;
    if (!ps || ps->hFile == INVALID_HANDLE_VALUE) return FALSE;
    return WriteFile(ps->hFile, pBuffer, cbBuf, pcbWritten, NULL);
}

static BOOL WINAPI Mon2ReadPort(HANDLE hPort, LPBYTE pBuffer,
    DWORD cbBuffer, LPDWORD pcbRead)
{
    (void)hPort; (void)pBuffer; (void)cbBuffer;
    *pcbRead = 0;
    return TRUE;
}

static BOOL WINAPI Mon2EndDocPort(HANDLE hPort)
{
    PORT_STATE *ps = (PORT_STATE *)hPort;
    if (!ps) return FALSE;

    if (ps->hFile != INVALID_HANDLE_VALUE) {
        CloseHandle(ps->hFile);
        ps->hFile = INVALID_HANDLE_VALUE;

        /* Derive .job path from .ps path (same base name, different extension) */
        WCHAR jobPath[MAX_PATH_W];
        wcsncpy_s(jobPath, MAX_PATH_W, ps->psPath, _TRUNCATE);
        WCHAR *ext = wcsrchr(jobPath, L'.');
        if (ext) wcscpy_s(ext, 5, L".job");

        WriteJobFile(jobPath, ps->psPath, ps->jobName, ps->appName);
    }
    return TRUE;
}

static BOOL WINAPI Mon2ClosePort(HANDLE hPort)
{
    PORT_STATE *ps = (PORT_STATE *)hPort;
    if (!ps) return FALSE;
    if (ps->hFile != INVALID_HANDLE_VALUE) CloseHandle(ps->hFile);
    free(ps);
    return TRUE;
}

/* -----------------------------------------------------------------------
 * Port management UI -- not implemented
 * ----------------------------------------------------------------------- */
static BOOL WINAPI Mon2AddPort(HANDLE h, LPWSTR a, HWND b, LPWSTR c)
{ (void)h;(void)a;(void)b;(void)c; return FALSE; }

static BOOL WINAPI Mon2AddPortEx(HANDLE h, LPWSTR a, DWORD b, LPBYTE c, LPWSTR d)
{ (void)h;(void)a;(void)b;(void)c;(void)d; return FALSE; }

static BOOL WINAPI Mon2ConfigurePort(HANDLE h, LPWSTR a, HWND b, LPWSTR c)
{ (void)h;(void)a;(void)b;(void)c; return FALSE; }

static BOOL WINAPI Mon2DeletePort(HANDLE h, LPWSTR a, HWND b, LPWSTR c)
{ (void)h;(void)a;(void)b;(void)c; return FALSE; }

static BOOL WINAPI Mon2GetPrinterDataFromPort(HANDLE a, DWORD b, LPWSTR c,
    LPWSTR d, DWORD e, LPWSTR f, DWORD g, LPDWORD h)
{ (void)a;(void)b;(void)c;(void)d;(void)e;(void)f;(void)g;(void)h; return FALSE; }

static BOOL WINAPI Mon2SetPortTimeOuts(HANDLE h, LPCOMMTIMEOUTS t, DWORD r)
{ (void)h;(void)t;(void)r; return TRUE; }

static BOOL WINAPI Mon2XcvOpenPort(HANDLE h, LPCWSTR o, ACCESS_MASK a, PHANDLE p)
{ (void)h;(void)o;(void)a; *p=(HANDLE)1; return TRUE; }

static DWORD WINAPI Mon2XcvDataPort(HANDLE h, LPCWSTR n,
    PBYTE i, DWORD ci, PBYTE o, DWORD co, PDWORD p)
{ (void)h;(void)n;(void)i;(void)ci;(void)o;(void)co;(void)p;
  return ERROR_INVALID_PARAMETER; }

static BOOL WINAPI Mon2XcvClosePort(HANDLE h)
{ (void)h; return TRUE; }

/* -----------------------------------------------------------------------
 * Static MONITOR2 function table
 * ----------------------------------------------------------------------- */
static MONITOR2 g_Monitor2 = {
    sizeof(MONITOR2),
    Mon2EnumPorts,
    Mon2OpenPort,
    Mon2OpenPortEx,
    Mon2StartDocPort,
    Mon2WritePort,
    Mon2ReadPort,
    Mon2EndDocPort,
    Mon2ClosePort,
    Mon2AddPort,
    Mon2AddPortEx,
    Mon2ConfigurePort,
    Mon2DeletePort,
    Mon2GetPrinterDataFromPort,
    Mon2SetPortTimeOuts,
    Mon2XcvOpenPort,
    Mon2XcvDataPort,
    Mon2XcvClosePort
};

/* -----------------------------------------------------------------------
 * DLL entry points
 * ----------------------------------------------------------------------- */
LPMONITOR2 WINAPI InitializePrintMonitor2(PMONITORINIT pMonitorInit,
                                          PHANDLE      phMonitor)
{
    (void)pMonitorInit;
    *phMonitor = (HANDLE)1;   /* dummy single-instance handle */
    return &g_Monitor2;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD reason, LPVOID lpReserved)
{
    (void)hModule; (void)reason; (void)lpReserved;
    return TRUE;
}
