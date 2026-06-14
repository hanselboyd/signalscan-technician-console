namespace SignalScan.TechnicianConsole.Models;

public sealed record SecurityScan(
    SecuritySnapshot Snapshot,
    IReadOnlyList<DiagnosticFinding> Findings);
