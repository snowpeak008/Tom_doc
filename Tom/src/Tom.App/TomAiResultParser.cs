using System.Text.Json;
using System.Text.RegularExpressions;

namespace Tom.App;

internal static class TomAiResultParser
{
    public static TomAiResult Parse(string output, string expectedOperation, string expectedBefore)
    {
        var json = ExtractJson(output);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var operation = ReadRequiredString(root, "operation");
        var before = ReadRequiredString(root, "before");
        var after = ReadRequiredString(root, "after");
        var summary = ReadRequiredString(root, "summary");

        if (operation is not "replace-selection" and not "insert-at-cursor" and not "replace-document")
        {
            throw new InvalidOperationException($"AI 返回了不允许的 operation：{operation}");
        }

        if (string.IsNullOrWhiteSpace(after))
        {
            throw new InvalidOperationException("AI 返回的 after 为空。");
        }

        if (expectedOperation == "replace-selection" && operation != "replace-selection")
        {
            throw new InvalidOperationException("当前操作有目标文本，AI 必须返回 replace-selection。");
        }

        if (expectedOperation == "replace-selection" && before.Trim() != expectedBefore.Trim())
        {
            throw new InvalidOperationException("AI 返回的 before 与目标文本不一致，已阻止自动应用。");
        }

        GuardResult(after);
        return new TomAiResult(operation, before, after, summary);
    }

    private static string ReadRequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"AI JSON 缺少字段：{propertyName}");
        }

        return property.GetString() ?? string.Empty;
    }

    private static string ExtractJson(string output)
    {
        var text = Regex.Replace(output.Trim(), "^```(?:json)?\\s*|\\s*```$", string.Empty, RegexOptions.IgnoreCase);
        if (text.StartsWith("{") && text.EndsWith("}")) return text;

        var start = text.IndexOf('{');
        if (start < 0) throw new InvalidOperationException("AI 输出中没有 JSON 对象。");

        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var index = start; index < text.Length; index += 1)
        {
            var ch = text[index];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (ch == '"') inString = false;
                continue;
            }

            if (ch == '"')
            {
                inString = true;
                continue;
            }

            if (ch == '{') depth += 1;
            if (ch == '}') depth -= 1;

            if (depth == 0)
            {
                return text[start..(index + 1)];
            }
        }

        throw new InvalidOperationException("AI JSON 对象不完整。");
    }

    private static void GuardResult(string after)
    {
        var blockedTerms = new[]
        {
            "<script",
            "<html",
            "```",
            "powershell",
            "cmd.exe",
            "rm -",
            "del /"
        };

        foreach (var term in blockedTerms)
        {
            if (after.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"AI 输出包含不允许的内容：{term}");
            }
        }
    }
}
