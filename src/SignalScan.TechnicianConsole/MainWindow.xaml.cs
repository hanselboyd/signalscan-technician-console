using System.Collections.ObjectModel;
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
    private readonly ObservableCollection<FindingRow> _findingRows = new();
    private readonly ObservableCollection<FindingRow> _performanceFindingRows = new();
    private readonly ObservableCollection<FindingRow> _maintenanceFindingRows = new();
    private readonly ObservableCollection<DriveRow> _driveRows = new();
    private ScanResult? _lastScanResult;

    public MainWindow()
    {
        InitializeComponent();
        FindingsDataGrid.ItemsSource = _findingRows;
        PerformanceFindingsDataGrid.ItemsSource = _performanceFindingRows;
        MaintenanceFindingsDataGrid.ItemsSource = _maintenanceFindingRows;
        FixedDrivesDataGrid.ItemsSource = _driveRows;
    }

    private async void RunScanButton_Click(object sender, RoutedEventArgs e)
    {
        RunScanButton.IsEnabled = false;
        ExportReportButton.IsEnabled = false;
        StatusMessageTextBlock.Text = "Collecting read-only diagnostics...";

        try
        {
            _lastScanResult = await _collector.CollectAsync();
            RenderScan(_lastScanResult);
            ExportReportButton.IsEnabled = true;
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

    private async void ExportReportButton_Click(object sender, RoutedEventArgs e)
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
            StatusMessageTextBlock.Text = $"Report draft exported: {dialog.FileName}";
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
            TechnicianNotesTextBox.Text,
            selectedService);
    }

    private sealed record FindingRow(string Category, string Name, string Status, string Details);

    private sealed record DriveRow(string Name, string Format, string Capacity, string FreeSpace, string FreePercent);
}
