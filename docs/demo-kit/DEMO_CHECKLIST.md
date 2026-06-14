# SignalScan Demo Checklist

Use this checklist for internal or prospect demos of SignalScan v1.0.0.

- [ ] Download the artifact zip from the GitHub Actions `Windows Release Package` workflow.
- [ ] If Windows marks the zip as downloaded, open file properties and unblock the zip before extracting.
- [ ] Extract the zip to the Desktop or another local demo folder.
- [ ] Launch `SignalScan.TechnicianConsole.exe` from the extracted folder.
- [ ] If SmartScreen appears for the unsigned early build, handle it safely for internal/demo testing with `More info` -> `Run anyway`.
- [ ] Do not disable SmartScreen globally.
- [ ] Run `Run Read-Only Scan`.
- [ ] Generate `Offline Draft Summary`.
- [ ] Review and edit the technician summary before export.
- [ ] Export the PDF report.
- [ ] Export the Markdown draft.
- [ ] Save demo outputs in a separate local folder outside the repository.
- [ ] Do not use real client personal data in public demos.
- [ ] Confirm the report says SignalScan is read-only and performs no repairs or system changes.
