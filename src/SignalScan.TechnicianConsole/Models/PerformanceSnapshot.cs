namespace SignalScan.TechnicianConsole.Models;

public sealed record PerformanceSnapshot(
    string CpuUsage,
    string RamUsage,
    string RamAvailable,
    string DiskFreeEvaluation,
    string StartupAppCount,
    string ProcessCount,
    string UptimeIndicator);
