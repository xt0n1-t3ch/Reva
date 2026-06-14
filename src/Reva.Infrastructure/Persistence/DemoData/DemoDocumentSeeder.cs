using System.Text;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace Reva.Infrastructure.Persistence.DemoData;

// Seeds a small, realistic set of documents the first time the app runs against an empty
// database, so the cockpit is populated out of the box. The content is embedded (not read
// from disk) so it also works in the packaged single-file build. Each sample is ingested
// through the real workflow — no fabricated records.
public static class DemoDocumentSeeder
{
    public static async Task SeedIfEmptyAsync(IDocumentWorkflow workflow, RevaDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Documents.AnyAsync(cancellationToken))
        {
            return;
        }

        foreach (var sample in Samples)
        {
            try
            {
                using var stream = new MemoryStream(sample.Content);
                await workflow.IngestAsync(sample.FileName, sample.ContentType, stream, cancellationToken);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                // Demo seeding is best-effort; a failed sample must never block startup.
            }
        }
    }

    // Seeded oldest-first so the hero bordereau is the most recent — and therefore the
    // document the cockpit opens on by default.
    private static IReadOnlyList<DemoSample> Samples =>
    [
        new("operations-note.txt", "text/plain", Encoding.UTF8.GetBytes(OperationsNote)),
        new("technical-account-statement.txt", "text/plain", Encoding.UTF8.GetBytes(TechnicalAccount)),
        new("orion-property-cat-xl-jan-2025.eml", "message/rfc822", BuildHeroBordereauEmail()),
    ];

    // The hero document is a realistic broker submission: a cover-note email stating the
    // headline figures, with the line-item bordereau attached as a CSV. The stated totals
    // deliberately disagree with the attached line items (a very common real-world break),
    // so the reconciliation engine produces genuine field-level exceptions — the figures and
    // the agreement scores are computed from this data, never hardcoded.
    private static byte[] BuildHeroBordereauEmail()
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Global Re Solutions", "submissions@globalre.example"));
        message.To.Add(new MailboxAddress("Reve Intelligence Intake", "intake@reve.example"));
        message.Subject = "Orion Property Cat XL — January 2025 bordereau";

        var body = new TextPart("plain") { Text = HeroCoverNote };
        var attachment = new MimePart("text", "csv")
        {
            Content = new MimeContent(new MemoryStream(Encoding.UTF8.GetBytes(HeroLineItems))),
            FileName = "orion-property-cat-xl-jan-2025.csv",
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            ContentTransferEncoding = ContentEncoding.Base64
        };

        message.Body = new Multipart("mixed") { body, attachment };

        using var buffer = new MemoryStream();
        message.WriteTo(buffer);
        return buffer.ToArray();
    }

    // Stated headline figures. These intentionally do not reconcile with the attached
    // line items below, which is what the cockpit surfaces as field-level exceptions.
    private const string HeroCoverNote =
        "Dear Reve Intelligence team,\n\n" +
        "Please find attached the January 2025 property catastrophe bordereau for our cedent.\n\n" +
        "Cedent: Orion Insurance Company Ltd.\n" +
        "Broker: Global Re Solutions\n" +
        "Contract Ref: ORI-XL-2024-01\n" +
        "Line of Business: Prop Cat XL\n" +
        "Period: 01 Jan 2025 - 31 Jan 2025\n" +
        "Currency: USD\n" +
        "Premium: USD 4,400,000\n" +
        "Claims: USD 3,150,000\n" +
        "Commission: USD 332,000\n" +
        "Cession: 48.30%\n\n" +
        "Kind regards,\n" +
        "Global Re Solutions\n";

    // The authoritative line items. Column totals: Premium 5,550,000 · Claims 2,625,000 ·
    // Commission 277,500 · Net Ceded 2,647,500. Cession rate 47.72%.
    private const string HeroLineItems =
        "Member,Line of Business,Premium,Claims,Commission,Net Ceded,Cession %\n" +
        "North America,Property Cat XL,2450000,1125000,122500,1202500,47.72%\n" +
        "Europe,Property Cat XL,1850000,875000,92500,882500,47.72%\n" +
        "Asia Pacific,Property Cat XL,1250000,625000,62500,562500,47.72%\n";

    private const string TechnicalAccount =
        "Cedent: Andes Mutual Insurance\n" +
        "Broker: Meridian Re Brokers\n" +
        "Reinsurer: Active Capital Reinsurance\n" +
        "Contract Ref: AR-TREATY-2026-0042\n" +
        "Line of Business: Property & Engineering\n" +
        "Period: 2026-01-01 to 2026-03-31\n" +
        "Currency: USD\n" +
        "Premium: 1200000\n" +
        "Claims: 245000\n" +
        "Commission: 180000\n" +
        "Cession: 35%\n" +
        "Retention: 500000\n" +
        "Limit: 5000000\n" +
        "Technical Account Statement\n";

    // A non-reinsurance note: proves best-effort intake (ingested, low confidence, never rejected).
    private const string OperationsNote =
        "Internal note: please process the January submissions when you have a moment. Thanks, Operations team.\n";

    private sealed record DemoSample(string FileName, string ContentType, byte[] Content);
}
