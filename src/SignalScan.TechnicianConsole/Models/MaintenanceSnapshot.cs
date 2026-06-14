namespace SignalScan.TechnicianConsole.Models;

public sealed record MaintenanceSnapshot(
    string PendingReboot,
    string WindowsUpdateStatus,
    string WindowsUpdateStatusDate,
    string LastSuccessfulWindowsUpdateDate,
    string EventLogSummary,
    string DiskHealthStatus);
