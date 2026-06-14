using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Reva.Core.Contracts;
using Reva.Core.Export;
using Reva.Infrastructure.Persistence;

namespace Reva.Infrastructure.Export;

public sealed class ExportTemplateStore(RevaDbContext dbContext) : IExportTemplateStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<ExportTemplate>> ListAsync(CancellationToken cancellationToken)
    {
        await EnsureSeededAsync(cancellationToken);
        var records = await dbContext.ExportTemplates.AsNoTracking().ToListAsync(cancellationToken);
        return records
            .Select(ToTemplate)
            // Built-in templates first, then by name, for a stable, friendly order.
            .OrderByDescending(template => template.IsBuiltIn)
            .ThenBy(template => template.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<ExportTemplate?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await EnsureSeededAsync(cancellationToken);
        var record = await dbContext.ExportTemplates.AsNoTracking().FirstOrDefaultAsync(template => template.Id == id, cancellationToken);
        return record is null ? null : ToTemplate(record);
    }

    public async Task<ExportTemplate> CreateAsync(ExportTemplateDraft draft, CancellationToken cancellationToken)
    {
        await EnsureSeededAsync(cancellationToken);
        var record = new ExportTemplateRecord
        {
            Id = Guid.NewGuid(),
            IsBuiltIn = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        ApplyDraft(record, draft);
        dbContext.ExportTemplates.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToTemplate(record);
    }

    public async Task<ExportTemplate?> UpdateAsync(Guid id, ExportTemplateDraft draft, CancellationToken cancellationToken)
    {
        var record = await dbContext.ExportTemplates.FirstOrDefaultAsync(template => template.Id == id, cancellationToken);
        if (record is null || record.IsBuiltIn)
        {
            return null;
        }

        ApplyDraft(record, draft);
        record.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToTemplate(record);
    }

    public async Task<ExportTemplate?> DuplicateAsync(Guid id, CancellationToken cancellationToken)
    {
        var source = await dbContext.ExportTemplates.AsNoTracking().FirstOrDefaultAsync(template => template.Id == id, cancellationToken);
        if (source is null)
        {
            return null;
        }

        var copy = new ExportTemplateRecord
        {
            Id = Guid.NewGuid(),
            Name = $"{source.Name} (copy)",
            Format = source.Format,
            ColumnsJson = source.ColumnsJson,
            IsBuiltIn = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        dbContext.ExportTemplates.Add(copy);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToTemplate(copy);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var record = await dbContext.ExportTemplates.FirstOrDefaultAsync(template => template.Id == id, cancellationToken);
        if (record is null || record.IsBuiltIn)
        {
            return false;
        }

        dbContext.ExportTemplates.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    // Idempotently ensures every built-in template exists (matched by its fixed Id). Safe to
    // call before any operation — user-created templates never block built-in seeding.
    private async Task EnsureSeededAsync(CancellationToken cancellationToken)
    {
        var existingIds = (await dbContext.ExportTemplates.Select(template => template.Id).ToListAsync(cancellationToken)).ToHashSet();

        var added = false;
        foreach (var template in ExportTemplateDefaults.All)
        {
            if (existingIds.Contains(template.Id))
            {
                continue;
            }

            dbContext.ExportTemplates.Add(new ExportTemplateRecord
            {
                Id = template.Id,
                Name = template.Name,
                Format = template.Format.ToString(),
                ColumnsJson = JsonSerializer.Serialize(template.Columns, SerializerOptions),
                IsBuiltIn = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            added = true;
        }

        if (!added)
        {
            return;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent request seeded first; that is fine.
        }
    }

    private static void ApplyDraft(ExportTemplateRecord record, ExportTemplateDraft draft)
    {
        record.Name = draft.Name.Trim();
        record.Format = draft.Format.ToString();
        record.ColumnsJson = JsonSerializer.Serialize(draft.Columns, SerializerOptions);
    }

    private static ExportTemplate ToTemplate(ExportTemplateRecord record)
    {
        var columns = JsonSerializer.Deserialize<List<ExportColumn>>(record.ColumnsJson, SerializerOptions) ?? [];
        var format = Enum.TryParse<ExportFormat>(record.Format, out var parsed) ? parsed : ExportFormat.Csv;
        return new ExportTemplate(record.Id, record.Name, format, columns, record.IsBuiltIn);
    }
}
