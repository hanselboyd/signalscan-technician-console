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
