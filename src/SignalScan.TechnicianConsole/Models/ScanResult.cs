namespace SignalScan.TechnicianConsole.Models;

public sealed class ScanResult
{
    public DateTimeOffset ScanTimestamp { get; init; } = DateTimeOffset.Now;
    public required SystemProfile SystemProfile { get; init; }
    public required PerformanceSnapshot PerformanceSnapshot { get; init; }
    public required IReadOnlyList<DiagnosticFinding> Findings { get; init; }
    public HealthStatus OverallStatus { get; init; } = HealthStatus.ReviewRequired;
}
