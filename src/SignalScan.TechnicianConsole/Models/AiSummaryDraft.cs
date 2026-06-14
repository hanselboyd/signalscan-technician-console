namespace SignalScan.TechnicianConsole.Models;

public sealed record AiSummaryDraft(
    string TechnicianReviewedSummary,
    string NextStepRecommendation);
