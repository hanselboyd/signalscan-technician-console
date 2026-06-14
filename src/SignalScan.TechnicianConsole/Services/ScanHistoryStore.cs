using System.IO;
using System.Text.Json;
using SignalScan.TechnicianConsole.Models;

namespace SignalScan.TechnicianConsole.Services;

public sealed class ScanHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public ScanHistoryStore()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        StoreDirectory = Path.Combine(localAppData, "909 Signal IT", "SignalScan");
        StorePath = Path.Combine(StoreDirectory, "scan-history.json");
    }

    public string StoreDirectory { get; }

    public string StorePath { get; }

    public async Task<IReadOnlyList<ScanHistoryRecord>> LoadAsync()
    {
        try
        {
            if (!File.Exists(StorePath))
            {
                return Array.Empty<ScanHistoryRecord>();
            }

            await using var stream = File.OpenRead(StorePath);
            var records = await JsonSerializer.DeserializeAsync<List<ScanHistoryRecord>>(stream, JsonOptions);
            return records?
                .OrderByDescending(record => record.ScanTimestamp)
                .ToArray() ?? Array.Empty<ScanHistoryRecord>();
        }
        catch
        {
            return Array.Empty<ScanHistoryRecord>();
        }
    }

    public async Task SaveOrUpdateAsync(ScanHistoryRecord record)
    {
        var records = (await LoadAsync()).ToList();
        var existingIndex = records.FindIndex(item => item.Id == record.Id);
        if (existingIndex >= 0)
        {
            records[existingIndex] = record;
        }
        else
        {
            records.Add(record);
        }

        Directory.CreateDirectory(StoreDirectory);
        await using var stream = File.Create(StorePath);
        await JsonSerializer.SerializeAsync(stream, records.OrderByDescending(item => item.ScanTimestamp).ToArray(), JsonOptions);
    }
}
