namespace SignalScan.TechnicianConsole.Models;

public sealed record SystemProfile(
    string ComputerName,
    string WindowsVersion,
    string WindowsBuild,
    string CpuModel,
    string Ram,
    string StorageSummary,
    string Manufacturer,
    string Model,
    string BiosVersion,
    string Uptime,
    string CurrentUser);
