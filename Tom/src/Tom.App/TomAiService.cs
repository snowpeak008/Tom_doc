using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Tom.App;

internal sealed class TomAiService
{
    public async Task<(TomAiResult Result, string Raw)> RunAsync(TomAiRequest request, CancellationToken cancellationToken)
    {
        var prompt = TomAiPromptBuilder.Build(request);
        var raw = request.Provider switch
        {
            "codex" => await RunProcessAsync("codex", new[]
            {
                "--ask-for-approval", "never",
                "exec",
                "--skip-git-repo-check",
                "--sandbox", "read-only",
                "--ephemeral",
                "--color", "never",
                "--json",
                "-"
            }, prompt, cancellationToken),
            "claude" => await RunProcessAsync("claude", new[]
            {
                "--print",
                "--output-format", "json",
                "--exclude-dynamic-system-prompt-sections",
                "--no-session-persistence",
                "--permission-mode", "dontAsk"
            }, prompt, cancellationToken),
            _ => throw new InvalidOperationException($"未知 AI Provider：{request.Provider}")
        };

        var normalized = request.Provider == "claude" ? NormalizeClaudeOutput(raw) : NormalizeCodexOutput(raw);
        var result = TomAiResultParser.Parse(normalized, request.PreferredOperation, request.TargetText);
        return (result, raw);
    }

    private static async Task<string> RunProcessAsync(
        string command,
        IReadOnlyList<string> args,
        string input,
        CancellationToken cancellationToken)
    {
        Process? process = null;
        try
        {
            process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/d /s /c " + command + " " + string.Join(" ", args.Select(QuoteArg)),
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardInputEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            process.Start();
            await process.StandardInput.WriteAsync(input.AsMemory(), cancellationToken);
            process.StandardInput.Close();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            var raw = $"{stdout}\n{stderr}".Trim();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(raw) ? $"{command} 执行失败。" : raw);
            }

            return raw;
        }
        catch (OperationCanceledException)
        {
            KillIfRunning(process);
            throw new TimeoutException($"{command} 执行超时。");
        }
        finally
        {
            process?.Dispose();
        }
    }

    private static string NormalizeClaudeOutput(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.String)
            {
                return result.GetString() ?? raw;
            }
        }
        catch
        {
            // Some versions may already print the requested JSON object.
        }

        return raw;
    }

    private static string NormalizeCodexOutput(string raw)
    {
        var candidates = new List<string>();
        foreach (var line in raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                CollectTextValues(doc.RootElement, candidates);
            }
            catch
            {
                if (line.Contains("\"after\"", StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(line);
                }
            }
        }

        return candidates.LastOrDefault(value =>
            value.Contains("\"operation\"", StringComparison.OrdinalIgnoreCase) &&
            value.Contains("\"after\"", StringComparison.OrdinalIgnoreCase)) ?? raw;
    }

    private static void CollectTextValues(JsonElement element, List<string> results)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var value = element.GetString();
                if (!string.IsNullOrWhiteSpace(value)) results.Add(value);
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray()) CollectTextValues(item, results);
                break;
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject()) CollectTextValues(property.Value, results);
                break;
        }
    }

    private static string QuoteArg(string arg)
    {
        return arg.Contains(' ') ? $"\"{arg.Replace("\"", "\\\"")}\"" : arg;
    }

    private static void KillIfRunning(Process? process)
    {
        try
        {
            if (process is { HasExited: false }) process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Tom should report the original AI failure, not process cleanup failure.
        }
    }
}
