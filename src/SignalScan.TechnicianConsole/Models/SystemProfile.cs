namespace SignalScan.TechnicianConsole.Models;

public sealed record SystemProfile(
    string ComputerName,
    string WindowsEdition,
    string WindowsDisplayVersion,
    string WindowsBuild,
    string CpuModel,
    string Ram,
    IReadOnlyList<FixedDriveProfile> FixedDrives,
    string StorageSummary,
    string Manufacturer,
    string Model,
    string BiosVersion,
    string Uptime,
    string CurrentUser);
