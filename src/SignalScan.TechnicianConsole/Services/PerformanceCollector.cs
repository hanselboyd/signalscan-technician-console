using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using SignalScan.TechnicianConsole.Models;

namespace SignalScan.TechnicianConsole.Services;

public sealed class PerformanceCollector
{
    private static readonly string[] StartupSubKeyPaths =
    {
        @"Software\Microsoft\Windows\CurrentVersion\Run",
        @"Software\Microsoft\Windows\CurrentVersion\RunOnce"
    };

    public PerformanceScan Collect(SystemProfile profile)
    {
        var cpuPercent = TryGetCpuUsageSnapshot();
        var memory = GetMemoryStatus();
        var ramUsedPercent = CalculateRamUsedPercent(memory);
        var startupAppCount = CountStartupApps();
        var processCount = SafeProcessCount();
        var lowestDiskFreePercent = GetLowestFixedDriveFreePercent(profile.FixedDrives);
        var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);

        var snapshot = new PerformanceSnapshot(
            CpuUsage: FormatPercent(cpuPercent),
            RamUsage: FormatPercent(ramUsedPercent),
            RamAvailable: memory.AvailablePhysicalBytes > 0 ? DiagnosticValueFormatter.FormatBytes(memory.AvailablePhysicalBytes) : DiagnosticValueFormatter.Unavailable,
            DiskFreeEvaluation: FormatDiskFreeEvaluation(lowestDiskFreePercent),
            StartupAppCount: startupAppCount?.ToString() ?? DiagnosticValueFormatter.Unavailable,
            ProcessCount: processCount?.ToString() ?? DiagnosticValueFormatter.Unavailable,
            UptimeIndicator: DiagnosticValueFormatter.FormatUptime(uptime));

        var findings = new List<DiagnosticFinding>
        {
            new("Performance", "CPU usage snapshot", PerformanceFindingEvaluator.EvaluateCpu(cpuPercent), cpuPercent is null ? "CPU usage snapshot is unavailable." : $"Approximate CPU usage at scan time: {cpuPercent:0}%."),
            new("Performance", "RAM usage snapshot", PerformanceFindingEvaluator.EvaluateRam(ramUsedPercent), ramUsedPercent is null ? "RAM usage snapshot is unavailable." : $"Approximate RAM usage at scan time: {ramUsedPercent:0}%. Available RAM: {snapshot.RamAvailable}."),
            new("Performance", "Disk free percentage", PerformanceFindingEvaluator.EvaluateDiskFree(lowestDiskFreePercent), snapshot.DiskFreeEvaluation),
            new("Performance", "Startup app count", PerformanceFindingEvaluator.EvaluateStartupCount(startupAppCount), startupAppCount is null ? "Startup app count is unavailable." : $"{startupAppCount} startup entries were visible in read-only registry locations."),
            new("Performance", "Visible running process count", PerformanceFindingEvaluator.EvaluateProcessCount(processCount), processCount is null ? "Process count is unavailable." : $"{processCount} processes were visible to the current user."),
            new("Performance", "Uptime/reboot attention indicator", PerformanceFindingEvaluator.EvaluateUptime(uptime), BuildUptimeDetails(uptime))
        };

        return new PerformanceScan(snapshot, findings);
    }

    private static double? TryGetCpuUsageSnapshot()
    {
        try
        {
            if (!GetSystemTimes(out var idleStart, out var kernelStart, out var userStart)) return null;
            Thread.Sleep(500);
            if (!GetSystemTimes(out var idleEnd, out var kernelEnd, out var userEnd)) return null;

            var idle = FileTimeToUInt64(idleEnd) - FileTimeToUInt64(idleStart);
            var kernel = FileTimeToUInt64(kernelEnd) - FileTimeToUInt64(kernelStart);
            var user = FileTimeToUInt64(userEnd) - FileTimeToUInt64(userStart);
            var total = kernel + user;
            if (total == 0) return null;

            var busy = total > idle ? total - idle : 0;
            return Math.Clamp((double)busy * 100 / total, 0, 100);
        }
        catch
        {
            return null;
        }
    }

    private static int? CountStartupApps()
    {
        try
        {
            var count = 0;
            count += CountStartupValues(Registry.CurrentUser);
            count += CountStartupValues(Registry.LocalMachine);
            return count;
        }
        catch
        {
            return null;
        }
    }

    private static int CountStartupValues(RegistryKey rootKey)
    {
        var count = 0;
        foreach (var path in StartupSubKeyPaths)
        {
            try
            {
                using var subKey = rootKey.OpenSubKey(path, writable: false);
                count += subKey?.GetValueNames().Length ?? 0;
            }
            catch
            {
                // Some registry views/locations may be unavailable without elevation.
            }
        }

        return count;
    }

    private static int? SafeProcessCount()
    {
        try
        {
            return Process.GetProcesses().Length;
        }
        catch
        {
            return null;
        }
    }

    private static double? CalculateRamUsedPercent(MemoryStatus memory)
    {
        if (memory.TotalPhysicalBytes == 0) return null;
        var used = memory.TotalPhysicalBytes - memory.AvailablePhysicalBytes;
        return Math.Clamp((double)used * 100 / memory.TotalPhysicalBytes, 0, 100);
    }

    private static double? GetLowestFixedDriveFreePercent(IReadOnlyList<FixedDriveProfile> fixedDrives)
    {
        var percentages = fixedDrives
            .Select(drive => PerformanceFindingEvaluator.ParsePercent(drive.FreePercent))
            .Where(percent => percent.HasValue)
            .Select(percent => percent!.Value)
            .ToArray();

        return percentages.Length == 0 ? null : percentages.Min();
    }

    private static string FormatDiskFreeEvaluation(double? lowestDiskFreePercent)
    {
        if (lowestDiskFreePercent is null) return "Fixed-drive free percentage is unavailable.";
        return $"Lowest fixed-drive free space: {lowestDiskFreePercent:0}%.";
    }

    private static string BuildUptimeDetails(TimeSpan uptime)
    {
        var formatted = DiagnosticValueFormatter.FormatUptime(uptime);
        if (uptime.TotalDays >= 7)
        {
            return $"Current uptime is {formatted}. A technician may recommend a restart if the client reports slowness or pending updates.";
        }

        return $"Current uptime is {formatted}.";
    }

    private static string FormatPercent(double? value) =>
        value is null ? DiagnosticValueFormatter.Unavailable : $"{value:0}%";

    private static MemoryStatus GetMemoryStatus()
    {
        var status = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
        return GlobalMemoryStatusEx(ref status) ? status : default;
    }

    private static ulong FileTimeToUInt64(FileTime fileTime) =>
        ((ulong)fileTime.HighDateTime << 32) | fileTime.LowDateTime;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus buffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

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

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint LowDateTime;
        public uint HighDateTime;
    }
}

public sealed record PerformanceScan(
    PerformanceSnapshot Snapshot,
    IReadOnlyList<DiagnosticFinding> Findings);
