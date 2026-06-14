using System.Management;
using Microsoft.Win32;
using SignalScan.TechnicianConsole.Models;

namespace SignalScan.TechnicianConsole.Services;

public sealed class SecurityCollector
{
    public SecurityScan Collect(SystemProfile profile)
    {
        var defender = GetWindowsDefenderStatus();
        var firewall = GetFirewallProfileStatus();
        var bitLocker = GetBitLockerStatus();
        var administrators = GetLocalAdministratorCount();
        var windowsSupport = GetWindowsSupportStatus(profile);

        var snapshot = new SecuritySnapshot(
            defender.DisplayValue,
            firewall.DisplayValue,
            bitLocker.DisplayValue,
            administrators.DisplayValue,
            windowsSupport.DisplayValue);

        var findings = new List<DiagnosticFinding>
        {
            new("Security", "Windows Defender status", defender.Status, defender.Details),
            new("Security", "Firewall profile status", firewall.Status, firewall.Details),
            new("Security", "BitLocker status", bitLocker.Status, bitLocker.Details),
            new("Security", "Local administrator accounts", administrators.Status, administrators.Details),
            new("Security", "Windows version support", windowsSupport.Status, windowsSupport.Details)
        };

        return new SecurityScan(snapshot, findings);
    }

    private static SecurityCheckResult GetWindowsDefenderStatus()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\Microsoft\Windows\Defender",
                "SELECT AMServiceEnabled, AntivirusEnabled, RealTimeProtectionEnabled, NISEnabled, AntivirusSignatureLastUpdated FROM MSFT_MpComputerStatus");
            using var results = searcher.Get();
            foreach (ManagementObject result in results)
            {
                using (result)
                {
                    var serviceEnabled = ReadBool(result, "AMServiceEnabled");
                    var antivirusEnabled = ReadBool(result, "AntivirusEnabled");
                    var realTimeEnabled = ReadBool(result, "RealTimeProtectionEnabled");
                    var nisEnabled = ReadBool(result, "NISEnabled");
                    var signatureDate = FormatManagementDate(result["AntivirusSignatureLastUpdated"]?.ToString());

                    var display = $"Service: {FormatBool(serviceEnabled)}, Antivirus: {FormatBool(antivirusEnabled)}, Real-time: {FormatBool(realTimeEnabled)}";
                    var status = serviceEnabled == true && antivirusEnabled == true && realTimeEnabled == true
                        ? HealthStatus.Good
                        : serviceEnabled == false || antivirusEnabled == false || realTimeEnabled == false
                            ? HealthStatus.Critical
                            : HealthStatus.ReviewRequired;
                    var details = $"Read-only Defender status. Network inspection: {FormatBool(nisEnabled)}. Signature last updated: {signatureDate}. No Defender settings were changed.";

                    return new SecurityCheckResult(display, status, details);
                }
            }

            return new SecurityCheckResult(
                DiagnosticValueFormatter.Unavailable,
                HealthStatus.ReviewRequired,
                "Windows Defender status was unavailable from the read-only Defender WMI provider. No Defender settings were changed.");
        }
        catch (Exception ex)
        {
            return Unavailable("Windows Defender status unavailable.", ex);
        }
    }

    private static SecurityCheckResult GetFirewallProfileStatus()
    {
        try
        {
            var profiles = new Dictionary<string, string>
            {
                ["Domain"] = ReadFirewallProfile("DomainProfile"),
                ["Private"] = ReadFirewallProfile("StandardProfile"),
                ["Public"] = ReadFirewallProfile("PublicProfile")
            };

            var display = string.Join("; ", profiles.Select(profile => $"{profile.Key}: {profile.Value}"));
            var unavailable = profiles.Values.Any(DiagnosticValueFormatter.IsUnavailable);
            var disabled = profiles.Values.Any(value => value.Equals("Off", StringComparison.OrdinalIgnoreCase));
            var status = disabled ? HealthStatus.Critical : unavailable ? HealthStatus.ReviewRequired : HealthStatus.Good;
            var details = $"Read-only firewall profile state from registry policy locations. {display}. No firewall settings or rules were changed.";

            return new SecurityCheckResult(display, status, details);
        }
        catch (Exception ex)
        {
            return Unavailable("Firewall profile status unavailable.", ex);
        }
    }

    private static string ReadFirewallProfile(string profileName)
    {
        const string basePath = @"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy";
        using var key = Registry.LocalMachine.OpenSubKey(@$"{basePath}\{profileName}", writable: false);
        var value = key?.GetValue("EnableFirewall");
        return value switch
        {
            1 => "On",
            0 => "Off",
            int intValue => intValue == 1 ? "On" : intValue == 0 ? "Off" : DiagnosticValueFormatter.Unavailable,
            _ => DiagnosticValueFormatter.Unavailable
        };
    }

    private static SecurityCheckResult GetBitLockerStatus()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\CIMV2\Security\MicrosoftVolumeEncryption",
                "SELECT DriveLetter, ProtectionStatus FROM Win32_EncryptableVolume");
            using var results = searcher.Get();
            var volumes = new List<string>();
            var anyProtected = false;
            var anyUnprotectedFixed = false;

            foreach (ManagementObject volume in results)
            {
                using (volume)
                {
                    var drive = DiagnosticValueFormatter.NormalizeUnavailable(volume["DriveLetter"]?.ToString());
                    var protection = Convert.ToUInt32(volume["ProtectionStatus"] ?? 0);
                    var statusText = protection switch
                    {
                        0 => "Off",
                        1 => "On",
                        2 => "Unknown",
                        _ => DiagnosticValueFormatter.Unavailable
                    };

                    volumes.Add($"{drive}: {statusText}");
                    anyProtected |= protection == 1;
                    anyUnprotectedFixed |= protection == 0 && !DiagnosticValueFormatter.IsUnavailable(drive);
                }
            }

            if (volumes.Count == 0)
            {
                return new SecurityCheckResult(
                    DiagnosticValueFormatter.Unavailable,
                    HealthStatus.ReviewRequired,
                    "BitLocker status was unavailable from the read-only MicrosoftVolumeEncryption WMI provider. No BitLocker settings were changed.");
            }

            var display = string.Join("; ", volumes);
            var status = anyProtected ? HealthStatus.Good : anyUnprotectedFixed ? HealthStatus.AttentionNeeded : HealthStatus.ReviewRequired;
            var details = $"Read-only BitLocker protection status. {display}. SignalScan did not enable, disable, suspend, or resume BitLocker.";

            return new SecurityCheckResult(display, status, details);
        }
        catch (Exception ex)
        {
            return Unavailable("BitLocker status unavailable.", ex);
        }
    }

    private static SecurityCheckResult GetLocalAdministratorCount()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT PartComponent FROM Win32_GroupUser WHERE GroupComponent=\"Win32_Group.Domain='" + Environment.MachineName + "',Name='Administrators'\"");
            using var results = searcher.Get();
            var count = results.Count;
            var display = count.ToString();
            var status = count switch
            {
                0 => HealthStatus.ReviewRequired,
                <= 2 => HealthStatus.Good,
                <= 5 => HealthStatus.AttentionNeeded,
                _ => HealthStatus.Critical
            };
            var details = $"Read-only local Administrators group member count: {count}. SignalScan did not add, remove, enable, disable, or modify user accounts.";

            return new SecurityCheckResult(display, status, details);
        }
        catch (Exception ex)
        {
            return Unavailable("Local administrator account count unavailable.", ex);
        }
    }

    private static SecurityCheckResult GetWindowsSupportStatus(SystemProfile profile)
    {
        if (!TryParseBaseWindowsBuild(profile.WindowsBuild, out var build))
        {
            return new SecurityCheckResult(
                DiagnosticValueFormatter.Unavailable,
                HealthStatus.ReviewRequired,
                $"Windows build could not be evaluated from value '{profile.WindowsBuild}'. No system settings were changed.");
        }

        var buildDisplay = BuildDisplay(build, profile.WindowsBuild);

        if (build < 19045)
        {
            return new SecurityCheckResult(
                buildDisplay,
                HealthStatus.Critical,
                "Windows build appears older than the final Windows 10 22H2 build baseline. Recommend technician review for support status and upgrade planning. No update or upgrade was started.");
        }

        if (build < 22000)
        {
            return new SecurityCheckResult(
                buildDisplay,
                HealthStatus.AttentionNeeded,
                "Windows 10 is nearing or past its mainstream support window depending on the current date and edition. Recommend technician review for lifecycle planning. No update or upgrade was started.");
        }

        return new SecurityCheckResult(
            buildDisplay,
            HealthStatus.Good,
            "Windows build appears to be Windows 11 or newer based on the build number. Lifecycle status should still be confirmed for the installed edition.");
    }

    private static bool TryParseBaseWindowsBuild(string windowsBuild, out int build)
    {
        build = 0;
        if (DiagnosticValueFormatter.IsUnavailable(windowsBuild))
        {
            return false;
        }

        var baseBuild = windowsBuild.Split('.', 2)[0].Trim();
        return int.TryParse(baseBuild, out build);
    }

    private static string BuildDisplay(int baseBuild, string reportedBuild)
    {
        if (string.Equals(baseBuild.ToString(), reportedBuild, StringComparison.OrdinalIgnoreCase))
        {
            return $"Build {baseBuild}";
        }

        return $"Build {baseBuild} (reported {reportedBuild})";
    }

    private static bool? ReadBool(ManagementBaseObject result, string propertyName)
    {
        var value = result[propertyName];
        return value is null ? null : Convert.ToBoolean(value);
    }

    private static string FormatBool(bool? value) =>
        value.HasValue ? (value.Value ? "On" : "Off") : DiagnosticValueFormatter.Unavailable;

    private static string FormatManagementDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DiagnosticValueFormatter.Unavailable;
        }

        try
        {
            return ManagementDateTimeConverter.ToDateTime(value).ToString("g");
        }
        catch
        {
            return value;
        }
    }

    private static SecurityCheckResult Unavailable(string message, Exception ex) =>
        new(DiagnosticValueFormatter.Unavailable, HealthStatus.ReviewRequired, $"{message} {ex.GetType().Name}: {ex.Message}");

    private sealed record SecurityCheckResult(string DisplayValue, HealthStatus Status, string Details);
}
