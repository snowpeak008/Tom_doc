using System.IO.Compression;
using System.Text;

namespace Tom.App;

internal static class TomDiagnosticsService
{
    public static string CreatePackage(
        TomWorkspace workspace,
        TomDocumentStore store,
        string currentDocumentId,
        string statusText,
        IReadOnlyList<TomAiRunRecord> recentAiRuns)
    {
        Directory.CreateDirectory(workspace.LogsPath);
        var packagePath = Path.Combine(workspace.LogsPath, $"tom-diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.zip");

        using var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create);
        WriteText(archive, "status.txt", BuildStatus(workspace, currentDocumentId, statusText, recentAiRuns));
        AddIfExists(archive, store.GetTextPath(currentDocumentId), "current-document.txt");
        AddIfExists(archive, store.GetRtfPath(currentDocumentId), "current-document.rtf");

        if (Directory.Exists(workspace.AiRunsPath))
        {
            foreach (var file in Directory.EnumerateFiles(workspace.AiRunsPath, "*.json")
                         .OrderByDescending(File.GetLastWriteTime)
                         .Take(20))
            {
                archive.CreateEntryFromFile(file, $"ai-runs/{Path.GetFileName(file)}");
            }
        }

        if (Directory.Exists(workspace.SnapshotsPath))
        {
            foreach (var file in Directory.EnumerateFiles(workspace.SnapshotsPath)
                         .OrderByDescending(File.GetLastWriteTime)
                         .Take(20))
            {
                archive.CreateEntryFromFile(file, $"snapshots/{Path.GetFileName(file)}");
            }
        }

        return packagePath;
    }

    private static string BuildStatus(
        TomWorkspace workspace,
        string currentDocumentId,
        string statusText,
        IReadOnlyList<TomAiRunRecord> recentAiRuns)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Tom diagnostics");
        builder.AppendLine($"CreatedAt: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"ProjectPath: {workspace.ProjectPath}");
        builder.AppendLine($"Workspace: {workspace.RootPath}");
        builder.AppendLine($"CurrentDocumentId: {currentDocumentId}");
        builder.AppendLine($"Status: {statusText}");
        builder.AppendLine($"OS: {Environment.OSVersion}");
        builder.AppendLine($".NET: {Environment.Version}");
        builder.AppendLine($"Machine: {Environment.MachineName}");
        builder.AppendLine($"UserInteractive: {Environment.UserInteractive}");
        builder.AppendLine($"RecentAiRuns: {recentAiRuns.Count}");

        foreach (var run in recentAiRuns.Take(10))
        {
            builder.AppendLine($"- {run.CreatedAt:yyyy-MM-dd HH:mm:ss} {run.Provider} {run.Action} tokens~{run.EstimatedTokens} error={run.Error}");
        }

        return builder.ToString();
    }

    private static void WriteText(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        writer.Write(content);
    }

    private static void AddIfExists(ZipArchive archive, string sourcePath, string entryName)
    {
        if (File.Exists(sourcePath))
        {
            archive.CreateEntryFromFile(sourcePath, entryName);
        }
    }
}
