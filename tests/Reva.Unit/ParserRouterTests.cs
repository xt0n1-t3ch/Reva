using System.Text;
using ClosedXML.Excel;
using MimeKit;
using Reva.Infrastructure.Parsing;

namespace Reva.Unit;

public sealed class ParserRouterTests
{
    private static readonly ParserRouter Router = new();

    [Fact]
    public async Task ParsesExcelWorkbookIntoATable()
    {
        var path = TempPath(".xlsx");
        try
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Bordereau");
                sheet.Cell(1, 1).Value = "Cedent";
                sheet.Cell(1, 2).Value = "Premium";
                sheet.Cell(2, 1).Value = "Orion Insurance";
                sheet.Cell(2, 2).Value = "5550000";
                workbook.SaveAs(path);
            }

            var parsed = await Router.ParseAsync(path, CancellationToken.None);

            Assert.Equal("excel", parsed.ParserProfile);
            var table = Assert.Single(parsed.Tables);
            Assert.Contains("Cedent", table.Headers);
            Assert.Contains(table.Rows, row => row.TryGetValue("Cedent", out var value) && value == "Orion Insurance");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParsesEmailAndRecursivelyParsesAttachments()
    {
        var path = TempPath(".eml");
        try
        {
            var csv = "Cedent,Premium\nOrion Insurance,5550000\n"u8.ToArray();
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Broker", "broker@example.com"));
            message.To.Add(new MailboxAddress("Analyst", "analyst@example.com"));
            message.Subject = "January bordereau";
            var body = new TextPart("plain") { Text = "Please find the bordereau attached." };
            var attachment = new MimePart("text", "csv")
            {
                Content = new MimeContent(new MemoryStream(csv)),
                FileName = "bordereau.csv",
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment)
            };
            message.Body = new Multipart("mixed") { body, attachment };
            await using (var stream = File.Create(path))
            {
                await message.WriteToAsync(stream, CancellationToken.None);
            }

            var parsed = await Router.ParseAsync(path, CancellationToken.None);

            Assert.Equal("email-eml", parsed.ParserProfile);
            Assert.Contains("January bordereau", parsed.Text, StringComparison.Ordinal);
            // The CSV attachment was parsed recursively into a table.
            Assert.Contains(parsed.Tables, table => table.Headers.Contains("Cedent"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task UnknownBinaryFileFallsBackToVisibleTextWithoutThrowing()
    {
        var path = TempPath(".bin");
        try
        {
            var bytes = Encoding.Latin1.GetBytes("\x00\x01HELLO REINSURANCE 12345\xff\xfe");
            await File.WriteAllBytesAsync(path, bytes);

            var parsed = await Router.ParseAsync(path, CancellationToken.None);

            Assert.Equal("binary-fallback", parsed.ParserProfile);
            Assert.Contains("HELLO REINSURANCE", parsed.Text, StringComparison.Ordinal);
            Assert.NotEmpty(parsed.Warnings);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string TempPath(string extension) =>
        Path.Combine(Path.GetTempPath(), $"reva-test-{Guid.NewGuid():N}{extension}");
}
