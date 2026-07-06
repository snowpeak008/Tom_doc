using System.Text.Json;

namespace Tom.App;

internal static class TomTokenUsageParser
{
    private static readonly HashSet<string> InputNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "input_tokens",
        "inputTokens",
        "prompt_tokens",
        "promptTokens"
    };

    private static readonly HashSet<string> OutputNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "output_tokens",
        "outputTokens",
        "completion_tokens",
        "completionTokens"
    };

    private static readonly HashSet<string> CacheReadNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "cache_read_input_tokens",
        "cacheReadInputTokens",
        "cache_read_tokens",
        "cacheReadTokens",
        "cached_tokens",
        "cachedTokens"
    };

    private static readonly HashSet<string> CacheWriteNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "cache_creation_input_tokens",
        "cacheCreationInputTokens",
        "cache_write_tokens",
        "cacheWriteTokens"
    };

    private static readonly HashSet<string> TotalNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "total_tokens",
        "totalTokens"
    };

    public static TomTokenUsage Parse(string raw, int estimatedTokens)
    {
        var usage = new UsageAccumulator();
        foreach (var json in ExtractJsonCandidates(raw))
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                Collect(document.RootElement, usage);
            }
            catch
            {
                // Raw CLI output may contain progress lines mixed with JSONL events.
            }
        }

        var computedTotal = usage.TotalTokens;
        if (computedTotal <= 0)
        {
            computedTotal = usage.InputTokens + usage.OutputTokens + usage.CacheReadTokens + usage.CacheWriteTokens;
        }

        if (computedTotal <= 0)
        {
            return new TomTokenUsage(0, 0, 0, 0, estimatedTokens, -1D, "估算");
        }

        var cacheBase = usage.InputTokens + usage.CacheReadTokens + usage.CacheWriteTokens;
        var cacheHitRate = cacheBase > 0 ? usage.CacheReadTokens / (double)cacheBase : -1D;
        return new TomTokenUsage(
            usage.InputTokens,
            usage.OutputTokens,
            usage.CacheReadTokens,
            usage.CacheWriteTokens,
            computedTotal,
            cacheHitRate,
            "CLI");
    }

    private static IEnumerable<string> ExtractJsonCandidates(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) yield break;

        var trimmed = raw.Trim();
        if ((trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal)) ||
            (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal)))
        {
            yield return trimmed;
        }

        foreach (var line in raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("{", StringComparison.Ordinal) || line.StartsWith("[", StringComparison.Ordinal))
            {
                yield return line;
            }
        }
    }

    private static void Collect(JsonElement element, UsageAccumulator usage)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (TryReadPositiveInt(property.Value, out var value))
                    {
                        if (InputNames.Contains(property.Name)) usage.InputTokens = Math.Max(usage.InputTokens, value);
                        if (OutputNames.Contains(property.Name)) usage.OutputTokens = Math.Max(usage.OutputTokens, value);
                        if (CacheReadNames.Contains(property.Name)) usage.CacheReadTokens = Math.Max(usage.CacheReadTokens, value);
                        if (CacheWriteNames.Contains(property.Name)) usage.CacheWriteTokens = Math.Max(usage.CacheWriteTokens, value);
                        if (TotalNames.Contains(property.Name)) usage.TotalTokens = Math.Max(usage.TotalTokens, value);
                    }

                    Collect(property.Value, usage);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    Collect(item, usage);
                }

                break;
        }
    }

    private static bool TryReadPositiveInt(JsonElement element, out int value)
    {
        value = 0;
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number) && number > 0)
        {
            value = number;
            return true;
        }

        return false;
    }

    private sealed class UsageAccumulator
    {
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int CacheReadTokens { get; set; }
        public int CacheWriteTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
