namespace SignalScan.TechnicianConsole.Models;

public sealed record SecuritySnapshot(
    string WindowsDefenderStatus,
    string FirewallProfileStatus,
    string BitLockerStatus,
    string LocalAdministratorCount,
    string WindowsSupportStatus);
