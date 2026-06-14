using System.Text;
using SignalScan.TechnicianConsole.Models;

namespace SignalScan.TechnicianConsole.Services;

public sealed class OfflineAiSummaryProvider : IAiSummaryProvider
{
    public AiSummaryDraft GenerateDraft(ScanResult scanResult)
    {
        var status = HealthStatusFormatter.Format(scanResult.OverallStatus);
        var priorityFindings = scanResult.Findings
            .OrderBy(finding => RankStatus(finding.Status))
            .ThenBy(finding => finding.Category)
            .ThenBy(finding => finding.Name)
            .Take(5)
            .ToArray();

        var summary = new StringBuilder();
        summary.Append($"Offline draft summary: SignalScan completed a read-only diagnostic review with an overall status of {status}. ");

        if (priorityFindings.Length == 0)
        {
            summary.Append("No findings were available for summary. Technician review is required before sharing this report.");
        }
        else
        {
            summary.Append("Key items for technician review include ");
            summary.Append(string.Join("; ", priorityFindings.Select(FormatFinding)));
            summary.Append(". This draft was generated locally and must be reviewed by the technician before export.");
        }

        return new AiSummaryDraft(
            summary.ToString(),
            BuildNextStep(scanResult.OverallStatus, priorityFindings));
    }

    private static string FormatFinding(DiagnosticFinding finding) =>
        $"{finding.Category} - {finding.Name}: {HealthStatusFormatter.Format(finding.Status)}";

    private static string BuildNextStep(HealthStatus status, IReadOnlyList<DiagnosticFinding> priorityFindings)
    {
        var action = status switch
        {
            HealthStatus.Good => "routine maintenance or monitoring",
            HealthStatus.AttentionNeeded => "a tune-up or technician review",
            HealthStatus.Critical => "urgent technician review",
            _ => "manual technician review"
        };

        if (priorityFindings.Count == 0)
        {
            return $"Recommended next step: {action}. Confirm details with the client before making any service recommendation.";
        }

        var leadingItem = priorityFindings[0];
        return $"Recommended next step: {action}. Start by reviewing {leadingItem.Category} - {leadingItem.Name}, then confirm the recommendation with the client before export.";
    }

    private static int RankStatus(HealthStatus status) =>
        status switch
        {
            HealthStatus.Critical => 0,
            HealthStatus.AttentionNeeded => 1,
            HealthStatus.ReviewRequired => 2,
            HealthStatus.Good => 3,
            _ => 4
        };
}
