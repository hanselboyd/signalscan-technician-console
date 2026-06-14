using SignalScan.TechnicianConsole.Models;

namespace SignalScan.TechnicianConsole.Services;

public sealed class ReadOnlyDiagnosticCollector
{
    private readonly SystemProfileCollector _systemProfileCollector = new();
    private readonly PerformanceCollector _performanceCollector = new();
    private readonly MaintenanceCollector _maintenanceCollector = new();
    private readonly SecurityCollector _securityCollector = new();

    public Task<ScanResult> CollectAsync()
    {
        return Task.Run(() =>
        {
            var profile = _systemProfileCollector.Collect();
            var performance = _performanceCollector.Collect(profile);
            var maintenance = _maintenanceCollector.Collect();
            var security = _securityCollector.Collect(profile);
            var findings = CollectFindings(profile, performance.Findings, maintenance.Findings, security.Findings);
            return new ScanResult
            {
                ScanTimestamp = DateTimeOffset.Now,
                SystemProfile = profile,
                PerformanceSnapshot = performance.Snapshot,
                MaintenanceSnapshot = maintenance.Snapshot,
                SecuritySnapshot = security.Snapshot,
                Findings = findings,
                OverallStatus = CalculateOverallStatus(findings)
            };
        });
    }

    private static IReadOnlyList<DiagnosticFinding> CollectFindings(
        SystemProfile profile,
        IReadOnlyList<DiagnosticFinding> performanceFindings,
        IReadOnlyList<DiagnosticFinding> maintenanceFindings,
        IReadOnlyList<DiagnosticFinding> securityFindings)
    {
        var findings = new List<DiagnosticFinding>
        {
            new("System Profile", "Computer identity", HealthStatus.Good, $"Computer name: {profile.ComputerName}. Current user: {profile.CurrentUser}."),
            new("System Profile", "Windows edition/version/build", DiagnosticValueFormatter.IsUnavailable(profile.WindowsEdition) ? HealthStatus.ReviewRequired : HealthStatus.Good, $"{profile.WindowsEdition} {profile.WindowsDisplayVersion} build {profile.WindowsBuild}."),
            new("System Profile", "Processor", DiagnosticValueFormatter.IsUnavailable(profile.CpuModel) ? HealthStatus.ReviewRequired : HealthStatus.Good, profile.CpuModel),
            new("System Profile", "Installed memory", DiagnosticValueFormatter.IsUnavailable(profile.Ram) ? HealthStatus.ReviewRequired : HealthStatus.Good, profile.Ram),
            new("System Profile", "Fixed-drive storage", DiagnosticValueFormatter.IsUnavailable(profile.StorageSummary) ? HealthStatus.ReviewRequired : HealthStatus.Good, profile.StorageSummary),
            new("System Profile", "Manufacturer and model", DiagnosticValueFormatter.IsUnavailable(profile.Manufacturer) && DiagnosticValueFormatter.IsUnavailable(profile.Model) ? HealthStatus.ReviewRequired : HealthStatus.Good, $"{profile.Manufacturer} {profile.Model}".Trim()),
            new("System Profile", "BIOS/firmware version", DiagnosticValueFormatter.IsUnavailable(profile.BiosVersion) ? HealthStatus.ReviewRequired : HealthStatus.Good, profile.BiosVersion)
        };

        findings.AddRange(performanceFindings);
        findings.AddRange(maintenanceFindings);
        findings.AddRange(securityFindings);

        return findings;
    }

    private static HealthStatus CalculateOverallStatus(IReadOnlyList<DiagnosticFinding> findings)
    {
        if (findings.Any(f => f.Status == HealthStatus.Critical)) return HealthStatus.Critical;
        if (findings.Any(f => f.Status == HealthStatus.AttentionNeeded)) return HealthStatus.AttentionNeeded;
        if (findings.Any(f => f.Status == HealthStatus.ReviewRequired)) return HealthStatus.ReviewRequired;
        return HealthStatus.Good;
    }
}
