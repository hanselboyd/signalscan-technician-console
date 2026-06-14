using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using SignalScan.TechnicianConsole.Models;

namespace SignalScan.TechnicianConsole.Services;

public sealed class SystemProfileCollector
{
    public SystemProfile Collect()
    {
        var memory = GetMemoryStatus();
        var fixedDrives = GetFixedDrives();
        return new SystemProfile(
            ComputerName: DiagnosticValueFormatter.NormalizeUnavailable(Environment.MachineName),
            WindowsEdition: GetWindowsEdition(),
            WindowsDisplayVersion: GetWindowsDisplayVersion(),
            WindowsBuild: GetWindowsBuild(),
            CpuModel: ReadRegistryString(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString"),
            Ram: memory.TotalPhysicalBytes > 0 ? DiagnosticValueFormatter.FormatBytes(memory.TotalPhysicalBytes) : DiagnosticValueFormatter.Unavailable,
            FixedDrives: fixedDrives,
            StorageSummary: BuildStorageSummary(fixedDrives),
            Manufacturer: ReadRegistryString(@"HARDWARE\DESCRIPTION\System\BIOS", "SystemManufacturer"),
            Model: ReadRegistryString(@"HARDWARE\DESCRIPTION\System\BIOS", "SystemProductName"),
            BiosVersion: ReadRegistryString(@"HARDWARE\DESCRIPTION\System\BIOS", "BIOSVersion"),
            Uptime: DiagnosticValueFormatter.FormatUptime(TimeSpan.FromMilliseconds(Environment.TickCount64)),
            CurrentUser: DiagnosticValueFormatter.NormalizeUnavailable(Environment.UserName));
    }

    private static string GetWindowsEdition()
    {
        var productName = ReadRegistryString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName");
        return DiagnosticValueFormatter.IsUnavailable(productName)
            ? DiagnosticValueFormatter.NormalizeUnavailable(Environment.OSVersion.VersionString)
            : productName;
    }

    private static string GetWindowsDisplayVersion()
    {
        var displayVersion = ReadRegistryString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DisplayVersion");
        if (!DiagnosticValueFormatter.IsUnavailable(displayVersion)) return displayVersion;

        var releaseId = ReadRegistryString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId");
        return releaseId;
    }

    private static string GetWindowsBuild()
    {
        var build = ReadRegistryString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuildNumber");
        var ubr = ReadRegistryString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "UBR");
        if (DiagnosticValueFormatter.IsUnavailable(build)) return build;
        return DiagnosticValueFormatter.IsUnavailable(ubr) ? build : $"{build}.{ubr}";
    }

    private static IReadOnlyList<FixedDriveProfile> GetFixedDrives()
    {
        try
        {
            var drives = DriveInfo.GetDrives()
                .Where(drive => drive.DriveType == DriveType.Fixed)
                .Select(CreateDriveProfile)
                .ToArray();

            return drives.Length > 0 ? drives : new[] { UnavailableDriveProfile() };
        }
        catch
        {
            return new[] { UnavailableDriveProfile() };
        }
    }

    private static FixedDriveProfile CreateDriveProfile(DriveInfo drive)
    {
        try
        {
            if (!drive.IsReady)
            {
                return new FixedDriveProfile(
                    Name: DiagnosticValueFormatter.NormalizeUnavailable(drive.Name),
                    Format: DiagnosticValueFormatter.Unavailable,
                    Capacity: DiagnosticValueFormatter.Unavailable,
                    FreeSpace: DiagnosticValueFormatter.Unavailable,
                    FreePercent: DiagnosticValueFormatter.Unavailable);
            }

            return new FixedDriveProfile(
                Name: DiagnosticValueFormatter.NormalizeUnavailable(drive.Name),
                Format: DiagnosticValueFormatter.NormalizeUnavailable(drive.DriveFormat),
                Capacity: DiagnosticValueFormatter.FormatBytes((ulong)drive.TotalSize),
                FreeSpace: DiagnosticValueFormatter.FormatBytes((ulong)drive.AvailableFreeSpace),
                FreePercent: DiagnosticValueFormatter.FormatFreePercent((ulong)drive.TotalSize, (ulong)drive.AvailableFreeSpace));
        }
        catch
        {
            return new FixedDriveProfile(
                Name: DiagnosticValueFormatter.NormalizeUnavailable(drive.Name),
                Format: DiagnosticValueFormatter.Unavailable,
                Capacity: DiagnosticValueFormatter.Unavailable,
                FreeSpace: DiagnosticValueFormatter.Unavailable,
                FreePercent: DiagnosticValueFormatter.Unavailable);
        }
    }

    private static FixedDriveProfile UnavailableDriveProfile() =>
        new(
            Name: DiagnosticValueFormatter.Unavailable,
            Format: DiagnosticValueFormatter.Unavailable,
            Capacity: DiagnosticValueFormatter.Unavailable,
            FreeSpace: DiagnosticValueFormatter.Unavailable,
            FreePercent: DiagnosticValueFormatter.Unavailable);

    private static string BuildStorageSummary(IReadOnlyList<FixedDriveProfile> fixedDrives)
    {
        if (fixedDrives.Count == 0) return DiagnosticValueFormatter.Unavailable;
        if (fixedDrives.All(drive => DiagnosticValueFormatter.IsUnavailable(drive.Name))) return DiagnosticValueFormatter.Unavailable;

        return string.Join("; ", fixedDrives.Select(drive =>
            $"{drive.Name} {drive.FreeSpace} free of {drive.Capacity} ({drive.FreePercent})"));
    }

    private static string ReadRegistryString(string subKeyPath, string valueName)
    {
        try
        {
            using var subKey = Registry.LocalMachine.OpenSubKey(subKeyPath, writable: false);
            var value = subKey?.GetValue(valueName);
            return RegistryValueToString(value);
        }
        catch
        {
            return DiagnosticValueFormatter.Unavailable;
        }
    }

    private static string RegistryValueToString(object? value)
    {
        return value switch
        {
            null => DiagnosticValueFormatter.Unavailable,
            string[] values => DiagnosticValueFormatter.NormalizeUnavailable(string.Join(", ", values.Where(v => !string.IsNullOrWhiteSpace(v)))),
            _ => DiagnosticValueFormatter.NormalizeUnavailable(value.ToString())
        };
    }

    private static MemoryStatus GetMemoryStatus()
    {
        var status = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
        return GlobalMemoryStatusEx(ref status) ? status : default;
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
