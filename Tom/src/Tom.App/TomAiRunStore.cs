using System.Text.Json;

namespace Tom.App;

internal static class TomAiRunStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static void Save(TomWorkspace workspace, TomAiRunRecord record)
    {
        Directory.CreateDirectory(workspace.AiRunsPath);
        var safeAction = record.Action.ToString().ToLowerInvariant();
        var safeProvider = string.IsNullOrWhiteSpace(record.Provider) ? "unknown" : record.Provider;
        var fileName = $"{record.CreatedAt:yyyyMMdd-HHmmss}-{safeProvider}-{safeAction}.json";
        var path = Path.Combine(workspace.AiRunsPath, fileName);
        File.WriteAllText(path, JsonSerializer.Serialize(record, JsonOptions));
    }

    public static IReadOnlyList<TomAiRunRecord> LoadRecent(TomWorkspace workspace, int take)
    {
        if (!Directory.Exists(workspace.AiRunsPath)) return Array.Empty<TomAiRunRecord>();

        return Directory.EnumerateFiles(workspace.AiRunsPath, "*.json")
            .OrderByDescending(File.GetLastWriteTime)
            .Take(take)
            .Select(ReadRecord)
            .Where(record => record is not null)
            .Cast<TomAiRunRecord>()
            .OrderByDescending(record => record.CreatedAt)
            .ToList();
    }

    private static TomAiRunRecord? ReadRecord(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<TomAiRunRecord>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }
}
