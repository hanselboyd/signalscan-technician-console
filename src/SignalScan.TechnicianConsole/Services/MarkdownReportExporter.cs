using System.IO;
using System.Text;
using SignalScan.TechnicianConsole.Models;

namespace SignalScan.TechnicianConsole.Services;

public sealed class MarkdownReportExporter
{
    public async Task ExportAsync(string path, ScanResult scanResult, ReportContext context)
    {
        var markdown = BuildReport(scanResult, context);
        await File.WriteAllTextAsync(path, markdown, Encoding.UTF8);
    }

    private static string BuildReport(ScanResult scanResult, ReportContext context)
    {
        var profile = scanResult.SystemProfile;
        var builder = new StringBuilder();
        builder.AppendLine("# SignalScan PC Health Report");
        builder.AppendLine();
        builder.AppendLine("Prepared by **909 Signal IT**");
        builder.AppendLine();
        builder.AppendLine("## Client Information");
        builder.AppendLine();
        builder.AppendLine($"Client Name: {ValueOrBlank(context.ClientName)}");
        builder.AppendLine($"Phone/Email: {ValueOrBlank(context.ClientContact)}");
        builder.AppendLine($"Device Label: {ValueOrBlank(context.DeviceLabel)}");
        builder.AppendLine($"Device Name: {profile.ComputerName}");
        builder.AppendLine($"Scan Date: {scanResult.ScanTimestamp.LocalDateTime:g}");
        builder.AppendLine();
        builder.AppendLine("## Overall Status");
        builder.AppendLine();
        builder.AppendLine($"Status: {HealthStatusFormatter.Format(scanResult.OverallStatus)}");
        builder.AppendLine();
        builder.AppendLine("## System Profile");
        builder.AppendLine();
        builder.AppendLine($"- Computer Name: {profile.ComputerName}");
        builder.AppendLine($"- Windows Version: {profile.WindowsVersion}");
        builder.AppendLine($"- Windows Build: {profile.WindowsBuild}");
        builder.AppendLine($"- CPU: {profile.CpuModel}");
        builder.AppendLine($"- RAM: {profile.Ram}");
        builder.AppendLine($"- Storage: {profile.StorageSummary}");
        builder.AppendLine($"- Manufacturer/Model: {profile.Manufacturer} {profile.Model}".TrimEnd());
        builder.AppendLine($"- BIOS/Firmware: {profile.BiosVersion}");
        builder.AppendLine($"- Uptime: {profile.Uptime}");
        builder.AppendLine();

        AppendFindings(builder, "Performance Findings", scanResult, "Performance");
        AppendFindings(builder, "Maintenance Findings", scanResult, "Maintenance");
        AppendFindings(builder, "Security Posture", scanResult, "Security");
        AppendFindings(builder, "System Profile Findings", scanResult, "System Profile");

        builder.AppendLine("## Technician Notes");
        builder.AppendLine();
        builder.AppendLine(string.IsNullOrWhiteSpace(context.TechnicianNotes) ? "No technician notes entered." : context.TechnicianNotes.Trim());
        builder.AppendLine();
        builder.AppendLine("## Recommended Service");
        builder.AppendLine();
        builder.AppendLine(ValueOrBlank(context.RecommendedService));
        builder.AppendLine();
        builder.AppendLine("## 909 Signal IT Contact");
        builder.AppendLine();
        builder.AppendLine("909 Signal IT");
        builder.AppendLine("Local IT Support for Residential and Small Business Clients");
        builder.AppendLine("Website: https://909signalit.com");
        builder.AppendLine("Phone: 909-260-8660");
        builder.AppendLine("Email: support@909signalit.com");
        builder.AppendLine();
        builder.AppendLine("## Disclaimer");
        builder.AppendLine();
        builder.AppendLine("SignalScan provides a technician-reviewed system health assessment based on available diagnostic information at the time of scan. Findings are intended to guide maintenance and support decisions. Some issues may require additional hands-on inspection, hardware testing, vendor tools, or follow-up service.");
        builder.AppendLine();
        builder.AppendLine("Safety note: SignalScan v1 is read-only. This report was generated without deleting files, changing registry keys, disabling services, changing drivers, removing malware, or performing automatic repairs.");
        return builder.ToString();
    }

    private static void AppendFindings(StringBuilder builder, string heading, ScanResult scanResult, string category)
    {
        builder.AppendLine($"## {heading}");
        builder.AppendLine();
        builder.AppendLine("| Finding | Status | Explanation |");
        builder.AppendLine("|---|---|---|");

        var rows = scanResult.Findings.Where(f => f.Category == category).ToArray();
        if (rows.Length == 0)
        {
            builder.AppendLine("| No findings collected | Review Required | This area is scheduled for a later SignalScan task. |");
        }
        else
        {
            foreach (var finding in rows)
            {
                builder.AppendLine($"| {EscapeTable(finding.Name)} | {HealthStatusFormatter.Format(finding.Status)} | {EscapeTable(finding.Details)} |");
            }
        }

        builder.AppendLine();
    }

    private static string ValueOrBlank(string value) =>
        string.IsNullOrWhiteSpace(value) ? "Not provided" : value.Trim();

    private static string EscapeTable(string value) =>
        value.Replace("|", "\\|").ReplaceLineEndings(" ");
}
