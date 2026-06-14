using Microsoft.EntityFrameworkCore;
using Reva.Infrastructure.Persistence;
using Reva.Infrastructure.Persistence.DemoData;

namespace Reva.Infrastructure.Settings;

public sealed class DataMaintenance(RevaDbContext dbContext, IDocumentWorkflow workflow) : IDataMaintenance
{
    public async Task<int> ClearAllDocumentsAsync(CancellationToken cancellationToken)
    {
        // The FK cascade (configured on RevaDbContext) removes the child rows.
        return await dbContext.Documents.ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<bool> ReseedDemoAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Documents.AnyAsync(cancellationToken))
        {
            return false;
        }

        await DemoDocumentSeeder.SeedIfEmptyAsync(workflow, dbContext, cancellationToken);
        return true;
    }
}
