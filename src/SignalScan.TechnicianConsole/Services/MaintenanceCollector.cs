using System.Diagnostics.Eventing.Reader;
using System.Management;
using Microsoft.Win32;
using SignalScan.TechnicianConsole.Models;

namespace SignalScan.TechnicianConsole.Services;

public sealed class MaintenanceCollector
{
    private const string WindowsUpdateResultsPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\Results";

    public MaintenanceScan Collect()
    {
        var pendingReboot = GetPendingRebootStatus();
        var updateInfo = GetWindowsUpdateInfo();
        var eventLogSummary = GetEventLogWarningErrorSummary();
        var diskHealth = GetDiskHealthStatus();

        var snapshot = new MaintenanceSnapshot(
            pendingReboot.DisplayValue,
            updateInfo.Status,
            updateInfo.StatusDate,
            updateInfo.LastSuccessfulInstall,
            eventLogSummary.DisplayValue,
            diskHealth.DisplayValue);

        var findings = new List<DiagnosticFinding>
        {
            new("Maintenance", "Pending reboot", pendingReboot.Status, pendingReboot.Details),
            new("Maintenance", "Windows Update status", updateInfo.FindingStatus, updateInfo.Details),
            new("Maintenance", "Event log warning/error summary", eventLogSummary.Status, eventLogSummary.Details),
            new("Maintenance", "Disk health status", diskHealth.Status, diskHealth.Details)
        };

        return new MaintenanceScan(snapshot, findings);
    }

