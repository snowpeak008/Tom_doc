namespace Tom.App;

internal enum TomAiAction
{
    Expand,
    Beautify,
    Summarize,
    Requirements,
    Review,
    Custom
}

internal sealed record TomAiRequest(
    TomAiAction Action,
    string Provider,
    string Instruction,
    string TargetText,
    string FullText,
    string PreferredOperation);

internal sealed record TomAiResult(
    string Operation,
    string Before,
    string After,
    string Summary);

internal sealed record TomAiRunRecord(
    DateTime CreatedAt,
    TomAiAction Action,
    string Provider,
    string Summary,
    string Before,
    string After,
    string Raw,
    string Error,
    int InputCharacters = 0,
    int OutputCharacters = 0,
    int EstimatedTokens = 0,
    int InputTokens = 0,
    int OutputTokens = 0,
    int CacheReadTokens = 0,
    int CacheWriteTokens = 0,
    int TotalTokens = 0,
    double CacheHitRate = -1,
    string TokenSource = "估算",
    string Status = "applied",
    string Scope = "unknown",
    string Instruction = "",
    string DocumentId = "");

internal sealed record TomTokenUsage(
    int InputTokens,
    int OutputTokens,
    int CacheReadTokens,
    int CacheWriteTokens,
    int TotalTokens,
    double CacheHitRate,
    string Source)
{
    public bool HasReportedUsage => Source != "估算";

    public string CacheHitRateText =>
        CacheHitRate < 0 ? "未知" : $"{CacheHitRate:P0}";

    public string CacheHitLevel
    {
        get
        {
            if (CacheHitRate < 0) return "未知";
            if (CacheHitRate >= 0.6D) return "高";
            if (CacheHitRate >= 0.25D) return "中";
            return "低";
        }
    }
}
