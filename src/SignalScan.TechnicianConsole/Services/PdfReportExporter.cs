using System.IO;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SignalScan.TechnicianConsole.Models;

namespace SignalScan.TechnicianConsole.Services;

public sealed class PdfReportExporter
{
    public Task ExportAsync(string path, ScanResult scanResult, ReportContext context)
    {
        WindowsPdfSharpFontResolver.EnsureConfigured();
        RunSmokeExport();

        return Task.Run(() =>
        {
            using var writer = new PdfReportWriter();
            writer.Build(scanResult, context);
            writer.Save(path);
        });
    }

    private sealed class PdfReportWriter : IDisposable
    {
        private const double Margin = 48;
        private const double FooterHeight = 28;
        private const double LogoWidth = 188;
        private const double LogoHeight = 57;

        private readonly PdfDocument _document = new();
        private readonly XFont _titleFont;
        private readonly XFont _sectionFont;
        private readonly XFont _labelFont;
        private readonly XFont _bodyFont;
        private readonly XFont _smallFont;
        private readonly XBrush _navyBrush = new XSolidBrush(XColor.FromArgb(16, 42, 67));
        private readonly XBrush _blueBrush = new XSolidBrush(XColor.FromArgb(22, 115, 255));
        private readonly XBrush _textBrush = new XSolidBrush(XColor.FromArgb(32, 38, 48));
        private readonly XPen _rulePen = new(XColor.FromArgb(210, 219, 230), 0.75);

        private PdfPage? _page;
        private XGraphics? _gfx;
        private double _y;

        public PdfReportWriter()
        {
            WindowsPdfSharpFontResolver.EnsureConfigured();
            _titleFont = new XFont(WindowsPdfSharpFontResolver.FontFamilyName, 22, XFontStyleEx.Bold);
            _sectionFont = new XFont(WindowsPdfSharpFontResolver.FontFamilyName, 15, XFontStyleEx.Bold);
            _labelFont = new XFont(WindowsPdfSharpFontResolver.FontFamilyName, 10, XFontStyleEx.Bold);
            _bodyFont = new XFont(WindowsPdfSharpFontResolver.FontFamilyName, 10, XFontStyleEx.Regular);
            _smallFont = new XFont(WindowsPdfSharpFontResolver.FontFamilyName, 8, XFontStyleEx.Regular);
        }

        public void Build(ScanResult scanResult, ReportContext context)
        {
            _document.Info.Title = "SignalScan PC Health Report";
            _document.Info.Author = "909 Signal IT";
            AddPage();

            DrawLogoHeader();
            Title("SignalScan PC Health Report");
            Paragraph("Prepared by 909 Signal IT", _bodyFont, _blueBrush, 12);
            Paragraph("Client-facing report generated from read-only diagnostic information and technician-reviewed notes.", _bodyFont);

            Section("Client Information");
            KeyValue("Client Name", ValueOrBlank(context.ClientName));
            KeyValue("Phone/Email", ValueOrBlank(context.ClientContact));
            KeyValue("Device Label", ValueOrBlank(context.DeviceLabel));
            KeyValue("Device Name", scanResult.SystemProfile.ComputerName);
            KeyValue("Scan Date", scanResult.ScanTimestamp.LocalDateTime.ToString("g"));
            KeyValue("Overall Status", HealthStatusFormatter.Format(scanResult.OverallStatus));

            Section("Technician-Reviewed Summary");
            Paragraph(ValueOrBlank(context.TechnicianReviewedSummary), _bodyFont);

            Section("Next-Step Recommendation");
            Paragraph(ValueOrBlank(context.NextStepRecommendation), _bodyFont);
            KeyValue("Recommended Service Package", ValueOrBlank(context.RecommendedService));

            Section("Technician Notes");
            Paragraph(string.IsNullOrWhiteSpace(context.TechnicianNotes) ? "No technician notes entered." : context.TechnicianNotes.Trim(), _bodyFont);

            AppendSystemProfile(scanResult.SystemProfile);
            AppendPerformance(scanResult);
            AppendMaintenance(scanResult);
            AppendSecurity(scanResult);
            AppendFindings("All Findings Summary", scanResult.Findings);

            Section("909 Signal IT Contact");
            Paragraph("909 Signal IT", _bodyFont);
            Paragraph("Local IT Support for Residential and Small Business Clients", _bodyFont);
            Paragraph("Website: https://909signalit.com", _bodyFont);
            Paragraph("Phone: 909-260-8660", _bodyFont);
            Paragraph("Email: support@909signalit.com", _bodyFont);

            Section("Disclaimer");
            Paragraph("SignalScan provides a technician-reviewed system health assessment based on available diagnostic information at the time of scan. Findings are intended to guide maintenance and support decisions. Some issues may require additional hands-on inspection, hardware testing, vendor tools, or follow-up service.", _bodyFont);
            Paragraph("Safety note: SignalScan v1 is read-only. This report was generated without deleting files, changing registry keys, disabling services, changing drivers, removing malware, or performing automatic repairs.", _bodyFont);
        }

