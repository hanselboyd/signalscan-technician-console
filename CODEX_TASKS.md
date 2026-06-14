# Codex Task Plan for SignalScan

## Task 1 — Create Initial Project

Prompt:

Create a new Windows desktop application called SignalScan Technician Console for 909 Signal IT.

Use C# and .NET.

Version 1 must be read-only. It should collect basic Windows system diagnostics, display the results in a technician dashboard, and export a branded client-facing report.

Do not include file deletion, registry modification, service disabling, malware removal, driver changes, or automatic repair features.

Create the initial project structure, README, safety rules, product spec, and first working diagnostic dashboard.

Acceptance Criteria:

- Project builds successfully.
- App launches on Windows.
- App shows SignalScan / 909 Signal IT branding.
- No destructive actions exist.
- README includes build/run instructions.

## Task 2 — Add Read-Only System Profile

Prompt:

Add a read-only System Profile module that collects computer name, Windows version/build, CPU model, RAM amount, storage capacity/free space, manufacturer/model where available, BIOS version where available, and uptime.

Display this information in the dashboard.

Acceptance Criteria:

- Data is collected read-only.
- Errors are handled gracefully.
- Missing values show as "Unavailable."
- No admin-only requirement unless unavoidable.

## Task 3 — Add Performance Findings

Prompt:

Add read-only performance indicators: CPU usage snapshot, RAM usage snapshot, disk free percentage, startup app count where safely available, process count, and uptime/reboot indicator.

Create simple finding levels: Good, Attention Needed, Critical, Review Required.

Acceptance Criteria:

- Dashboard groups performance findings clearly.
- No startup items are modified.
- No processes are stopped.
- No settings are changed.

## Task 4 — Add Maintenance Findings

Prompt:

Add read-only maintenance indicators: pending reboot flag if available, Windows update status/date where safely available, event log warning/error counts, and disk health status where safely available.

Acceptance Criteria:

- App does not install updates.
- App does not repair disk.
- App does not clear logs.
- Findings are presented as recommendations only.

## Task 5 — Add Security Posture Findings

Prompt:

Add read-only security posture checks: Windows Defender status, firewall status, BitLocker status if available, local admin account count, and unsupported Windows version warning.

Acceptance Criteria:

- App does not change Defender/firewall settings.
- App does not manage user accounts.
- Findings are presented clearly and safely.

## Task 6 — Add Technician Notes

Prompt:

Add fields for client name, client phone/email, device label, technician notes, and recommended service package.

Service package options:
- SignalScan PC Health Check
- SignalScan Tune-Up
- SignalScan Security Review
- SignalScan Business Workstation Assessment
- SignalCare Monthly Maintenance Plan

Acceptance Criteria:

- Notes are editable.
- Recommendations are editable.
- Information appears in report preview.

## Task 7 — Add AI Summary Interface

Prompt:

Add an AI summary interface that accepts structured scan results and returns a plain-English client summary, key findings, and recommended next step.

Implement it behind an interface so the app can support multiple providers later.

For now, include a mock/offline AI provider that generates deterministic summary text from scan findings.

Acceptance Criteria:

- App works without an API key.
- AI output is editable before export.
- AI does not trigger any system actions.
- Provider interface is clearly separated.

## Task 8 — Add PDF Report Export

Prompt:

Add branded PDF report export for SignalScan by 909 Signal IT.

The PDF should include:
- Client/device info
- Scan date
- Overall status
- System summary
- Performance findings
- Maintenance findings
- Security findings
- AI-assisted summary
- Technician notes
- Recommended service package
- Disclaimer
- 909 Signal IT contact section

Acceptance Criteria:

- PDF exports successfully.
- Report is readable and professional.
- AI text can be edited before export.
- No sensitive local paths or private system data are exposed unless intentionally included.

## Task 9 — Add Local Scan History

Prompt:

Add local scan history using SQLite or a simple local database.

Store:
- Scan date/time
- Client/device name
- Overall status
- Main findings
- Recommended service
- Report path
- Technician notes

Acceptance Criteria:

- Technician can view previous scans.
- Local data storage is documented.
- No cloud sync exists in v1.

## Task 10 — Prepare v1 Release

Prompt:

Prepare SignalScan v1 for internal technician testing.

Add:
- Build instructions
- Release checklist
- Test checklist
- Known limitations
- Safety confirmation checklist

Acceptance Criteria:

- Clean build passes.
- README is complete.
- Release checklist confirms read-only behavior.
- App is ready for testing on a Windows VM or spare test PC.
