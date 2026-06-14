using SignalScan.TechnicianConsole.Models;

namespace SignalScan.TechnicianConsole.Services;

public interface IAiSummaryProvider
{
    AiSummaryDraft GenerateDraft(ScanResult scanResult);
}
