namespace Reva.Infrastructure.Settings;

public interface IDataMaintenance
{
    // Deletes every document (and its fields, tables, checks, and review history).
    Task<int> ClearAllDocumentsAsync(CancellationToken cancellationToken);

    // Loads the demo corpus when the workspace is empty. Returns true if it seeded.
    Task<bool> ReseedDemoAsync(CancellationToken cancellationToken);
}
