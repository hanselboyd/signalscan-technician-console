using System.Diagnostics;
using SignalScan.TechnicianConsole.Models;

namespace SignalScan.TechnicianConsole.Services;

public sealed class ReadOnlyDiagnosticCollector
{
    private readonly SystemProfileCollector _systemProfileCollector = new();

    public Task<ScanResult> CollectAsync()
    {
        return Task.Run(() =>
        {
            var profile = _systemProfileCollector.Collect();
            var findings = CollectFindings(profile);
            return new ScanResult
            {
                ScanTimestamp = DateTimeOffset.Now,
                SystemProfile = profile,
                Findings = findings,
                OverallStatus = CalculateOverallStatus(findings)
            };
        });
    }

    private static IReadOnlyList<DiagnosticFinding> CollectFindings(SystemProfile profile)
    {
        var findings = new List<DiagnosticFinding>
        {
            new("System Profile", "Computer identity", HealthStatus.Good, $"Computer name: {profile.ComputerName}. Current user: {profile.CurrentUser}."),
            new("System Profile", "Windows edition/version/build", DiagnosticValueFormatter.IsUnavailable(profile.WindowsEdition) ? HealthStatus.ReviewRequired : HealthStatus.Good, $"{profile.WindowsEdition} {profile.WindowsDisplayVersion} build {profile.WindowsBuild}."),
            new("System Profile", "Processor", DiagnosticValueFormatter.IsUnavailable(profile.CpuModel) ? HealthStatus.ReviewRequired : HealthStatus.Good, profile.CpuModel),
            new("System Profile", "Installed memory", DiagnosticValueFormatter.IsUnavailable(profile.Ram) ? HealthStatus.ReviewRequired : HealthStatus.Good, profile.Ram),
            new("System Profile", "Fixed-drive storage", DiagnosticValueFormatter.IsUnavailable(profile.StorageSummary) ? HealthStatus.ReviewRequired : HealthStatus.Good, profile.StorageSummary),
            new("System Profile", "Manufacturer and model", DiagnosticValueFormatter.IsUnavailable(profile.Manufacturer) && DiagnosticValueFormatter.IsUnavailable(profile.Model) ? HealthStatus.ReviewRequired : HealthStatus.Good, $"{profile.Manufacturer} {profile.Model}".Trim()),
            new("System Profile", "BIOS/firmware version", DiagnosticValueFormatter.IsUnavailable(profile.BiosVersion) ? HealthStatus.ReviewRequired : HealthStatus.Good, profile.BiosVersion),
            new("Performance", "Running process count", HealthStatus.Good, $"{SafeProcessCount()} processes were visible to the current user."),
            new("Maintenance", "Maintenance checks", HealthStatus.ReviewRequired, "Detailed update, event log, and disk health checks are planned for later tasks. No maintenance actions were performed."),
            new("Security", "Security posture checks", HealthStatus.ReviewRequired, "Defender, firewall, BitLocker, and account checks are planned for later tasks. No security settings were changed.")
        };

        if (profile.Uptime.Contains("days", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new("Performance", "Uptime", HealthStatus.AttentionNeeded, $"Current uptime is {profile.Uptime}. A technician may recommend a restart if the client reports slowness or pending updates."));
        }
        else
        {
            findings.Add(new("Performance", "Uptime", HealthStatus.Good, $"Current uptime is {profile.Uptime}."));
        }

        return findings;
    }

    private static HealthStatus CalculateOverallStatus(IReadOnlyList<DiagnosticFinding> findings)
    {
        if (findings.Any(f => f.Status == HealthStatus.Critical)) return HealthStatus.Critical;
        if (findings.Any(f => f.Status == HealthStatus.AttentionNeeded)) return HealthStatus.AttentionNeeded;
        if (findings.Any(f => f.Status == HealthStatus.ReviewRequired)) return HealthStatus.ReviewRequired;
        return HealthStatus.Good;
    }

    private static int SafeProcessCount()
    {
        try
        {
            return Process.GetProcesses().Length;
        }
        catch
        {
            return 0;
        }
    }
}
