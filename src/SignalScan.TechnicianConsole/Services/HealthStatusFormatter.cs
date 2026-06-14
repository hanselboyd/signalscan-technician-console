using SignalScan.TechnicianConsole.Models;

namespace SignalScan.TechnicianConsole.Services;

public static class HealthStatusFormatter
{
    public static string Format(HealthStatus status) =>
        status switch
        {
            HealthStatus.Good => "Good",
            HealthStatus.AttentionNeeded => "Attention Needed",
            HealthStatus.Critical => "Critical",
            _ => "Review Required"
        };
}
