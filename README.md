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
- Show computer name, Windows version/build, CPU model, RAM, storage summary, manufacturer/model, BIOS version, uptime, current user, and visible process count.
- Display findings using the required status language: Good, Attention Needed, Critical, and Review Required.
- Collect technician notes and a recommended service package.
- Export a branded Markdown report draft with the required disclaimer.

The current dashboard does not:

- Delete files.
- Modify registry keys.
- Disable services.
- Change drivers.
- Modify firewall, Defender, or antivirus settings.
- Remove malware.
- Perform cleanup or automatic repairs.

PDF export, AI summary generation, deeper maintenance/security checks, and local scan history are intentionally left for later tasks in `CODEX_TASKS.md`.

## Build and Run

Requirements:

- Windows 10/11.
- .NET 8 SDK or newer with Windows Desktop workload support.

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

SignalScan v1 is read-only. Diagnostic collection uses safe inspection APIs and read-only registry access only. Report export writes only the technician-selected report file. No repair, cleanup, deletion, registry write, driver change, service modification, firewall modification, Defender modification, malware removal, remote execution, or background monitoring feature exists in this initial project.
