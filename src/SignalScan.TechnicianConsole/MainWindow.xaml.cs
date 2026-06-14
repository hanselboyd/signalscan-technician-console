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
    private ScanResult? _lastScanResult;

    public MainWindow()
    {
        InitializeComponent();
        FindingsDataGrid.ItemsSource = _findingRows;
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
        SystemSummaryTextBlock.Text =
            $"Computer: {profile.ComputerName}\n" +
            $"Windows: {profile.WindowsVersion} build {profile.WindowsBuild}\n" +
            $"CPU: {profile.CpuModel}\n" +
            $"RAM: {profile.Ram}\n" +
            $"Storage: {profile.StorageSummary}\n" +
            $"Manufacturer/Model: {profile.Manufacturer} {profile.Model}\n" +
            $"BIOS/Firmware: {profile.BiosVersion}\n" +
            $"Uptime: {profile.Uptime}";

        _findingRows.Clear();
        foreach (var finding in scanResult.Findings)
        {
            _findingRows.Add(new FindingRow(
                finding.Category,
                finding.Name,
                HealthStatusFormatter.Format(finding.Status),
                finding.Details));
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
}
