namespace Tom.App;

internal sealed record TomWorkspace(
    string ProjectPath,
    string RootPath,
    string DocumentsPath,
    string AssetsPath,
    string ExportsPath,
    string SnapshotsPath,
    string AiRunsPath,
    string LogsPath);

internal static class TomWorkspaceService
{
    private const string TomWorkspaceFolderName = "tom-docs";
    private const string LegacyWorkspaceFolderName = "jam-docs";

    public static TomWorkspace CreateForProject(string projectPath)
    {
        var root = Path.Combine(projectPath, TomWorkspaceFolderName);
        MigrateLegacyWorkspace(projectPath, root);

        var workspace = new TomWorkspace(
            projectPath,
            root,
            Path.Combine(root, "documents"),
            Path.Combine(root, "assets"),
            Path.Combine(root, "exports"),
            Path.Combine(root, "snapshots"),
            Path.Combine(root, "ai-runs"),
            Path.Combine(root, "logs"));

        Ensure(workspace);
        return workspace;
    }

    public static TomWorkspace CreateDefault()
    {
        return CreateForProject(AppContext.BaseDirectory);
    }

    private static void MigrateLegacyWorkspace(string projectPath, string targetRoot)
    {
        var legacyRoot = Path.Combine(projectPath, LegacyWorkspaceFolderName);
        if (!Directory.Exists(legacyRoot) || Directory.Exists(targetRoot)) return;

        Directory.Move(legacyRoot, targetRoot);
    }

    private static void Ensure(TomWorkspace workspace)
    {
        Directory.CreateDirectory(workspace.RootPath);
        Directory.CreateDirectory(workspace.DocumentsPath);
        Directory.CreateDirectory(workspace.AssetsPath);
        Directory.CreateDirectory(workspace.ExportsPath);
        Directory.CreateDirectory(workspace.SnapshotsPath);
        Directory.CreateDirectory(workspace.AiRunsPath);
        Directory.CreateDirectory(workspace.LogsPath);

        var readme = Path.Combine(workspace.RootPath, "README.txt");
        if (!File.Exists(readme))
        {
            File.WriteAllText(readme, """
Tom document workspace

This folder is created by Tom.

documents  - Tom editable document data
assets     - imported images and related assets
exports    - exported files
snapshots  - document snapshots
ai-runs    - AI run records
logs       - local diagnostics
""");
        }
    }
}
