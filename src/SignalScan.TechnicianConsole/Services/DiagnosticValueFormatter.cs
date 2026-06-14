namespace SignalScan.TechnicianConsole.Services;

public static class DiagnosticValueFormatter
{
    public const string Unavailable = "Unavailable";

    public static string NormalizeUnavailable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? Unavailable : value.Trim();

    public static bool IsUnavailable(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Equals(Unavailable, StringComparison.OrdinalIgnoreCase);

    public static string FormatBytes(ulong bytes)
    {
        if (bytes == 0) return "0 B";

        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double value = bytes;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.##} {units[unitIndex]}";
    }

    public static string FormatFreePercent(ulong totalBytes, ulong freeBytes)
    {
        if (totalBytes == 0) return Unavailable;
        var freePercent = (double)freeBytes / totalBytes;
        return $"{freePercent:P0}";
    }

    public static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1) return $"{(int)uptime.TotalDays} days, {uptime.Hours} hours";
        if (uptime.TotalHours >= 1) return $"{(int)uptime.TotalHours} hours, {uptime.Minutes} minutes";
        return $"{uptime.Minutes} minutes";
    }
}
