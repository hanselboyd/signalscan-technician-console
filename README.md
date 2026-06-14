# SignalScan Technician Console

**SignalScan by 909 Signal IT** is an AI-assisted Windows PC diagnostic and client-reporting tool for residential and small business IT support.

Current release candidate: **v1.0.0**

Internal codename: **Horsepower**

## Product Positioning

SignalScan is not a "one-click PC cleaner." It is a professional technician utility used to inspect Windows systems, explain findings in plain English, and generate technician-reviewed client reports.

## Core Principle

> AI explains. The technician decides.

Version 1 must be read-only. It may collect diagnostics, summarize findings, and generate reports, but it must not automatically repair, delete, disable, uninstall, or change system configuration.

## MVP Goals

- Windows desktop application
- 909 Signal IT branding
- Read-only system diagnostics
- Technician dashboard
- AI-assisted findings summary
- Technician-editable recommendations
- Branded PDF report export
- Local scan history
- No automatic repair actions in v1

## Recommended Stack

- C#
- .NET 8 or newer
- WPF or WinUI
- SQLite for local scan history
- PowerShell/WMI/CIM for read-only diagnostics
- PDF generation library
- Optional AI integration through a configurable provider layer

## Initial Repository Structure

```text
signalscan-technician-console/
  README.md
  PRODUCT_SPEC.md
  SAFETY_RULES.md
  CODEX_TASKS.md
  REPORT_TEMPLATE.md
  src/
  docs/
  assets/
  tests/
```

## First Codex Objective

Create the first working SignalScan Technician Console MVP with read-only diagnostics, dashboard display, and report export scaffolding.

## Current Implementation Status

SignalScan Technician Console v1.0.0 is prepared as a safe technician demo/release candidate with a Windows WPF desktop app:

```text
src/
  SignalScan.TechnicianConsole/
    SignalScan.TechnicianConsole.csproj
    App.xaml
    MainWindow.xaml
    Models/
    Services/
```

The current dashboard can:

- Display SignalScan by 909 Signal IT branding.
- Run a read-only diagnostic scan.
- Show computer name, Windows edition/version/build, CPU model, RAM, fixed-drive storage, manufacturer/model, BIOS version, uptime, current user, and visible process count.
- Show read-only performance findings for CPU, RAM, disk free percentage, startup entry count, visible process count, and uptime.
- Show read-only maintenance findings for pending reboot indicators, Windows Update history timestamps, Event Log warning/error counts, and disk health status where available.
- Show read-only security posture findings for Windows Defender, firewall profiles, BitLocker, local administrator count, and Windows version support where available.
- Display findings using the required status language: Good, Attention Needed, Critical, and Review Required.
- Collect technician notes, a technician-reviewed summary, a next-step recommendation, and a recommended service package.
- Show local-only scan history with minimal scan/report metadata.
- Export a branded client-facing PDF report and a Markdown draft/debug report with the required disclaimer.

## Task 2 Read-Only System Profile

The System Profile module collects and displays:

- Computer name
- Windows edition
- Windows display version
- Windows build number and update build revision where available
- CPU model
- RAM amount
- Fixed-drive storage capacity, free space, and free percentage
- Device manufacturer and model where available
- BIOS/firmware version where available
- Current uptime
- Current user

System profile collection is read-only. It uses safe environment/runtime APIs, fixed-drive enumeration, a read-only memory status API, and read-only registry access with `OpenSubKey(..., writable: false)`. Missing values are normalized to `Unavailable`.

Lightweight helper methods live in `DiagnosticValueFormatter` for unavailable-value normalization, byte formatting, free-space percentage formatting, and uptime formatting.

## Task 3 Read-Only Performance Findings

The Performance module collects and displays:

- CPU usage snapshot using read-only Windows system time counters
- RAM usage snapshot and available RAM using a read-only memory status API
- Disk free percentage evaluation using the existing fixed-drive System Profile data
- Startup app count from read-only registry locations
- Visible running process count
- Uptime/reboot attention indicator

Performance collection is read-only. It does not stop processes, modify startup items, disable services, write to registry keys, delete files, run cleanup, or perform repairs.

Startup entry counting reads these locations only:

- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\RunOnce`
- `HKLM\Software\Microsoft\Windows\CurrentVersion\Run`
- `HKLM\Software\Microsoft\Windows\CurrentVersion\RunOnce`

Registry access remains read-only through `OpenSubKey(..., writable: false)`. Missing values are shown as `Unavailable`. Performance status thresholds are implemented in `PerformanceFindingEvaluator` so they can be unit tested separately later.

## Task 4 Read-Only Maintenance Findings

The Maintenance module collects and displays:

- Pending reboot flag from common read-only Windows registry indicators
- Windows Update detect/install history status where available
- Last successful Windows Update install date where available
- System and Application Event Log warning/error counts for the last 7 days
- Disk health status from the read-only `Win32_DiskDrive` provider where available

Maintenance collection is read-only. It does not install updates, trigger update scans, repair disks, clear logs, delete files, modify services, write to registry keys, perform cleanup, or perform automatic repairs.

Registry access remains read-only through `OpenSubKey(..., writable: false)`. Event Log access uses read-only event queries. Disk health access uses read-only WMI data. Missing or inaccessible values are shown as `Unavailable`.

## Task 5 Read-Only Security Posture Findings

The Security module collects and displays:

- Windows Defender service, antivirus, real-time protection, network inspection, and signature timestamp where available
- Firewall profile status for Domain, Private, and Public profiles where available
- BitLocker protection status by volume where available
- Local Administrators group member count where available
- Windows build support/lifecycle attention indicator where available, including build values reported with a dotted update build revision such as `19045.5247`

Security collection is read-only. It does not change Defender settings, firewall settings, BitLocker state, user accounts, group policy, services, registry keys, files, or security policy.

Firewall profile status is read through `OpenSubKey(..., writable: false)`. Defender, BitLocker, and local administrator checks use read-only WMI queries. Missing or inaccessible values are shown as `Unavailable`.

## Task 6 Technician Notes and Service Recommendation Workflow

The technician workflow includes editable fields for:

- Client name
- Client contact
- Device label
- Technician-reviewed summary
- Next-step recommendation
- Technician notes
- Recommended service package

After a read-only scan, SignalScan seeds safe default recommendation text based on the overall status:

- Good: routine maintenance or monitoring
- Attention Needed: tune-up or technician review recommended
- Critical: urgent technician review
- Review Required: manual technician review needed

The default summary and recommendation are plain text only. They must be reviewed and edited by the technician before export. No AI summary provider, PDF export, cleanup, repair, or system modification workflow is included in Task 6.

## Task 7 Offline AI Summary Interface

SignalScan includes an AI summary provider interface, `IAiSummaryProvider`, so future summary providers can be added behind a clear boundary.

Task 7 uses `OfflineAiSummaryProvider`, a deterministic mock provider that:

- Runs locally only
- Uses the existing in-memory `ScanResult`
- Generates draft plain-English summary and next-step text
- Populates editable technician review fields only
- Requires technician review before export

The offline provider does not use API keys, network calls, cloud sync, telemetry, or external AI services. It does not send diagnostic data anywhere. It does not auto-export, auto-finalize, repair, clean up, delete files, write registry keys, modify services, change drivers, change Defender/firewall/BitLocker settings, manage users, change Windows Update, or modify startup items.

## Task 8 PDF Report Export

SignalScan can export:

- A branded client-facing PDF report
- A Markdown draft/debug report

Both export formats use only the already-collected `ScanResult` and technician-entered report context. Export does not collect new diagnostics, call external services, sync data to the cloud, run AI providers, repair, clean up, delete files, write registry keys, modify services, change drivers, change Defender/firewall/BitLocker settings, manage users, change Windows Update, or modify startup items.

PDF export uses the `PDFsharp` NuGet package. PDFsharp is documented by its project as MIT licensed, which is suitable for commercial use with normal license compliance. The app does not use Microsoft Office automation, Word, Edge, Chrome, browser rendering, printer drivers, or system print services to generate reports.

PDFsharp font resolution is handled by a local Windows font resolver that reads installed fonts from the Windows Fonts directory. SignalScan maps regular text to `arial.ttf` with `segoeui.ttf` as fallback, and bold text to `arialbd.ttf` with `segoeuib.ttf` as fallback. SignalScan does not bundle, download, install, or commit font files; if neither the preferred nor fallback fonts are available, PDF export fails with a clear message and no system changes are made.

## Task 9 Local Scan History

SignalScan stores a local-only scan history JSON file at:

```text
%LOCALAPPDATA%\909 Signal IT\SignalScan\scan-history.json
```

The history store is used only by this Windows profile on this machine. It does not sync to the cloud, send telemetry, make network calls, or transmit scan data.

History records contain only minimal metadata:

- Scan timestamp
- Client name
- Device/computer name
- Overall status
- Recommended service package
- Exported PDF path, if available
- Exported Markdown path, if available

History does not store passwords, browser history, emails, personal file names, photos, license keys, sensitive personal content, full diagnostic details, technician notes, summaries, or finding explanations. If the history file is missing or unreadable, the dashboard treats history as empty and continues without blocking scans or exports.

## v1.0.0 Release Candidate Notes

SignalScan Technician Console v1.0.0 is intended for internal technician demo and release-candidate testing on Windows 10/11 machines or VMs. Before client use, complete `RELEASE_CHECKLIST.md` and review `PRIVACY_AND_SAFETY.md`.

v1.0.0 includes read-only diagnostics, dashboard findings, offline draft summary text, technician-reviewed recommendations, PDF/Markdown export, and local-only scan history. It does not include real external AI calls, cloud sync, telemetry, analytics, licensing, payments, auto-update, background monitoring, cleanup, repair, or system modification features.

The current dashboard does not:

- Install updates.
- Trigger update scans.
- Repair disks.
- Clear Event Logs.
- Delete files.
- Modify registry keys.
- Disable services.
- Manage user accounts.
- Modify group policy.
- Change drivers.
- Modify firewall, Defender, or antivirus settings.
- Remove malware.
- Perform cleanup or automatic repairs.

Deeper maintenance/security checks are intentionally left for later tasks in `CODEX_TASKS.md`.

## Demo Kit

The SignalScan demo kit contains sales and demo support materials for presenting v1.0.0 without changing app behavior:

- [Demo kit overview](docs/demo-kit/README.md)
- [PC Health Check service offer](docs/demo-kit/PC_HEALTH_CHECK_SERVICE_OFFER.md)
- [Prospect outreach scripts](docs/demo-kit/PROSPECT_OUTREACH_SCRIPTS.md)
- [Demo checklist](docs/demo-kit/DEMO_CHECKLIST.md)

## Build and Run

Requirements:

- Windows 10/11.
- .NET 8 SDK or newer with Windows Desktop workload support.

Known local environment blocker: the current development machine has .NET runtimes installed but no .NET SDK. Running `dotnet build .\SignalScan.TechnicianConsole.sln` locally reports `No .NET SDKs were found.` Install the .NET 8 SDK or newer to build and run the app.

Build:

```powershell
dotnet build .\SignalScan.TechnicianConsole.sln
```

Continuous integration:

- GitHub Actions runs `.github/workflows/windows-build.yml` on pushes and pull requests to `main`.
- The workflow uses `windows-latest`, installs the .NET 8 SDK, restores NuGet packages, and builds `SignalScan.TechnicianConsole.sln` in Release configuration.
- GitHub Actions also includes `.github/workflows/windows-release-package.yml`, a manual/tag-triggered zip packaging workflow for v1.0.0 release candidates.
- CI validates restore/build readiness. Launching the WPF dashboard, running a read-only scan, and exporting a Markdown report still require a manual smoke test on a Windows desktop session with the .NET SDK installed.

Run:

```powershell
dotnet run --project .\src\SignalScan.TechnicianConsole\SignalScan.TechnicianConsole.csproj
```

Publish a local test build:

```powershell
dotnet publish .\src\SignalScan.TechnicianConsole\SignalScan.TechnicianConsole.csproj -c Release -r win-x64 --self-contained false
```

Create a local v1.0.0 zip release package:

```powershell
dotnet publish .\src\SignalScan.TechnicianConsole\SignalScan.TechnicianConsole.csproj -c Release -r win-x64 --self-contained false -o .\artifacts\SignalScan-v1.0.0-win-x64
Copy-Item .\README.md, .\RELEASE_CHECKLIST.md, .\PRIVACY_AND_SAFETY.md, .\LAUNCH_INSTRUCTIONS.md .\artifacts\SignalScan-v1.0.0-win-x64\
Compress-Archive -Path .\artifacts\SignalScan-v1.0.0-win-x64 -DestinationPath .\artifacts\SignalScan-v1.0.0-win-x64.zip -Force
```

Run the packaged app:

1. Unzip `SignalScan-v1.0.0-win-x64.zip`.
2. Open the unzipped folder.
3. Run `SignalScan.TechnicianConsole.exe`.
4. Run the app first on a non-client machine or VM and complete `RELEASE_CHECKLIST.md` before client use.

Trigger the GitHub Actions release package workflow:

1. Open the repository's GitHub Actions tab.
2. Select `Windows Release Package`.
3. Choose `Run workflow` for a manual package, or push a tag matching `v*`, such as `v1.0.0`.
4. Download the `SignalScan-v1.0.0-win-x64` artifact from the completed workflow run.

The first v1.0.0 package is zip-only, not an installer. It does not install services, write registry keys, add startup items, configure auto-update, or change Windows system settings.

## Development Safety Confirmation

SignalScan v1 is read-only. Diagnostic collection uses safe inspection APIs and read-only registry access only. Report export writes only the technician-selected report file. No repair, cleanup, deletion, registry write, startup item modification, process stopping, driver change, service modification, firewall modification, Defender modification, malware removal, remote execution, or background monitoring feature exists in this initial project.
