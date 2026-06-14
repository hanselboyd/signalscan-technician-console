using SignalScan.TechnicianConsole.Models;

namespace SignalScan.TechnicianConsole.Services;

public static class PerformanceFindingEvaluator
{
    public static HealthStatus EvaluateCpu(double? cpuPercent)
    {
        if (cpuPercent is null) return HealthStatus.ReviewRequired;
        if (cpuPercent >= 95) return HealthStatus.Critical;
        if (cpuPercent >= 80) return HealthStatus.AttentionNeeded;
        return HealthStatus.Good;
    }

    public static HealthStatus EvaluateRam(double? ramUsedPercent)
    {
        if (ramUsedPercent is null) return HealthStatus.ReviewRequired;
        if (ramUsedPercent >= 95) return HealthStatus.Critical;
        if (ramUsedPercent >= 85) return HealthStatus.AttentionNeeded;
        return HealthStatus.Good;
    }

    public static HealthStatus EvaluateDiskFree(double? lowestFreePercent)
    {
        if (lowestFreePercent is null) return HealthStatus.ReviewRequired;
        if (lowestFreePercent < 5) return HealthStatus.Critical;
        if (lowestFreePercent < 15) return HealthStatus.AttentionNeeded;
        return HealthStatus.Good;
    }

    public static HealthStatus EvaluateStartupCount(int? startupAppCount)
    {
        if (startupAppCount is null) return HealthStatus.ReviewRequired;
        if (startupAppCount >= 20) return HealthStatus.AttentionNeeded;
        return HealthStatus.Good;
    }

    public static HealthStatus EvaluateProcessCount(int? processCount)
    {
        if (processCount is null) return HealthStatus.ReviewRequired;
        if (processCount >= 250) return HealthStatus.AttentionNeeded;
        return HealthStatus.Good;
    }

    public static HealthStatus EvaluateUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 30) return HealthStatus.AttentionNeeded;
        if (uptime.TotalDays >= 7) return HealthStatus.AttentionNeeded;
        return HealthStatus.Good;
    }

    public static double? ParsePercent(string value)
    {
        if (DiagnosticValueFormatter.IsUnavailable(value)) return null;
        var cleaned = value.Replace("%", string.Empty).Trim();
        return double.TryParse(cleaned, out var parsed) ? parsed : null;
    }
}