        public void Save(string path) => _document.Save(path);

        public void Dispose()
        {
            _gfx?.Dispose();
            _document.Dispose();
        }

        private void AppendSystemProfile(SystemProfile profile)
        {
            Section("System Profile");
            KeyValue("Computer Name", profile.ComputerName);
            KeyValue("Windows Edition", profile.WindowsEdition);
            KeyValue("Windows Version", profile.WindowsDisplayVersion);
            KeyValue("Windows Build", profile.WindowsBuild);
            KeyValue("CPU", profile.CpuModel);
            KeyValue("RAM", profile.Ram);
            KeyValue("Manufacturer/Model", $"{profile.Manufacturer} {profile.Model}".Trim());
            KeyValue("BIOS/Firmware", profile.BiosVersion);
            KeyValue("Uptime", profile.Uptime);
            KeyValue("Current User", profile.CurrentUser);

            Section("Fixed-Drive Storage");
            foreach (var drive in profile.FixedDrives)
            {
                Bullet($"{drive.Name} ({drive.Format}): {drive.FreeSpace} free of {drive.Capacity} ({drive.FreePercent} free)");
            }
        }

        private void AppendPerformance(ScanResult scanResult)
        {
            var snapshot = scanResult.PerformanceSnapshot;
            Section("Performance Snapshot");
            KeyValue("CPU Usage", snapshot.CpuUsage);
            KeyValue("RAM Usage", snapshot.RamUsage);
            KeyValue("RAM Available", snapshot.RamAvailable);
            KeyValue("Disk Free Evaluation", snapshot.DiskFreeEvaluation);
            KeyValue("Startup Entries", snapshot.StartupAppCount);
            KeyValue("Visible Running Processes", snapshot.ProcessCount);
            KeyValue("Uptime Indicator", snapshot.UptimeIndicator);
            AppendFindings("Performance Findings", scanResult.Findings.Where(finding => finding.Category == "Performance"));
        }

        private void AppendMaintenance(ScanResult scanResult)
        {
            var snapshot = scanResult.MaintenanceSnapshot;
            Section("Maintenance Snapshot");
            KeyValue("Pending Reboot", snapshot.PendingReboot);
            KeyValue("Windows Update Status", snapshot.WindowsUpdateStatus);
            KeyValue("Windows Update Status Date", snapshot.WindowsUpdateStatusDate);
            KeyValue("Last Successful Windows Update Install", snapshot.LastSuccessfulWindowsUpdateDate);
            KeyValue("Event Log Summary", snapshot.EventLogSummary);
            KeyValue("Disk Health Status", snapshot.DiskHealthStatus);
            AppendFindings("Maintenance Findings", scanResult.Findings.Where(finding => finding.Category == "Maintenance"));
        }

        private void AppendSecurity(ScanResult scanResult)
        {
            var snapshot = scanResult.SecuritySnapshot;
            Section("Security Snapshot");
            KeyValue("Windows Defender Status", snapshot.WindowsDefenderStatus);
            KeyValue("Firewall Profile Status", snapshot.FirewallProfileStatus);
            KeyValue("BitLocker Status", snapshot.BitLockerStatus);
            KeyValue("Local Administrator Count", snapshot.LocalAdministratorCount);
            KeyValue("Windows Support Status", snapshot.WindowsSupportStatus);
            AppendFindings("Security Findings", scanResult.Findings.Where(finding => finding.Category == "Security"));
        }

        private void AppendFindings(string title, IEnumerable<DiagnosticFinding> findings)
        {
            Section(title);
            var any = false;
            foreach (var finding in findings)
            {
                any = true;
                Bullet($"{finding.Category} - {finding.Name}: {HealthStatusFormatter.Format(finding.Status)}. {finding.Details}");
            }

            if (!any)
            {
                Paragraph("No findings collected for this section.", _bodyFont);
            }
        }

        private void AddPage()
        {
            _gfx?.Dispose();
            _page = _document.AddPage();
            _page.Size = PageSize.Letter;
            _gfx = XGraphics.FromPdfPage(_page);
            _y = Margin;
            DrawFooter();
        }

