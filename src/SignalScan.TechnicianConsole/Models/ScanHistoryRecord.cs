namespace SignalScan.TechnicianConsole.Models;

public sealed record ScanHistoryRecord(
    string Id,
    DateTimeOffset ScanTimestamp,
    string ClientName,
    string DeviceName,
    string OverallStatus,
    string RecommendedService,
    string ExportedPdfPath,
    string ExportedMarkdownPath);
