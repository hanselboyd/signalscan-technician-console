# SignalScan Product Specification

## Product Name

SignalScan

## Full Brand

SignalScan by 909 Signal IT

## Internal Codename

Horsepower

## Product Category

AI-assisted Windows PC diagnostic and client reporting utility.

## Business Purpose

SignalScan helps 909 Signal IT provide professional, repeatable, and client-friendly PC diagnostics. The tool should convert technical system findings into clear recommendations that help residential and small business clients understand what their computer needs.

## Target Users

### Primary User

909 Signal IT technician.

### Secondary User

Residential or small business client receiving the report.

## Version 1 Objective

Build a safe, read-only Windows diagnostic tool that collects system information, displays findings in a technician dashboard, and generates a branded client-facing report.

## Positioning Statement

SignalScan is an AI-assisted PC health reporting tool that helps clients understand system performance, maintenance needs, and security posture before paying for repair work.

## Key Product Rule

AI may explain and recommend, but it must not perform system changes automatically.

## MVP Features

### 1. Technician Dashboard

The dashboard should show:

- Client name
- Device name
- Scan date/time
- Overall health status
- System summary
- Performance findings
- Maintenance findings
- Security posture
- Technician notes
- Recommended service package

### 2. System Profile

Collect read-only data:

- Computer name
- Windows edition/version/build
- CPU model
- RAM amount
- Disk model/capacity/free space
- Device manufacturer/model where available
- BIOS/firmware version where available
- Current uptime
- Current logged-in user name, if safe and useful

### 3. Performance Check

Collect read-only indicators:

- CPU usage snapshot
- RAM usage snapshot
- Disk free percentage
- Startup app count
- Running process count
- High-level boot/startup pressure indicator
- Uptime/reboot needed indicator

### 4. Maintenance Check

Collect read-only indicators:

- Pending Windows updates where available
- Last update date where available
- Reboot required flag where available
- Estimated temporary file footprint, if collected safely
- Event log warning/error summary count
- Disk health status if available through safe system APIs

### 5. Security Posture

Collect read-only indicators:

- Windows Defender status
- Firewall status
- BitLocker status where available
- Local administrator account count
- Windows version support warning
- Basic security configuration flags

### 6. AI-Assisted Summary

The AI feature should generate:

- Plain-English client summary
- Key findings
- Risk explanation
- Recommended next step
- Suggested service package

The technician must be able to review and edit AI output before it appears in the final report.

### 7. PDF Report Export

Report should include:

- 909 Signal IT branding
- SignalScan name
- Client/device info
- Overall health status
- Findings by category
- Plain-English explanation
- Technician notes
- Recommended service
- Disclaimer
- Contact details

### 8. Local Scan History

Store locally:

- Scan timestamp
- Client/device name
- Overall status
- Key findings
- Exported report path
- Technician notes

Use SQLite or another simple local storage method.

## Explicit Non-Goals for Version 1

Do not include:

- File deletion
- Registry modification
- Service disabling
- Driver installation/removal
- Program uninstallation
- Malware removal claims
- Automatic cleanup
- Remote execution
- Background monitoring
- Cloud sync
- Payment processing
- Client self-service portal

## Service Packages to Support

The app should support recommendation labels:

- SignalScan PC Health Check
- SignalScan Tune-Up
- SignalScan Security Review
- SignalScan Business Workstation Assessment
- SignalCare Monthly Maintenance Plan

## Health Status Levels

Use simple labels:

- Good
- Attention Needed
- Critical
- Review Required

Avoid exaggerated scoring unless the scoring model is transparent.

## Recommended Status Logic

A system may be marked **Good** when there are no major performance, maintenance, or security warnings.

A system may be marked **Attention Needed** when it has moderate issues such as low disk space, high startup load, pending updates, or outdated maintenance.

A system may be marked **Critical** when it has severe disk pressure, disabled firewall/Defender, unsupported Windows version, repeated serious system errors, or other high-risk indicators.

A system may be marked **Review Required** when data is incomplete or unavailable.

## Branding Tone

Professional, local, trustworthy, and plain-English.

Avoid gimmicky language like:

- Supercharge your PC
- 500% faster
- Magic AI repair
- One-click fix everything

Preferred language:

- PC health check
- Technician-reviewed
- Clear diagnostics
- Plain-English report
- Maintenance recommendation
- Small business IT support

## Data Privacy Requirements

Version 1 should avoid collecting:

- Passwords
- Browser history
- Personal files
- Documents
- Photos
- Emails
- Full software license keys
- Sensitive client content

Diagnostics should focus on system health, not user content.

## Future Roadmap

### Version 2

- Safe cleanup checklist with technician approval
- Before/after report
- Service quote builder
- Maintenance plan reminders
- Better report customization

### Version 3

- Business device inventory
- Multi-device reports
- SignalCare recurring maintenance dashboard
- Cloud sync with consent
- Remote support integration

### Version 4

- Optional local AI model support
- Trend analysis
- Business risk scoring
- Agent-assisted service planning
