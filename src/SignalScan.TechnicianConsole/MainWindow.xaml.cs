using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SignalScan.TechnicianConsole.Models;
using SignalScan.TechnicianConsole.Services;

namespace SignalScan.TechnicianConsole;

public partial class MainWindow : Window
{
    private readonly ReadOnlyDiagnosticCollector _collector = new();
    private readonly MarkdownReportExporter _reportExporter = new();
    private readonly PdfReportExporter _pdfReportExporter = new();
    private readonly IAiSummaryProvider _aiSummaryProvider = new OfflineAiSummaryProvider();
    private readonly ScanHistoryStore _historyStore = new();
    private readonly ObservableCollection<FindingRow> _findingRows = new();
    private readonly ObservableCollection<FindingRow> _performanceFindingRows = new();
    private readonly ObservableCollection<FindingRow> _maintenanceFindingRows = new();
    private readonly ObservableCollection<FindingRow> _securityFindingRows = new();
    private readonly ObservableCollection<DriveRow> _driveRows = new();
    private readonly ObservableCollection<HistoryRow> _historyRows = new();
    private ScanResult? _lastScanResult;
    private ScanHistoryRecord? _lastHistoryRecord;

    public MainWindow()
    {
        InitializeComponent();
        FindingsDataGrid.ItemsSource = _findingRows;
        PerformanceFindingsDataGrid.ItemsSource = _performanceFindingRows;
        MaintenanceFindingsDataGrid.ItemsSource = _maintenanceFindingRows;
        SecurityFindingsDataGrid.ItemsSource = _securityFindingRows;
        FixedDrivesDataGrid.ItemsSource = _driveRows;
        ScanHistoryDataGrid.ItemsSource = _historyRows;
        HistoryStorePathTextBlock.Text = $"Local history: {_historyStore.StorePath}";
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadHistoryAsync();
    }

    private async void RunScanButton_Click(object sender, RoutedEventArgs e)
    {
        RunScanButton.IsEnabled = false;
        ExportPdfReportButton.IsEnabled = false;
        ExportMarkdownDraftButton.IsEnabled = false;
        GenerateOfflineSummaryButton.IsEnabled = false;
        StatusMessageTextBlock.Text = "Collecting read-only diagnostics...";

        try
        {
            _lastScanResult = await _collector.CollectAsync();
            _lastHistoryRecord = null;
            RenderScan(_lastScanResult);
            ApplyDefaultRecommendationText(_lastScanResult);
            await SaveCurrentHistoryRecordAsync();
            ExportPdfReportButton.IsEnabled = true;
            ExportMarkdownDraftButton.IsEnabled = true;
            GenerateOfflineSummaryButton.IsEnabled = true;
            StatusMessageTextBlock.Text = "Read-only scan complete. Review findings before exporting a report draft.";
        }
        catch (Exception ex)
        {
            StatusMessageTextBlock.Text = $"Scan could not complete: {ex.Message}";
            MessageBox.Show(
                this,
                "SignalScan could not complete the read-only diagnostic scan. No system changes were made.",
                "SignalScan",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            RunScanButton.IsEnabled = true;
        }
    }

    private void GenerateOfflineSummaryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastScanResult is null)
        {
            return;
        }

