namespace SignalScan.TechnicianConsole.Models;

public sealed record FixedDriveProfile(
    string Name,
    string Format,
    string Capacity,
    string FreeSpace,
    string FreePercent);
