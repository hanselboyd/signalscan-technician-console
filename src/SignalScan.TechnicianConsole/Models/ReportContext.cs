namespace SignalScan.TechnicianConsole.Models;

public sealed record ReportContext(
    string ClientName,
    string ClientContact,
    string DeviceLabel,
    string TechnicianNotes,
    string RecommendedService);
