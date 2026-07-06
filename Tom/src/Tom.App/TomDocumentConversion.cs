using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Tom.App;

internal static class TomDocumentConversion
{
    public static string PlainTextToRtf(string text)
    {
        using var box = new RichTextBox();
        box.Text = text;
        return box.Rtf ?? string.Empty;
    }

    public static string MarkdownToText(string markdown)
    {
        return markdown
            .Replace("\r\n", "\n")
            .Replace("**", string.Empty)
            .Replace("__", string.Empty)
            .Replace("`", string.Empty);
    }

    public static string HtmlToText(string html)
    {
        var normalized = Regex.Replace(html, "(?i)<\\s*br\\s*/?>", "\n");
        normalized = Regex.Replace(normalized, "(?i)</\\s*p\\s*>", "\n\n");
        normalized = Regex.Replace(normalized, "<[^>]+>", string.Empty);
        return WebUtility.HtmlDecode(normalized).Trim();
    }

    public static string TextToHtml(string text)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"zh-CN\"><head><meta charset=\"utf-8\"><title>Tom Document</title>");
        builder.AppendLine("<style>body{font-family:'Microsoft YaHei',sans-serif;line-height:1.65;font-size:11pt;color:#1f2933;} p{margin:0 0 8px;} </style>");
        builder.AppendLine("</head><body>");

        foreach (var paragraph in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                builder.AppendLine("<p>&nbsp;</p>");
            }
            else
            {
                builder.Append("<p>");
                builder.Append(WebUtility.HtmlEncode(paragraph));
                builder.AppendLine("</p>");
            }
        }

        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    public static string TextToMarkdown(string text)
    {
        return text.Replace("\r\n", "\n");
    }
}
