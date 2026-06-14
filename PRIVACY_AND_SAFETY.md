# SignalScan v1.0.0 Privacy and Safety

SignalScan Technician Console v1.0.0 is a local, read-only Windows diagnostic and reporting tool for 909 Signal IT technicians.

## What SignalScan Collects

SignalScan collects read-only system health information that is useful for a technician-reviewed PC health report:

- Computer name
- Windows edition, version, build, and update build revision where available
- CPU model
- Installed RAM amount
- Fixed-drive capacity, free space, and free percentage
- Device manufacturer and model where available
- BIOS/firmware version where available
- Uptime
- Current user name
- CPU usage snapshot
- RAM usage snapshot
- Startup app count from read-only registry locations
- Visible running process count
- Pending reboot indicators where safely available
- Windows Update history timestamps where safely available
- System and Application Event Log warning/error counts
- Disk health status where safely available
- Windows Defender status where safely available
- Firewall profile status where safely available
- BitLocker status where safely available
- Local Administrators group member count where safely available
- Windows build support indicator

Technicians may also enter:

- Client name
- Client contact
- Device label
- Technician-reviewed summary
- Next-step recommendation
- Technician notes
- Recommended service package

## What SignalScan Does Not Collect

SignalScan v1.0.0 does not collect:

- Passwords
- Browser history
- Emails
- Personal documents
- Photos
- Personal file names
- Software license keys
- Banking, medical, or private document contents
- Screen captures
- Keystrokes
- Full customer content

## Local History Storage

SignalScan stores local-only scan history at:

```text
%LOCALAPPDATA%\909 Signal IT\SignalScan\scan-history.json
```

The history file stores only minimal metadata:

- Scan timestamp
- Client name
- Device/computer name
- Overall status
- Recommended service package
- Exported PDF path, if available
- Exported Markdown path, if available

The history file does not store full diagnostic findings, technician notes, generated summaries, passwords, browser history, emails, personal file names, photos, license keys, or sensitive personal content.

## No Cloud, Network, or Telemetry

SignalScan v1.0.0 does not include:

- Cloud sync
- Telemetry
- Analytics
- External AI calls
- API keys
- Licensing checks
- Payment processing
- Auto-update checks
- Background monitoring
- Remote execution

Diagnostic data stays on the technician's local Windows machine unless the technician intentionally exports and shares a report.

## Offline AI Draft Text

The v1.0.0 AI summary workflow is offline only.

`OfflineAiSummaryProvider` uses the already-collected in-memory scan result to generate deterministic draft text. It does not call an external model, send data over the network, use an API key, or sync data to the cloud.

AI draft text is not a final diagnosis. The technician must review and edit summary and recommendation text before export or client sharing.

## Read-Only Safety Boundary

SignalScan v1.0.0 may inspect, summarize, and report. It must not automatically change the client system.

It does not:

- Repair issues
- Clean up files
- Delete files
- Write registry keys
- Modify services
- Modify drivers
- Change Defender or antivirus settings
- Change firewall settings
- Enable, disable, suspend, or resume BitLocker
- Manage user accounts
- Modify group policy
- Install updates
- Trigger Windows Update scans
- Modify startup items
- Remove malware

Registry access, where present, must remain read-only through `OpenSubKey(..., writable: false)`.

## Technician Review Required

Every report is a technician-reviewed work product. Before sharing with a client, the technician should verify:

- The scan ran on the intended device.
- Findings are reasonable for the device context.
- Unavailable checks are interpreted correctly.
- Offline draft text is edited for accuracy.
- Recommended service language is appropriate.
- The exported report does not include unintended sensitive information.
