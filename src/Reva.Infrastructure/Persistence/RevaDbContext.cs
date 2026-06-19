using Microsoft.EntityFrameworkCore;
using Reva.Core.Settings;

namespace Reva.Infrastructure.Persistence;

public sealed class RevaDbContext(DbContextOptions<RevaDbContext> options) : DbContext(options)
{
    public DbSet<DocumentRecord> Documents => Set<DocumentRecord>();
    public DbSet<ExportTemplateRecord> ExportTemplates => Set<ExportTemplateRecord>();
    public DbSet<AppSettingsRecord> AppSettings => Set<AppSettingsRecord>();
    public DbSet<LearnedSchemaMappingRecord> LearnedSchemaMappings => Set<LearnedSchemaMappingRecord>();
    public DbSet<DocumentSourceSpanRecord> DocumentSourceSpans => Set<DocumentSourceSpanRecord>();
    public DbSet<DocumentPageRecord> DocumentPages => Set<DocumentPageRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentRecord>(entity =>
        {
            entity.HasKey(document => document.Id);
            entity.HasIndex(document => document.Sha256Hash).IsUnique();
            entity.Property(document => document.FileName).HasMaxLength(260);
            entity.Property(document => document.Sha256Hash).HasMaxLength(64);
            entity.Property(document => document.Extension).HasMaxLength(16);
            entity.Property(document => document.Status).HasMaxLength(32);
            entity.Property(document => document.ReviewState).HasMaxLength(32);
            entity.Property(document => document.DocumentType).HasMaxLength(48);
            entity.Property(document => document.ParserProfile).HasMaxLength(80);
            entity.HasMany(document => document.Fields).WithOne().HasForeignKey(field => field.DocumentRecordId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(document => document.Tables).WithOne().HasForeignKey(table => table.DocumentRecordId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(document => document.SchemaMappings).WithOne().HasForeignKey(mapping => mapping.DocumentRecordId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(document => document.SourceSpans).WithOne().HasForeignKey(span => span.DocumentRecordId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(document => document.Pages).WithOne().HasForeignKey(page => page.DocumentRecordId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(document => document.Exceptions).WithOne().HasForeignKey(exception => exception.DocumentRecordId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(document => document.ReviewEvents).WithOne().HasForeignKey(review => review.DocumentRecordId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentFieldRecord>(entity =>
        {
            entity.Property(field => field.Name).HasMaxLength(96);
            entity.Property(field => field.Source).HasMaxLength(96);
        });

        modelBuilder.Entity<DocumentTableRecord>(entity =>
        {
            entity.Property(table => table.Name).HasMaxLength(96);
        });

        modelBuilder.Entity<DocumentSchemaMappingRecord>(entity =>
        {
            entity.Property(mapping => mapping.SenderKey).HasMaxLength(160);
            entity.Property(mapping => mapping.SourceHeader).HasMaxLength(160);
            entity.Property(mapping => mapping.NormalizedSourceHeader).HasMaxLength(160);
            entity.Property(mapping => mapping.CanonicalField).HasMaxLength(96);
            entity.Property(mapping => mapping.NormalizedValue).HasMaxLength(256);
            entity.Property(mapping => mapping.Source).HasMaxLength(32);
        });

        modelBuilder.Entity<DocumentSourceSpanRecord>(entity =>
        {
            entity.Property(span => span.SpanId).HasMaxLength(80);
            entity.Property(span => span.Text).HasMaxLength(2000);
            entity.Property(span => span.BlockId).HasMaxLength(80);
            entity.Property(span => span.TableId).HasMaxLength(80);
        });

        modelBuilder.Entity<DocumentPageRecord>(entity =>
        {
            entity.Property(page => page.ImagePath).HasMaxLength(512);
        });

        modelBuilder.Entity<DocumentIssueRecord>(entity =>
        {
            entity.Property(exception => exception.Severity).HasMaxLength(24);
            entity.Property(exception => exception.Message).HasMaxLength(512);
            entity.Property(exception => exception.FieldName).HasMaxLength(96);
            entity.Property(exception => exception.Detected).HasMaxLength(256);
            entity.Property(exception => exception.Expected).HasMaxLength(256);
        });

        modelBuilder.Entity<ExportTemplateRecord>(entity =>
        {
            entity.HasKey(template => template.Id);
            entity.Property(template => template.Name).HasMaxLength(120);
            entity.Property(template => template.Format).HasMaxLength(16);
        });

        modelBuilder.Entity<AppSettingsRecord>(entity =>
        {
            entity.HasKey(settings => settings.Id);
            entity.Property(settings => settings.Theme).HasMaxLength(16);
            entity.Property(settings => settings.AccentColor).HasMaxLength(16);
            entity.Property(settings => settings.ProductName).HasMaxLength(80);
            entity.Property(settings => settings.ReconciliationTolerance).HasDefaultValue(0.01);
            entity.Property(settings => settings.UseLlmAssist).HasDefaultValue(false);
            entity.Property(settings => settings.AiProvider).HasMaxLength(32).HasDefaultValue(AiProviderNames.Ollama);
            entity.Property(settings => settings.AiBaseUrl).HasMaxLength(512).HasDefaultValue(AiSettingsDefaults.OllamaBaseUrl);
            entity.Property(settings => settings.AiApiKey).HasMaxLength(2048);
            entity.Property(settings => settings.AiModel).HasMaxLength(256).HasDefaultValue(AiSettingsDefaults.DefaultModel);
        });

        modelBuilder.Entity<LearnedSchemaMappingRecord>(entity =>
        {
            entity.HasKey(mapping => mapping.Id);
            entity.HasIndex(mapping => new { mapping.SenderKey, mapping.NormalizedSourceHeader }).IsUnique();
            entity.Property(mapping => mapping.SenderKey).HasMaxLength(160);
            entity.Property(mapping => mapping.SourceHeader).HasMaxLength(160);
            entity.Property(mapping => mapping.NormalizedSourceHeader).HasMaxLength(160);
            entity.Property(mapping => mapping.CanonicalField).HasMaxLength(96);
            entity.Property(mapping => mapping.IsOverride).HasDefaultValue(false);
        });

        modelBuilder.Entity<ReviewEventRecord>(entity =>
        {
            entity.Property(review => review.Decision).HasMaxLength(32);
            entity.Property(review => review.Reviewer).HasMaxLength(120);
            entity.Property(review => review.Notes).HasMaxLength(1000);
        });
    }
}
