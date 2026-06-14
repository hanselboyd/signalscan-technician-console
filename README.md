# SignalScan Technician Console

**SignalScan by 909 Signal IT** is an AI-assisted Windows PC diagnostic and client-reporting tool for residential and small business IT support.

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

Task 1 has been started with a Windows WPF desktop app:

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
- Collect technician notes and a recommended service package.
- Export a branded Markdown report draft with the required disclaimer.

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
- Windows build support/lifecycle attention indicator where available

Security collection is read-only. It does not change Defender settings, firewall settings, BitLocker state, user accounts, group policy, services, registry keys, files, or security policy.

Firewall profile status is read through `OpenSubKey(..., writable: false)`. Defender, BitLocker, and local administrator checks use read-only WMI queries. Missing or inaccessible values are shown as `Unavailable`.

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

PDF export, AI summary generation, deeper maintenance/security checks, and local scan history are intentionally left for later tasks in `CODEX_TASKS.md`.

## Build and Run

Requirements:

- Windows 10/11.
- .NET 8 SDK or newer with Windows Desktop workload support.

Known local environment blocker: the current development machine has .NET runtimes installed but no .NET SDK. Running `dotnet build .\SignalScan.TechnicianConsole.sln` locally reports `No .NET SDKs were found.` Install the .NET 8 SDK or newer to build and run the app.

Build:

```powershell
dotnet build .\SignalScan.TechnicianConsole.sln
```

Run:

```powershell
dotnet run --project .\src\SignalScan.TechnicianConsole\SignalScan.TechnicianConsole.csproj
```

Publish a local test build:

```powershell
dotnet publish .\src\SignalScan.TechnicianConsole\SignalScan.TechnicianConsole.csproj -c Release -r win-x64 --self-contained false
```

## Development Safety Confirmation

SignalScan v1 is read-only. Diagnostic collection uses safe inspection APIs and read-only registry access only. Report export writes only the technician-selected report file. No repair, cleanup, deletion, registry write, startup item modification, process stopping, driver change, service modification, firewall modification, Defender modification, malware removal, remote execution, or background monitoring feature exists in this initial project.
