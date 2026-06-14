using Microsoft.EntityFrameworkCore;

namespace Reva.Infrastructure.Persistence;

public sealed class RevaDbContext(DbContextOptions<RevaDbContext> options) : DbContext(options)
{
    public DbSet<DocumentRecord> Documents => Set<DocumentRecord>();
    public DbSet<ExportTemplateRecord> ExportTemplates => Set<ExportTemplateRecord>();

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

        modelBuilder.Entity<ReviewEventRecord>(entity =>
        {
            entity.Property(review => review.Decision).HasMaxLength(32);
            entity.Property(review => review.Reviewer).HasMaxLength(120);
            entity.Property(review => review.Notes).HasMaxLength(1000);
        });
    }
}

