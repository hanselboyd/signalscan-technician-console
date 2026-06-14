# Package Handoff Notes

Use these notes when handing a SignalScan v1.0.0 zip package to a tester or prospect.

## Release Format

- The v1.0.0 demo package is zip-only, not an installer.
- Extract the zip before running the app.
- Run `SignalScan.TechnicianConsole.exe` from the extracted folder.
- Do not run the app directly from inside the compressed zip.

## SmartScreen

- A SmartScreen warning is expected for an unsigned early build.
- Do not disable SmartScreen globally.
- For internal/demo testing, use `More info` -> `Run anyway` if you trust the build source.
- For broader distribution, plan code signing before sending the app to a wider audience.

## Local Data

SignalScan stores local scan-history metadata under:

```text
%LOCALAPPDATA%\909 Signal IT\SignalScan\scan-history.json
```

The history file is local to the Windows profile. It is not cloud sync, telemetry, licensing, payment, or auto-update data.

## Safety Reminder

SignalScan v1.0.0 is read-only. It does not repair, clean up, delete files, modify registry keys, change services, change drivers, alter Defender/firewall/BitLocker settings, manage users, change Windows Update, modify startup items, or change system settings.
