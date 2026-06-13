using System.Text;
using Microsoft.EntityFrameworkCore;

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
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sample.Content));
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
    private static readonly IReadOnlyList<DemoSample> Samples =
    [
        new("operations-note.txt", "text/plain", OperationsNote),
        new("technical-account-statement.txt", "text/plain", TechnicalAccount),
        new("orion-property-cat-xl-jan-2025.csv", "text/csv", HeroBordereau),
    ];

    private const string HeroBordereau =
        "Cedent,Broker,Contract Reference,Line of Business,Period,Currency,Member,Premium,Claims,Commission,Net Ceded,Cession %\n" +
        "Orion Insurance Company Ltd.,Global Re Solutions,ORI-XL-2024-01,Property Cat XL,01 Jan 2025 - 31 Jan 2025,USD,North America,2450000,1125000,122500,1202500,47.72%\n" +
        "Orion Insurance Company Ltd.,Global Re Solutions,ORI-XL-2024-01,Property Cat XL,01 Jan 2025 - 31 Jan 2025,USD,Europe,1850000,875000,92500,882500,47.72%\n" +
        "Orion Insurance Company Ltd.,Global Re Solutions,ORI-XL-2024-01,Property Cat XL,01 Jan 2025 - 31 Jan 2025,USD,Asia Pacific,1250000,625000,62500,562500,47.72%\n";

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

    private sealed record DemoSample(string FileName, string ContentType, string Content);
}
