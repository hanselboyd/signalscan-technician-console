namespace SignalScan.TechnicianConsole.Models;

public sealed record MaintenanceScan(
    MaintenanceSnapshot Snapshot,
    IReadOnlyList<DiagnosticFinding> Findings);
