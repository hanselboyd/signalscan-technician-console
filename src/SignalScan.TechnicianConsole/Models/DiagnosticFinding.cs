namespace SignalScan.TechnicianConsole.Models;

public sealed record DiagnosticFinding(
    string Category,
    string Name,
    HealthStatus Status,
    string Details);
