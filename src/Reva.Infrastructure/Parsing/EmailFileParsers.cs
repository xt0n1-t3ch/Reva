using System.Globalization;
using System.Text;
using MimeKit;
using Reva.Core.Contracts;
using MsgStorage = MsgReader.Outlook.Storage;

namespace Reva.Infrastructure.Parsing;

// .eml via MimeKit. Body text plus every attachment, each attachment parsed recursively
// through the same router so a PDF or spreadsheet inside an email is fully understood.
public sealed class EmailFileParser(IDocumentParser router) : IFileParser
{
    public string Profile => "email-eml";

    public bool CanParse(string extension) => extension == ".eml";

    public async Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken)
    {
        var message = await MimeMessage.LoadAsync(filePath, cancellationToken);
        var text = new StringBuilder();
        text.AppendLine(CultureInfo.InvariantCulture, $"Subject: {message.Subject}");
        text.AppendLine(CultureInfo.InvariantCulture, $"From: {message.From}");
        text.AppendLine(CultureInfo.InvariantCulture, $"To: {message.To}");
        text.AppendLine(CultureInfo.InvariantCulture, $"Date: {message.Date:u}");
        text.AppendLine();
        text.AppendLine(message.TextBody ?? EmailSupport.StripHtml(message.HtmlBody) ?? string.Empty);

        var tables = new List<ExtractedTable>();
        var warnings = new List<string>();
        foreach (var attachment in message.Attachments.OfType<MimePart>())
        {
            if (attachment.Content is null)
            {
                continue;
            }

            var name = attachment.FileName ?? "attachment";
            await using var buffer = new MemoryStream();
            await attachment.Content.DecodeToAsync(buffer, cancellationToken);
            await EmailSupport.MergeAttachmentAsync(router, name, buffer.ToArray(), text, tables, warnings, cancellationToken);
        }

        var body = text.ToString().Trim();
        return ParseSupport.Build(Profile, "eml", body, body, tables, warnings);
    }
}

// Outlook .msg via MsgReader.
public sealed class MsgFileParser(IDocumentParser router) : IFileParser
{
    public string Profile => "email-msg";

    public bool CanParse(string extension) => extension == ".msg";

    public async Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken)
    {
        var text = new StringBuilder();
        var tables = new List<ExtractedTable>();
        var warnings = new List<string>();

        using (var message = new MsgStorage.Message(filePath))
        {
            text.AppendLine(CultureInfo.InvariantCulture, $"Subject: {message.Subject}");
            text.AppendLine(CultureInfo.InvariantCulture, $"From: {message.Sender?.Email}");
            text.AppendLine(CultureInfo.InvariantCulture, $"Date: {message.SentOn:u}");
            text.AppendLine();
            text.AppendLine(message.BodyText ?? EmailSupport.StripHtml(message.BodyHtml) ?? string.Empty);

            foreach (var attachment in message.Attachments.OfType<MsgStorage.Attachment>())
            {
                var name = attachment.FileName ?? "attachment";
                await EmailSupport.MergeAttachmentAsync(router, name, attachment.Data, text, tables, warnings, cancellationToken);
            }
        }

        var body = text.ToString().Trim();
        return ParseSupport.Build(Profile, "msg", body, body, tables, warnings);
    }
}

internal static class EmailSupport
{
    public static async Task MergeAttachmentAsync(
        IDocumentParser router,
        string fileName,
        byte[] data,
        StringBuilder text,
        List<ExtractedTable> tables,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var safeName = Path.GetFileName(fileName);
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-{safeName}");
        try
        {
            await File.WriteAllBytesAsync(tempPath, data, cancellationToken);
            var parsed = await router.ParseAsync(tempPath, cancellationToken);
            text.AppendLine();
            text.AppendLine(CultureInfo.InvariantCulture, $"--- Attachment: {safeName} ---");
            text.AppendLine(parsed.Text);
            tables.AddRange(parsed.Tables);
            warnings.AddRange(parsed.Warnings);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public static string? StripHtml(string? html) =>
        string.IsNullOrEmpty(html) ? html : System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ").Trim();
}