    private static MaintenanceCheckResult GetPendingRebootStatus()
    {
        try
        {
            var rebootKeys = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired",
                @"SYSTEM\CurrentControlSet\Control\Session Manager\PendingFileRenameOperations"
            };

            var detected = false;
            foreach (var path in rebootKeys)
            {
                if (path.EndsWith(@"\PendingFileRenameOperations", StringComparison.OrdinalIgnoreCase))
                {
                    detected |= RegistryValueExists(
                        @"SYSTEM\CurrentControlSet\Control\Session Manager",
                        "PendingFileRenameOperations");
                }
                else
                {
                    detected |= RegistryKeyExists(path);
                }
            }

            return detected
                ? new MaintenanceCheckResult("Yes", HealthStatus.AttentionNeeded, "A read-only reboot-pending indicator was found. Recommend confirming with the client before scheduling updates or maintenance.")
                : new MaintenanceCheckResult("No", HealthStatus.Good, "No common read-only pending reboot indicators were found.");
        }
        catch (Exception ex)
        {
            return Unavailable("Pending reboot status unavailable.", ex);
        }
    }

    private static WindowsUpdateInfo GetWindowsUpdateInfo()
    {
        try
        {
            var detectSuccess = ReadRegistryString(@$"{WindowsUpdateResultsPath}\Detect", "LastSuccessTime");
            var detectError = ReadRegistryString(@$"{WindowsUpdateResultsPath}\Detect", "LastError");
            var installSuccess = ReadRegistryString(@$"{WindowsUpdateResultsPath}\Install", "LastSuccessTime");
            var installError = ReadRegistryString(@$"{WindowsUpdateResultsPath}\Install", "LastError");

            var statusDate = FormatDateOrUnavailable(detectSuccess);
            var installDate = FormatDateOrUnavailable(installSuccess);

            if (IsErrorCodePresent(detectError) || IsErrorCodePresent(installError))
            {
                return new WindowsUpdateInfo(
                    "Review recommended",
                    statusDate,
                    installDate,
                    HealthStatus.AttentionNeeded,
                    $"Windows Update registry history reports a recent error code. Detect error: {ValueOrUnavailable(detectError)}. Install error: {ValueOrUnavailable(installError)}. No update scan or install was performed.");
            }

            if (DiagnosticValueFormatter.IsUnavailable(statusDate) && DiagnosticValueFormatter.IsUnavailable(installDate))
            {
                return new WindowsUpdateInfo(
                    DiagnosticValueFormatter.Unavailable,
                    DiagnosticValueFormatter.Unavailable,
                    DiagnosticValueFormatter.Unavailable,
                    HealthStatus.ReviewRequired,
                    "Windows Update history timestamps were unavailable from read-only registry locations. No update scan or install was performed.");
            }

            return new WindowsUpdateInfo(
                "History available",
                statusDate,
                installDate,
                HealthStatus.Good,
                $"Windows Update read-only history was available. Last detect success: {statusDate}. Last install success: {installDate}.");
        }
        catch (Exception ex)
        {
            return new WindowsUpdateInfo(
                DiagnosticValueFormatter.Unavailable,
                DiagnosticValueFormatter.Unavailable,
                DiagnosticValueFormatter.Unavailable,
                HealthStatus.ReviewRequired,
                $"Windows Update status unavailable. {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static MaintenanceCheckResult GetEventLogWarningErrorSummary()
    {
        try
        {
            var system = CountWarningAndErrorEvents("System");
            var application = CountWarningAndErrorEvents("Application");
            var totalErrors = system.Errors + application.Errors;
            var totalWarnings = system.Warnings + application.Warnings;
            var display = $"Last 7 days: {totalErrors} errors, {totalWarnings} warnings";
            var details = $"Read-only Event Log summary for System and Application logs over the last 7 days. System: {system.Errors} errors, {system.Warnings} warnings. Application: {application.Errors} errors, {application.Warnings} warnings. No logs were cleared.";

            var status = totalErrors switch
            {
                >= 50 => HealthStatus.Critical,
                > 0 => HealthStatus.AttentionNeeded,
                _ when totalWarnings > 0 => HealthStatus.AttentionNeeded,
                _ => HealthStatus.Good
            };

            return new MaintenanceCheckResult(display, status, details);
        }
        catch (Exception ex)
        {
            return Unavailable("Event log warning/error summary unavailable.", ex);
        }
    }

    private static EventLogCounts CountWarningAndErrorEvents(string logName)
    {
        const long sevenDaysMilliseconds = 7L * 24L * 60L * 60L * 1000L;
        var query = new EventLogQuery(
            logName,
            PathType.LogName,
            $"*[System[(Level=2 or Level=3) and TimeCreated[timediff(@SystemTime) <= {sevenDaysMilliseconds}]]]");

        var errors = 0;
        var warnings = 0;
        using var reader = new EventLogReader(query);
        for (var record = reader.ReadEvent(); record is not null; record = reader.ReadEvent())
        {
            using (record)
            {
                if (record.Level == 2)
                {
                    errors++;
                }
                else if (record.Level == 3)
                {
                    warnings++;
                }
            }
        }

        return new EventLogCounts(errors, warnings);
    }

    private static MaintenanceCheckResult GetDiskHealthStatus()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Model, Status FROM Win32_DiskDrive");
            using var results = searcher.Get();
            var statuses = new List<string>();
            foreach (ManagementObject disk in results)
            {
                using (disk)
                {
                    var model = ValueOrUnavailable(disk["Model"]?.ToString());
                    var status = ValueOrUnavailable(disk["Status"]?.ToString());
                    statuses.Add($"{model}: {status}");
                }
            }

            if (statuses.Count == 0)
            {
                return new MaintenanceCheckResult(
                    DiagnosticValueFormatter.Unavailable,
                    HealthStatus.ReviewRequired,
                    "Disk health status was unavailable from the read-only Win32_DiskDrive provider.");
            }

            var unhealthy = statuses.Any(value =>
                value.Contains("Pred Fail", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("Error", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("Degraded", StringComparison.OrdinalIgnoreCase));
            var review = statuses.Any(value => value.Contains(DiagnosticValueFormatter.Unavailable, StringComparison.OrdinalIgnoreCase));
            var statusLevel = unhealthy ? HealthStatus.Critical : review ? HealthStatus.ReviewRequired : HealthStatus.Good;
            var details = "Read-only disk status from Win32_DiskDrive. No disk repair, scan, cleanup, or SMART vendor test was started.";

            return new MaintenanceCheckResult(string.Join("; ", statuses), statusLevel, $"{details} Results: {string.Join("; ", statuses)}");
        }
        catch (Exception ex)
        {
            return Unavailable("Disk health status unavailable.", ex);
        }
    }

    private static bool RegistryKeyExists(string subKeyPath)
    {
        using var key = Registry.LocalMachine.OpenSubKey(subKeyPath, writable: false);
        return key is not null;
    }

    private static bool RegistryValueExists(string subKeyPath, string valueName)
    {
        using var key = Registry.LocalMachine.OpenSubKey(subKeyPath, writable: false);
        return key?.GetValue(valueName) is not null;
    }

    private static string ReadRegistryString(string subKeyPath, string valueName)
    {
        using var key = Registry.LocalMachine.OpenSubKey(subKeyPath, writable: false);
        return ValueOrUnavailable(key?.GetValue(valueName)?.ToString());
    }

    private static string FormatDateOrUnavailable(string value)
    {
        if (DiagnosticValueFormatter.IsUnavailable(value))
        {
            return DiagnosticValueFormatter.Unavailable;
        }

        return DateTimeOffset.TryParse(value, out var timestamp)
            ? timestamp.LocalDateTime.ToString("g")
            : value;
    }

    private static bool IsErrorCodePresent(string value)
    {
        if (DiagnosticValueFormatter.IsUnavailable(value))
        {
            return false;
        }

        return !string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(value, "0x0", StringComparison.OrdinalIgnoreCase);
    }

    private static string ValueOrUnavailable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DiagnosticValueFormatter.Unavailable : value.Trim();

    private static MaintenanceCheckResult Unavailable(string message, Exception ex) =>
        new(DiagnosticValueFormatter.Unavailable, HealthStatus.ReviewRequired, $"{message} {ex.GetType().Name}: {ex.Message}");

    private sealed record MaintenanceCheckResult(string DisplayValue, HealthStatus Status, string Details);

    private sealed record WindowsUpdateInfo(string Status, string StatusDate, string LastSuccessfulInstall, HealthStatus FindingStatus, string Details);

    private sealed record EventLogCounts(int Errors, int Warnings);
}
