using Reva.Core.Contracts;

namespace Reva.Infrastructure.Export;

public interface IExportTemplateStore
{
    Task<IReadOnlyList<ExportTemplate>> ListAsync(CancellationToken cancellationToken);

    Task<ExportTemplate?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<ExportTemplate> CreateAsync(ExportTemplateDraft draft, CancellationToken cancellationToken);

    // Returns null when the template does not exist or is built-in (built-ins are read-only).
    Task<ExportTemplate?> UpdateAsync(Guid id, ExportTemplateDraft draft, CancellationToken cancellationToken);

    Task<ExportTemplate?> DuplicateAsync(Guid id, CancellationToken cancellationToken);

    // Returns false when the template does not exist or is built-in.
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
