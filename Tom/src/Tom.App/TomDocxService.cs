using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace Tom.App;

internal static class TomDocxService
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public static string ImportText(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var entry = archive.GetEntry("word/document.xml")
            ?? throw new InvalidOperationException("DOCX 中缺少 word/document.xml。");

        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        var body = document.Root?.Element(W + "body")
            ?? throw new InvalidOperationException("DOCX 文档结构不完整。");

        var builder = new StringBuilder();
        foreach (var node in body.Elements())
        {
            if (node.Name == W + "p")
            {
                AppendParagraph(builder, node);
            }
            else if (node.Name == W + "tbl")
            {
                AppendTable(builder, node);
            }
        }

        return builder.ToString().TrimEnd();
    }

    public static void ExportText(string path, string title, string text)
    {
        if (File.Exists(path)) File.Delete(path);
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        AddText(archive, "[Content_Types].xml", ContentTypesXml());
        AddText(archive, "_rels/.rels", RootRelsXml());
        AddText(archive, "word/_rels/document.xml.rels", DocumentRelsXml());
        AddText(archive, "docProps/core.xml", CoreXml(title));
        AddText(archive, "docProps/app.xml", AppXml());
        AddText(archive, "word/document.xml", DocumentXml(text));
    }

    private static void AppendParagraph(StringBuilder builder, XElement paragraph)
    {
        var text = string.Concat(paragraph.Descendants(W + "t").Select(node => node.Value));
        if (paragraph.Descendants(W + "drawing").Any() || paragraph.Descendants(W + "pict").Any())
        {
            text += string.IsNullOrWhiteSpace(text) ? "[图片]" : " [图片]";
        }

        builder.AppendLine(text);
    }

    private static void AppendTable(StringBuilder builder, XElement table)
    {
        foreach (var row in table.Elements(W + "tr"))
        {
            var cells = row.Elements(W + "tc")
                .Select(cell => string.Join(" ", cell.Descendants(W + "t").Select(text => text.Value)).Trim())
                .ToArray();
            builder.AppendLine(string.Join("\t", cells));
        }
    }

    private static void AddText(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string ContentTypesXml()
    {
        return """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
  <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
  <Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
</Types>
""";
    }

    private static string RootRelsXml()
    {
        return """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
</Relationships>
""";
    }

    private static string DocumentRelsXml()
    {
        return """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"/>
""";
    }

    private static string CoreXml(string title)
    {
        return $"""
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <dc:title>{EscapeXml(title)}</dc:title>
  <dc:creator>Tom</dc:creator>
  <cp:lastModifiedBy>Tom</cp:lastModifiedBy>
  <dcterms:created xsi:type="dcterms:W3CDTF">{DateTime.UtcNow:O}</dcterms:created>
  <dcterms:modified xsi:type="dcterms:W3CDTF">{DateTime.UtcNow:O}</dcterms:modified>
</cp:coreProperties>
""";
    }

    private static string AppXml()
    {
        return """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes">
  <Application>Tom</Application>
</Properties>
""";
    }

    private static string DocumentXml(string text)
    {
        var body = new StringBuilder();
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            body.Append("<w:p><w:r><w:t xml:space=\"preserve\">");
            body.Append(EscapeXml(line));
            body.Append("</w:t></w:r></w:p>");
        }

        return $"""
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="{W}" xmlns:r="{R}">
  <w:body>
    {body}
    <w:sectPr><w:pgSz w:w="11906" w:h="16838"/><w:pgMar w:top="1440" w:right="1440" w:bottom="1440" w:left="1440"/></w:sectPr>
  </w:body>
</w:document>
""";
    }

    private static string EscapeXml(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
