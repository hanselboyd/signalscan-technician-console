using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using SignalScan.TechnicianConsole.Models;

namespace SignalScan.TechnicianConsole.Services;

public sealed class ReadOnlyDiagnosticCollector
{
    public Task<ScanResult> CollectAsync()
    {
        return Task.Run(() =>
        {
            var profile = CollectSystemProfile();
            var findings = CollectFindings(profile);
            return new ScanResult
            {
                ScanTimestamp = DateTimeOffset.Now,
                SystemProfile = profile,
                Findings = findings,
                OverallStatus = CalculateOverallStatus(findings)
            };
        });
    }

    private static SystemProfile CollectSystemProfile()
    {
        var memory = GetMemoryStatus();
        return new SystemProfile(
            ComputerName: Environment.MachineName,
            WindowsVersion: GetWindowsVersion(),
            WindowsBuild: ReadRegistryString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuildNumber"),
            CpuModel: ReadRegistryString(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString"),
            Ram: memory.TotalPhysicalBytes > 0 ? FormatBytes(memory.TotalPhysicalBytes) : "Unavailable",
            StorageSummary: GetStorageSummary(),
            Manufacturer: ReadRegistryString(@"HARDWARE\DESCRIPTION\System\BIOS", "SystemManufacturer"),
            Model: ReadRegistryString(@"HARDWARE\DESCRIPTION\System\BIOS", "SystemProductName"),
            BiosVersion: ReadRegistryString(@"HARDWARE\DESCRIPTION\System\BIOS", "BIOSVersion"),
            Uptime: FormatUptime(TimeSpan.FromMilliseconds(Environment.TickCount64)),
            CurrentUser: Environment.UserName);
    }

    private static IReadOnlyList<DiagnosticFinding> CollectFindings(SystemProfile profile)
    {
        var findings = new List<DiagnosticFinding>
        {
            new("System Profile", "Computer identity", HealthStatus.Good, $"Computer name: {profile.ComputerName}. Current user: {profile.CurrentUser}."),
            new("System Profile", "Windows version", IsUnavailable(profile.WindowsVersion) ? HealthStatus.ReviewRequired : HealthStatus.Good, $"{profile.WindowsVersion} build {profile.WindowsBuild}."),
            new("System Profile", "Processor", IsUnavailable(profile.CpuModel) ? HealthStatus.ReviewRequired : HealthStatus.Good, profile.CpuModel),
            new("System Profile", "Installed memory", IsUnavailable(profile.Ram) ? HealthStatus.ReviewRequired : HealthStatus.Good, profile.Ram),
            new("System Profile", "Storage summary", profile.StorageSummary.Contains("Low free space", StringComparison.OrdinalIgnoreCase) ? HealthStatus.AttentionNeeded : HealthStatus.Good, profile.StorageSummary),
            new("Performance", "Running process count", HealthStatus.Good, $"{SafeProcessCount()} processes were visible to the current user."),
            new("Maintenance", "Maintenance checks", HealthStatus.ReviewRequired, "Detailed update, event log, and disk health checks are planned for later tasks. No maintenance actions were performed."),
            new("Security", "Security posture checks", HealthStatus.ReviewRequired, "Defender, firewall, BitLocker, and account checks are planned for later tasks. No security settings were changed.")
        };

        if (profile.Uptime.Contains("days", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new("Performance", "Uptime", HealthStatus.AttentionNeeded, $"Current uptime is {profile.Uptime}. A technician may recommend a restart if the client reports slowness or pending updates."));
        }
        else
        {
            findings.Add(new("Performance", "Uptime", HealthStatus.Good, $"Current uptime is {profile.Uptime}."));
        }

        return findings;
    }

    private static HealthStatus CalculateOverallStatus(IReadOnlyList<DiagnosticFinding> findings)
    {
        if (findings.Any(f => f.Status == HealthStatus.Critical)) return HealthStatus.Critical;
        if (findings.Any(f => f.Status == HealthStatus.AttentionNeeded)) return HealthStatus.AttentionNeeded;
        if (findings.Any(f => f.Status == HealthStatus.ReviewRequired)) return HealthStatus.ReviewRequired;
        return HealthStatus.Good;
    }

    private static string GetWindowsVersion()
    {
        var productName = ReadRegistryString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName");
        var displayVersion = ReadRegistryString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DisplayVersion");
        if (IsUnavailable(productName)) return Environment.OSVersion.VersionString;
        return IsUnavailable(displayVersion) ? productName : $"{productName} {displayVersion}";
    }

    private static string GetStorageSummary()
    {
        try
        {
            var driveSummaries = DriveInfo.GetDrives()
                .Where(drive => drive.IsReady && drive.DriveType == DriveType.Fixed)
                .Select(drive =>
                {
                    var freePercent = drive.TotalSize > 0 ? (double)drive.AvailableFreeSpace / drive.TotalSize : 0;
                    var status = freePercent < 0.10 ? "Low free space" : "OK";
                    return $"{drive.Name} {FormatBytes(drive.AvailableFreeSpace)} free of {FormatBytes(drive.TotalSize)} ({freePercent:P0}) - {status}";
                })
                .ToArray();

            return driveSummaries.Length > 0 ? string.Join("; ", driveSummaries) : "Unavailable";
        }
        catch (Exception ex)
        {
            return $"Unavailable ({ex.Message})";
        }
    }

    private static int SafeProcessCount()
    {
        try
        {
            return Process.GetProcesses().Length;
        }
        catch
        {
            return 0;
        }
    }

    private static string ReadRegistryString(string subKeyPath, string valueName)
    {
        try
        {
            using var subKey = Registry.LocalMachine.OpenSubKey(subKeyPath, writable: false);
            var value = subKey?.GetValue(valueName)?.ToString();
            return string.IsNullOrWhiteSpace(value) ? "Unavailable" : value.Trim();
        }
        catch
        {
            return "Unavailable";
        }
    }

    private static MemoryStatus GetMemoryStatus()
    {
        var status = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
        return GlobalMemoryStatusEx(ref status) ? status : default;
    }

    private static bool IsUnavailable(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Equals("Unavailable", StringComparison.OrdinalIgnoreCase);

    private static string FormatBytes(ulong bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unitIndex = 0;
        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.##} {units[unitIndex]}";
    }

    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1) return $"{(int)uptime.TotalDays} days, {uptime.Hours} hours";
        if (uptime.TotalHours >= 1) return $"{(int)uptime.TotalHours} hours, {uptime.Minutes} minutes";
        return $"{uptime.Minutes} minutes";
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus buffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatus
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysicalBytes;
        public ulong AvailablePhysicalBytes;
        public ulong TotalPageFileBytes;
        public ulong AvailablePageFileBytes;
        public ulong TotalVirtualBytes;
        public ulong AvailableVirtualBytes;
        public ulong AvailableExtendedVirtualBytes;
    }
}
