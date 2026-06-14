# SignalScan v1 Release Checklist

## Build

- [ ] Project builds cleanly.
- [ ] App launches on Windows test machine.
- [ ] No build warnings related to unsafe operations.
- [ ] README has working build/run instructions.

## Safety

- [ ] No file deletion code exists.
- [ ] No registry write code exists.
- [ ] No service modification code exists.
- [ ] No driver modification code exists.
- [ ] No program uninstall code exists.
- [ ] No Defender/firewall modification code exists.
- [ ] No automatic repair feature exists.
- [ ] All diagnostics are read-only.
- [ ] AI output is reviewable/editable before export.

## Privacy

- [ ] No passwords collected.
- [ ] No browser history collected.
- [ ] No documents/photos/emails scanned.
- [ ] No cloud transmission in v1 unless explicitly added and documented.
- [ ] Local scan history location is documented.

## Report

- [ ] PDF exports correctly.
- [ ] Report includes SignalScan and 909 Signal IT branding.
- [ ] Report includes disclaimer.
- [ ] Report does not expose sensitive data unintentionally.
- [ ] Technician can edit notes and recommendations before export.

## Testing

- [ ] Tested on Windows 10.
- [ ] Tested on Windows 11.
- [ ] Tested without admin rights.
- [ ] Tested with missing/unavailable diagnostic values.
- [ ] Tested on a non-client machine or VM first.

## Business Readiness

- [ ] Service names finalized.
- [ ] Pricing added separately if desired.
- [ ] Website landing page copy prepared.
- [ ] Client report language approved.
- [ ] Technician workflow documented.