        private void EnsureSpace(double requiredHeight)
        {
            if (_page is null || _gfx is null)
            {
                AddPage();
                return;
            }

            if (_y + requiredHeight > _page.Height.Point - Margin - FooterHeight)
            {
                AddPage();
            }
        }

        private void Title(string text)
        {
            EnsureSpace(44);
            _gfx!.DrawString(text, _titleFont, _navyBrush, new XPoint(Margin, _y));
            _y += 30;
            _gfx.DrawLine(new XPen(XColor.FromArgb(22, 115, 255), 2), Margin, _y, _page!.Width.Point - Margin, _y);
            _y += 16;
        }

        private void DrawLogoHeader()
        {
            var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "signalscan-logo.png");
            if (!File.Exists(logoPath))
            {
                return;
            }

            EnsureSpace(LogoHeight + 14);
            using var logo = XImage.FromFile(logoPath);
            _gfx!.DrawImage(logo, Margin, _y, LogoWidth, LogoHeight);
            _y += LogoHeight + 14;
        }

        private void Section(string text)
        {
            EnsureSpace(34);
            _y += 6;
            _gfx!.DrawString(text, _sectionFont, _navyBrush, new XPoint(Margin, _y));
            _y += 18;
            _gfx.DrawLine(_rulePen, Margin, _y, _page!.Width.Point - Margin, _y);
            _y += 10;
        }

        private void KeyValue(string label, string value)
        {
            EnsureSpace(20);
            var labelText = $"{label}: ";
            var labelWidth = _gfx!.MeasureString(labelText, _labelFont).Width;
            _gfx.DrawString(labelText, _labelFont, _textBrush, new XPoint(Margin, _y));
            DrawWrappedText(value, _bodyFont, _textBrush, Margin + labelWidth, _y, ContentWidth - labelWidth, 13, 4);
        }

        private void Bullet(string text)
        {
            EnsureSpace(18);
            _gfx!.DrawString("-", _bodyFont, _textBrush, new XPoint(Margin, _y));
            DrawWrappedText(text, _bodyFont, _textBrush, Margin + 14, _y, ContentWidth - 14, 13, 5);
        }

        private void Paragraph(string text, XFont font, XBrush brush, double spaceAfter = 8)
        {
            EnsureSpace(18);
            DrawWrappedText(text, font, brush, Margin, _y, ContentWidth, 13, spaceAfter);
        }

        private void Paragraph(string text, XFont font) => Paragraph(text, font, _textBrush);

        private void DrawWrappedText(string text, XFont font, XBrush brush, double x, double y, double width, double lineHeight, double spaceAfter)
        {
            var normalized = string.IsNullOrWhiteSpace(text) ? "Not provided" : text.ReplaceLineEndings(" ");
            var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var line = string.Empty;
            _y = y;

            foreach (var word in words)
            {
                var candidate = string.IsNullOrEmpty(line) ? word : $"{line} {word}";
                if (_gfx!.MeasureString(candidate, font).Width > width && !string.IsNullOrEmpty(line))
                {
                    EnsureSpace(lineHeight);
                    _gfx.DrawString(line, font, brush, new XPoint(x, _y));
                    _y += lineHeight;
                    line = word;
                }
                else
                {
                    line = candidate;
                }
            }

            if (!string.IsNullOrEmpty(line))
            {
                EnsureSpace(lineHeight);
                _gfx!.DrawString(line, font, brush, new XPoint(x, _y));
                _y += lineHeight;
            }

            _y += spaceAfter;
        }

        private void DrawFooter()
        {
            if (_page is null || _gfx is null)
            {
                return;
            }

            var footerY = _page.Height.Point - 32;
            _gfx.DrawLine(_rulePen, Margin, footerY - 10, _page.Width.Point - Margin, footerY - 10);
            _gfx.DrawString("SignalScan by 909 Signal IT | 909-260-8660 | support@909signalit.com", _smallFont, _textBrush, new XPoint(Margin, footerY));
        }

        private double ContentWidth => _page!.Width.Point - (Margin * 2);

        private static string ValueOrBlank(string value) =>
            string.IsNullOrWhiteSpace(value) ? "Not provided" : value.Trim();
    }

    private static void RunSmokeExport()
    {
        using var document = new PdfDocument();
        document.Info.Title = "SignalScan PDF smoke test";

        var page = document.AddPage();
        page.Size = PageSize.Letter;

        using var gfx = XGraphics.FromPdfPage(page);
        var font = new XFont(WindowsPdfSharpFontResolver.FontFamilyName, 12, XFontStyleEx.Regular);
        gfx.DrawString("SignalScan PDF smoke test", font, XBrushes.Black, new XPoint(48, 48));

        using var stream = new MemoryStream();
        document.Save(stream, false);
    }
}