        var draft = _aiSummaryProvider.GenerateDraft(_lastScanResult);
        TechnicianReviewedSummaryTextBox.Text = draft.TechnicianReviewedSummary;
        NextStepRecommendationTextBox.Text = draft.NextStepRecommendation;
        StatusMessageTextBlock.Text = "Offline draft summary generated. Review and edit before exporting a report draft.";
    }

    private async void ExportPdfReportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastScanResult is null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Export SignalScan PDF Report",
            Filter = "PDF report (*.pdf)|*.pdf",
            FileName = $"SignalScan-{_lastScanResult.SystemProfile.ComputerName}-{DateTime.Now:yyyyMMdd-HHmm}.pdf",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            await _pdfReportExporter.ExportAsync(dialog.FileName, _lastScanResult, BuildReportContext());
            await SaveCurrentHistoryRecordAsync(pdfPath: dialog.FileName);
            StatusMessageTextBlock.Text = $"PDF report exported: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            var failureMessage = BuildPdfExportFailureMessage(ex);
            WritePdfExportFailureDebugFile(failureMessage);
            StatusMessageTextBlock.Text = $"PDF export failed: {ex}";
            MessageBox.Show(
                this,
                failureMessage,
                "SignalScan",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private async void ExportMarkdownDraftButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastScanResult is null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Export SignalScan Report Draft",
            Filter = "Markdown report (*.md)|*.md|Text report (*.txt)|*.txt",
            FileName = $"SignalScan-{_lastScanResult.SystemProfile.ComputerName}-{DateTime.Now:yyyyMMdd-HHmm}.md",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            await _reportExporter.ExportAsync(dialog.FileName, _lastScanResult, BuildReportContext());
            await SaveCurrentHistoryRecordAsync(markdownPath: dialog.FileName);
            StatusMessageTextBlock.Text = $"Markdown draft exported: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessageTextBlock.Text = $"Report export failed: {ex.Message}";
            MessageBox.Show(
                this,
                "SignalScan could not export the report draft. No system changes were made.",
                "SignalScan",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void RenderScan(ScanResult scanResult)
    {
        var profile = scanResult.SystemProfile;
        ScanTimestampTextBlock.Text = $"Scan date/time: {scanResult.ScanTimestamp.LocalDateTime:f}";
        OverallStatusTextBlock.Text = HealthStatusFormatter.Format(scanResult.OverallStatus);
        CpuUsageTextBlock.Text = scanResult.PerformanceSnapshot.CpuUsage;
        RamUsageTextBlock.Text = scanResult.PerformanceSnapshot.RamUsage;
        RamAvailableTextBlock.Text = scanResult.PerformanceSnapshot.RamAvailable;
        StartupAppCountTextBlock.Text = scanResult.PerformanceSnapshot.StartupAppCount;
        ProcessCountTextBlock.Text = scanResult.PerformanceSnapshot.ProcessCount;
        UptimeIndicatorTextBlock.Text = scanResult.PerformanceSnapshot.UptimeIndicator;
        DiskFreeEvaluationTextBlock.Text = scanResult.PerformanceSnapshot.DiskFreeEvaluation;
        PendingRebootTextBlock.Text = scanResult.MaintenanceSnapshot.PendingReboot;
        WindowsUpdateStatusTextBlock.Text = scanResult.MaintenanceSnapshot.WindowsUpdateStatus;
        WindowsUpdateStatusDateTextBlock.Text = scanResult.MaintenanceSnapshot.WindowsUpdateStatusDate;
        LastSuccessfulUpdateTextBlock.Text = scanResult.MaintenanceSnapshot.LastSuccessfulWindowsUpdateDate;
        EventLogSummaryTextBlock.Text = scanResult.MaintenanceSnapshot.EventLogSummary;
        DiskHealthStatusTextBlock.Text = scanResult.MaintenanceSnapshot.DiskHealthStatus;
        DefenderStatusTextBlock.Text = scanResult.SecuritySnapshot.WindowsDefenderStatus;
        FirewallProfileStatusTextBlock.Text = scanResult.SecuritySnapshot.FirewallProfileStatus;
        BitLockerStatusTextBlock.Text = scanResult.SecuritySnapshot.BitLockerStatus;
        LocalAdministratorCountTextBlock.Text = scanResult.SecuritySnapshot.LocalAdministratorCount;
        WindowsSupportStatusTextBlock.Text = scanResult.SecuritySnapshot.WindowsSupportStatus;
        SystemSummaryTextBlock.Text =
            $"Computer: {profile.ComputerName}\n" +
            $"Windows edition: {profile.WindowsEdition}\n" +
            $"Windows version/build: {profile.WindowsDisplayVersion} / {profile.WindowsBuild}\n" +
            $"CPU: {profile.CpuModel}\n" +
            $"RAM: {profile.Ram}\n" +
            $"Manufacturer/Model: {profile.Manufacturer} {profile.Model}\n" +
            $"BIOS/Firmware: {profile.BiosVersion}\n" +
            $"Uptime: {profile.Uptime}\n" +
            $"Current user: {profile.CurrentUser}";

        _driveRows.Clear();
        foreach (var drive in profile.FixedDrives)
        {
            _driveRows.Add(new DriveRow(
                drive.Name,
                drive.Format,
                drive.Capacity,
                drive.FreeSpace,
                drive.FreePercent));
        }

        _findingRows.Clear();
        _performanceFindingRows.Clear();
        _maintenanceFindingRows.Clear();
        _securityFindingRows.Clear();
        foreach (var finding in scanResult.Findings)
        {
            var row = new FindingRow(
                finding.Category,
                finding.Name,
                HealthStatusFormatter.Format(finding.Status),
                finding.Details);
            _findingRows.Add(row);
            if (finding.Category == "Performance")
            {
                _performanceFindingRows.Add(row);
            }
            else if (finding.Category == "Maintenance")
            {
                _maintenanceFindingRows.Add(row);
            }
            else if (finding.Category == "Security")
            {
                _securityFindingRows.Add(row);
            }
        }
    }

    private ReportContext BuildReportContext()
    {
        var selectedService = (RecommendedServiceComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()
            ?? RecommendedServiceComboBox.Text;
        return new ReportContext(
            ClientNameTextBox.Text,
            ClientContactTextBox.Text,
            DeviceLabelTextBox.Text,
            TechnicianReviewedSummaryTextBox.Text,
            NextStepRecommendationTextBox.Text,
            TechnicianNotesTextBox.Text,
            selectedService);
    }

    private async Task SaveCurrentHistoryRecordAsync(string? pdfPath = null, string? markdownPath = null)
    {
        try
        {
            if (_lastScanResult is null)
            {
                return;
            }

            var context = BuildReportContext();
            var record = new ScanHistoryRecord(
                _lastHistoryRecord?.Id ?? Guid.NewGuid().ToString("N"),
                _lastScanResult.ScanTimestamp,
                ValueOrBlank(context.ClientName),
                _lastScanResult.SystemProfile.ComputerName,
                HealthStatusFormatter.Format(_lastScanResult.OverallStatus),
                ValueOrBlank(context.RecommendedService),
                CleanOptionalPath(pdfPath ?? _lastHistoryRecord?.ExportedPdfPath),
                CleanOptionalPath(markdownPath ?? _lastHistoryRecord?.ExportedMarkdownPath));

            _lastHistoryRecord = record;
            await _historyStore.SaveOrUpdateAsync(record);
            await LoadHistoryAsync();
        }
        catch (Exception ex)
        {
            HistoryStorePathTextBlock.Text = $"Local history unavailable: {ex.Message}";
        }
    }

    private async Task LoadHistoryAsync()
    {
        _historyRows.Clear();
        var records = await _historyStore.LoadAsync();
        foreach (var record in records)
        {
            _historyRows.Add(new HistoryRow(
                record.ScanTimestamp.LocalDateTime.ToString("g"),
                record.ClientName,
                record.DeviceName,
                record.OverallStatus,
                record.RecommendedService,
                record.ExportedPdfPath,
                record.ExportedMarkdownPath));
        }
    }

    private void ApplyDefaultRecommendationText(ScanResult scanResult)
    {
        if (string.IsNullOrWhiteSpace(TechnicianReviewedSummaryTextBox.Text))
        {
            TechnicianReviewedSummaryTextBox.Text = BuildDefaultSummary(scanResult);
        }

        if (string.IsNullOrWhiteSpace(NextStepRecommendationTextBox.Text))
        {
            NextStepRecommendationTextBox.Text = BuildDefaultNextStep(scanResult.OverallStatus);
        }
    }

    private static string BuildDefaultSummary(ScanResult scanResult)
    {
        var status = HealthStatusFormatter.Format(scanResult.OverallStatus);
        return $"Technician-reviewed summary pending. Initial read-only SignalScan status: {status}. Review the System Profile, Performance, Maintenance, and Security findings before sharing with the client.";
    }

    private static string BuildDefaultNextStep(HealthStatus status) =>
        status switch
        {
            HealthStatus.Good => "Routine maintenance or monitoring is recommended. No urgent issue was identified by the read-only scan.",
            HealthStatus.AttentionNeeded => "A tune-up or technician review is recommended to address the items marked Attention Needed.",
            HealthStatus.Critical => "Urgent technician review is recommended before relying on this device for important work.",
            _ => "Manual technician review is needed because one or more checks could not be completed or require interpretation."
        };

    private static string ValueOrBlank(string value) =>
        string.IsNullOrWhiteSpace(value) ? "Not provided" : value.Trim();

    private static string CleanOptionalPath(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string BuildPdfExportFailureMessage(Exception exception)
    {
        var message = new StringBuilder();
        message.AppendLine("PDF export failed with exception details:");
        message.AppendLine(exception.ToString());
        message.AppendLine();
        message.AppendLine($"Regular font: {WindowsPdfSharpFontResolver.SelectedRegularFontPath ?? "Unavailable"}");
        message.AppendLine($"Bold font: {WindowsPdfSharpFontResolver.SelectedBoldFontPath ?? "Unavailable"}");
        message.AppendLine($"PDFsharp resolver initialized: {WindowsPdfSharpFontResolver.IsPdfSharpResolverInitialized}");

        return message.ToString();
    }

    private static void WritePdfExportFailureDebugFile(string failureMessage)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "909 Signal IT",
                "SignalScan");
            Directory.CreateDirectory(directory);

            var path = Path.Combine(directory, "pdf-export-error.txt");
            File.WriteAllText(path, failureMessage);
        }
        catch
        {
            // Preserve the original export failure dialog even if the debug file cannot be written.
        }
    }

    private sealed record FindingRow(string Category, string Name, string Status, string Details);

    private sealed record DriveRow(string Name, string Format, string Capacity, string FreeSpace, string FreePercent);

    private sealed record HistoryRow(
        string ScanTimestamp,
        string ClientName,
        string DeviceName,
        string OverallStatus,
        string RecommendedService,
        string ExportedPdfPath,
        string ExportedMarkdownPath);
}
