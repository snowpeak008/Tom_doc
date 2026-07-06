namespace Tom.App;

internal sealed class TomDocumentStore
{
    public const string DefaultDocumentId = "current-document";

    private const string RtfExtension = ".rtf";
    private const string TextExtension = ".txt";
    private readonly TomWorkspace _workspace;

    public TomDocumentStore(TomWorkspace workspace)
    {
        _workspace = workspace;
    }

    public string RtfPath => GetRtfPath(DefaultDocumentId);

    public string TextPath => GetTextPath(DefaultDocumentId);

    public bool HasSavedDocument => File.Exists(RtfPath) || File.Exists(TextPath);

    public void Save(string rtf, string text)
    {
        Save(DefaultDocumentId, rtf, text);
    }

    public void Save(string documentId, string rtf, string text)
    {
        Directory.CreateDirectory(_workspace.DocumentsPath);
        File.WriteAllText(GetRtfPath(documentId), rtf);
        File.WriteAllText(GetTextPath(documentId), text);
    }

    public TomDocumentSnapshot Load()
    {
        return Load(DefaultDocumentId);
    }

    public TomDocumentSnapshot Load(string documentId)
    {
        var rtfPath = GetRtfPath(documentId);
        var textPath = GetTextPath(documentId);

        if (File.Exists(rtfPath))
        {
            return new TomDocumentSnapshot(File.ReadAllText(rtfPath), string.Empty, true);
        }

        if (File.Exists(textPath))
        {
            return new TomDocumentSnapshot(string.Empty, File.ReadAllText(textPath), false);
        }

        return TomDocumentSnapshot.Empty;
    }

    public IReadOnlyList<TomDocumentRecord> ListDocuments()
    {
        Directory.CreateDirectory(_workspace.DocumentsPath);

        var ids = Directory.EnumerateFiles(_workspace.DocumentsPath)
            .Where(path => string.Equals(Path.GetExtension(path), RtfExtension, StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(Path.GetExtension(path), TextExtension, StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(id => LastWriteTime(GetRtfPath(id), GetTextPath(id)))
            .ToList();

        return ids
            .Select(id => new TomDocumentRecord(
                id,
                ReadTitle(id),
                GetRtfPath(id),
                GetTextPath(id),
                LastWriteTime(GetRtfPath(id), GetTextPath(id))))
            .ToList();
    }

    public string CreateDocumentId(string title)
    {
        var safeTitle = SanitizeFileName(string.IsNullOrWhiteSpace(title) ? "tom-document" : title.Trim());
        return $"{DateTime.Now:yyyyMMdd-HHmmss}-{safeTitle}";
    }

    public bool Delete(string documentId)
    {
        var deleted = false;
        var rtfPath = GetRtfPath(documentId);
        var textPath = GetTextPath(documentId);

        if (File.Exists(rtfPath))
        {
            File.Delete(rtfPath);
            deleted = true;
        }

        if (File.Exists(textPath))
        {
            File.Delete(textPath);
            deleted = true;
        }

        return deleted;
    }

    public string GetRtfPath(string documentId)
    {
        return Path.Combine(_workspace.DocumentsPath, $"{documentId}{RtfExtension}");
    }

    public string GetTextPath(string documentId)
    {
        return Path.Combine(_workspace.DocumentsPath, $"{documentId}{TextExtension}");
    }

    private string ReadTitle(string documentId)
    {
        var textPath = GetTextPath(documentId);
        if (File.Exists(textPath))
        {
            var firstLine = File.ReadLines(textPath)
                .Select(line => line.Trim())
                .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
            if (!string.IsNullOrWhiteSpace(firstLine)) return firstLine.Length > 42 ? firstLine[..42] : firstLine;
        }

        return documentId == DefaultDocumentId ? "当前文档" : documentId;
    }

    private static DateTime LastWriteTime(string rtfPath, string textPath)
    {
        var rtfTime = File.Exists(rtfPath) ? File.GetLastWriteTime(rtfPath) : DateTime.MinValue;
        var textTime = File.Exists(textPath) ? File.GetLastWriteTime(textPath) : DateTime.MinValue;
        return rtfTime > textTime ? rtfTime : textTime;
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var cleaned = new string(value.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray()).Trim('-', ' ');
        if (cleaned.Length == 0) return "tom-document";
        return cleaned.Length > 48 ? cleaned[..48] : cleaned;
    }
}

internal sealed record TomDocumentSnapshot(string Rtf, string Text, bool IsRtf)
{
    public static TomDocumentSnapshot Empty { get; } = new(string.Empty, string.Empty, false);
}

internal sealed record TomDocumentRecord(
    string Id,
    string Title,
    string RtfPath,
    string TextPath,
    DateTime UpdatedAt)
{
    public override string ToString()
    {
        return $"{UpdatedAt:MM-dd HH:mm}  {Title}";
    }
}
