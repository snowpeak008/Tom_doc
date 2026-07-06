using System.Diagnostics;
using System.Text;

namespace Tom.App;

internal sealed record TomCliStatus(string Id, string Label, string Command, bool Available, string Version, string Message);

internal static class TomCliDetector
{
    public static async Task<IReadOnlyList<TomCliStatus>> DetectAsync(CancellationToken cancellationToken)
    {
        var codexTask = DetectOneAsync("codex", "Codex CLI", "codex", "--version", cancellationToken);
        var claudeTask = DetectOneAsync("claude", "Claude CLI", "claude", "-v", cancellationToken);
        return await Task.WhenAll(codexTask, claudeTask);
    }

    private static async Task<TomCliStatus> DetectOneAsync(
        string id,
        string label,
        string command,
        string versionArgs,
        CancellationToken cancellationToken)
    {
        Process? process = null;
        try
        {
            process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/d /s /c {command} {versionArgs}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            var text = FirstLine(stdout) ?? FirstLine(stderr) ?? string.Empty;

            if (process.ExitCode == 0)
            {
                return new TomCliStatus(id, label, command, true, text, text);
            }

            return new TomCliStatus(id, label, command, false, string.Empty, $"未检测到 {label}，AI 功能不可用。");
        }
        catch (OperationCanceledException)
        {
            KillIfRunning(process);
            return new TomCliStatus(id, label, command, false, string.Empty, $"{label} 检测超时。");
        }
        catch
        {
            KillIfRunning(process);
            return new TomCliStatus(id, label, command, false, string.Empty, $"未检测到 {label}，AI 功能不可用。");
        }
        finally
        {
            process?.Dispose();
        }
    }

    private static string? FirstLine(string value)
    {
        return value
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
    }

    private static void KillIfRunning(Process? process)
    {
        try
        {
            if (process is { HasExited: false })
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Detection must never block Tom startup because of a stuck CLI process.
        }
    }
}
