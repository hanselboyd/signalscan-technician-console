# SignalScan v1.0.0 Release Checklist

Use this checklist before treating SignalScan Technician Console v1.0.0 as a technician demo or release candidate.

## Build Readiness

- [ ] GitHub Actions `Windows Build` workflow passes on `main`.
- [ ] Project restores NuGet packages successfully on a Windows machine with the .NET 8 SDK.
- [ ] `dotnet build .\SignalScan.TechnicianConsole.sln --configuration Release` passes.
- [ ] README build/run instructions are accurate.
- [ ] No build warnings suggest unsafe, destructive, or unsupported behavior.

## Manual Smoke Test

- [ ] App launches on a Windows 10 or Windows 11 test machine.
- [ ] App shows SignalScan / 909 Signal IT branding and v1.0.0 marker.
- [ ] `Run Read-Only Scan` completes without crashing.
- [ ] Dashboard populates System Profile data.
- [ ] Dashboard populates Performance findings.
- [ ] Dashboard populates Maintenance findings.
- [ ] Dashboard populates Security findings.
- [ ] Missing or unavailable values display gracefully as `Unavailable`.
- [ ] `Generate Offline Draft Summary` populates editable summary and next-step fields.
- [ ] Offline draft text is clearly marked as technician-reviewed draft text.
- [ ] Technician can edit client name, contact, device label, notes, summary, next step, and recommended service.
- [ ] `Export PDF Report` creates a readable branded PDF.
- [ ] `Export Markdown Draft` creates a readable Markdown report.
- [ ] Local scan history updates after scan.
- [ ] Local scan history updates PDF path after PDF export.
- [ ] Local scan history updates Markdown path after Markdown export.
- [ ] App still functions if local history file is missing or unreadable.

## Safety Confirmation

- [ ] No repair feature exists.
- [ ] No cleanup feature exists.
- [ ] No file deletion workflow exists except normal build artifacts outside the app.
- [ ] No registry write code exists.
- [ ] Registry access, where present, uses read-only `OpenSubKey(..., writable: false)`.
- [ ] No service modification code exists.
- [ ] No driver modification code exists.
- [ ] No Defender or antivirus modification code exists.
- [ ] No firewall modification code exists.
- [ ] No BitLocker enable/disable/suspend/resume code exists.
- [ ] No user-account or group-policy modification code exists.
- [ ] No Windows Update install, scan trigger, or settings-change code exists.
- [ ] No startup-item modification code exists.
- [ ] No automatic repair action exists.

## Privacy Confirmation

- [ ] No passwords are collected.
- [ ] No browser history is collected.
- [ ] No emails are collected.
- [ ] No personal documents, photos, or file names are collected.
- [ ] No software license keys are collected.
- [ ] No cloud sync exists.
- [ ] No telemetry or analytics exists.
- [ ] No network calls are used for diagnostics, AI, reporting, or history.
- [ ] Local history stores only the minimal metadata documented in `PRIVACY_AND_SAFETY.md`.
- [ ] Reports are reviewed by the technician before sharing.

## Report Review

- [ ] PDF includes SignalScan PC Health Report title.
- [ ] PDF includes 909 Signal IT branding and contact section.
- [ ] PDF includes client information.
- [ ] PDF includes overall status.
- [ ] PDF includes technician-reviewed summary.
- [ ] PDF includes next-step recommendation.
- [ ] PDF includes recommended service package.
- [ ] PDF includes technician notes.
- [ ] PDF includes System Profile details.
- [ ] PDF includes Performance snapshot/findings.
- [ ] PDF includes Maintenance snapshot/findings.
- [ ] PDF includes Security snapshot/findings.
- [ ] PDF includes findings summary.
- [ ] PDF includes required disclaimer.
- [ ] PDF does not expose sensitive local paths except technician-selected report paths in local history.

## Demo Readiness

- [ ] Test on a non-client machine or VM first.
- [ ] Confirm expected behavior without administrator rights.
- [ ] Confirm checks that require unavailable permissions fail gracefully.
- [ ] Confirm technician understands offline draft text is not a final diagnosis.
- [ ] Confirm technician understands SignalScan v1.0.0 is read-only and report-focused.
