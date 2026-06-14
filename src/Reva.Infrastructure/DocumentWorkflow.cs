using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Reva.Core.Contracts;
using Reva.Core.Documents;
using Reva.Core.Reinsurance;
using Reva.Infrastructure.Extraction;
using Reva.Infrastructure.Hashing;
using Reva.Infrastructure.Parsing;
using Reva.Infrastructure.Persistence;
using Reva.Infrastructure.Storage;

namespace Reva.Infrastructure;

public sealed class DocumentWorkflow(
    RevaDbContext dbContext,
    IDocumentHasher hasher,
    IDocumentStorage storage,
    IDocumentParser parser,
    IReinsuranceClassifier classifier,
    IReinsuranceExtractor extractor) : IDocumentWorkflow
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<DocumentUploadResult> IngestAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        ValidateFile(fileName, content);
        var sha256Hash = await hasher.ComputeSha256Async(content, cancellationToken);
        var duplicate = await dbContext.Documents.FirstOrDefaultAsync(document => document.Sha256Hash == sha256Hash, cancellationToken);
        if (duplicate is not null)
        {
            return new DocumentUploadResult(duplicate.Id, duplicate.FileName, duplicate.Sha256Hash, Enum.Parse<DocumentStatus>(duplicate.Status), duplicate.CreatedAt);
        }

        var now = DateTimeOffset.UtcNow;
        var documentId = Guid.NewGuid();
        var storedPath = await storage.SaveAsync(documentId, fileName, content, cancellationToken);
        var record = new DocumentRecord
        {
            Id = documentId,
            FileName = Path.GetFileName(fileName),
            Sha256Hash = sha256Hash,
            Extension = Path.GetExtension(fileName).ToLowerInvariant(),
            StoragePath = storedPath,
            Status = DocumentStatus.Uploaded.ToString(),
            ReviewState = ReviewState.Pending.ToString(),
            DocumentType = ReinsuranceDocumentType.Unknown.ToString(),
            Confidence = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Documents.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        await ParseAndExtractAsync(record, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DocumentUploadResult(record.Id, record.FileName, record.Sha256Hash, Enum.Parse<DocumentStatus>(record.Status), record.CreatedAt);
    }

    public async Task<IReadOnlyList<DocumentSummary>> ListAsync(CancellationToken cancellationToken)
    {
        var documents = await dbContext.Documents
            .AsNoTracking()
            .Include(document => document.Exceptions)
            .ToListAsync(cancellationToken);
        return documents
            .OrderByDescending(document => document.CreatedAt)
            .Select(ToSummary)
            .ToList();
    }

    public async Task<DocumentDetail?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await LoadDocumentAsync(id, cancellationToken);
        return document is null ? null : ToDetail(document);
    }

    public async Task<DocumentDetail?> ReviewAsync(Guid id, ReviewDecision decision, CancellationToken cancellationToken)
    {
        var document = await LoadDocumentAsync(id, cancellationToken);
        if (document is null)
        {
            return null;
        }

        foreach (var correction in decision.FieldCorrections)
        {
            var field = document.Fields.FirstOrDefault(item => item.Name == correction.Name);
            if (field is null)
            {
                document.Fields.Add(new DocumentFieldRecord
                {
                    Name = correction.Name,
                    Value = correction.Value,
                    Confidence = 1,
                    Source = "review",
                    IsCorrected = true
                });
                continue;
            }

            field.Value = correction.Value;
            field.Confidence = 1;
            field.Source = "review";
            field.IsCorrected = true;
        }

        document.ReviewState = decision.Decision switch
        {
            "Approve" => ReviewState.Approved.ToString(),
            "Reject" => ReviewState.Rejected.ToString(),
            _ => ReviewState.NeedsCorrection.ToString()
        };
        document.ReviewEvents.Add(new ReviewEventRecord
        {
            Decision = decision.Decision,
            Reviewer = decision.Reviewer,
            Notes = decision.Notes ?? string.Empty,
            CreatedAt = DateTimeOffset.UtcNow
        });
        document.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDetail(document);
    }

    public async Task<ExportRecord?> ExportAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await LoadDocumentAsync(id, cancellationToken);
        if (document is null)
        {
            return null;
        }

        // Raw export is always available, even for unknown/low-confidence documents.
        var fields = document.Fields.ToDictionary(field => field.Name, field => field.Value, StringComparer.OrdinalIgnoreCase);
        return new ExportRecord(
            document.Id,
            Enum.Parse<ReinsuranceDocumentType>(document.DocumentType),
            Enum.Parse<ReviewState>(document.ReviewState),
            fields,
            DateTimeOffset.UtcNow);
    }

    private async Task ParseAndExtractAsync(DocumentRecord record, CancellationToken cancellationToken)
    {
        try
        {
            var parsed = await parser.ParseAsync(record.StoragePath, cancellationToken);
            var classification = classifier.Classify(parsed);
            record.ParserProfile = parsed.ParserProfile;
            record.ParsedMarkdown = parsed.Markdown;
            record.ParsedJson = parsed.RawJson;

            // Always run best-effort extraction. Unknown/low-confidence documents are still
            // ingested as reviewable records (the extractor flags them) — never quarantined.
            var extraction = extractor.Extract(parsed, classification);
            record.Status = DocumentStatus.Extracted.ToString();
            record.DocumentType = extraction.DocumentType.ToString();
            record.Confidence = extraction.Confidence;
            record.Fields = extraction.Fields.Select(field => new DocumentFieldRecord
            {
                Name = field.Name,
                Value = field.Value,
                Confidence = field.Confidence,
                Source = field.Source,
                IsCorrected = field.IsCorrected
            }).ToList();
            record.Tables = extraction.Tables.Select(table => new DocumentTableRecord
            {
                Name = table.Name,
                HeadersJson = JsonSerializer.Serialize(table.Headers, SerializerOptions),
                RowsJson = JsonSerializer.Serialize(table.Rows, SerializerOptions)
            }).ToList();
            record.Exceptions = extraction.Exceptions.Select(exception => new DocumentIssueRecord
            {
                Severity = exception.Severity.ToString(),
                Message = exception.Message,
                FieldName = exception.FieldName,
                Detected = exception.Detected,
                Expected = exception.Expected,
                Confidence = exception.Confidence
            }).ToList();
            record.ErrorMessage = null;
            record.UpdatedAt = DateTimeOffset.UtcNow;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or TimeoutException or JsonException)
        {
            record.Status = DocumentStatus.Failed.ToString();
            record.ErrorMessage = ex.Message;
            record.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private static void ValidateFile(string fileName, Stream content)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("A file name is required.", nameof(fileName));
        }

        // No extension whitelist: any file type is accepted and parsed best-effort.
        // Only operational safety limits remain (empty + oversize).
        if (content.Length == 0)
        {
            throw new InvalidOperationException("The uploaded document is empty.");
        }

        if (content.Length > DocumentIntakePolicy.MaxFileBytes)
        {
            throw new InvalidOperationException("The uploaded document exceeds the size limit.");
        }
    }

    private Task<DocumentRecord?> LoadDocumentAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Documents
            .AsSplitQuery()
            .Include(document => document.Fields)
            .Include(document => document.Tables)
            .Include(document => document.Exceptions)
            .Include(document => document.ReviewEvents)
            .FirstOrDefaultAsync(document => document.Id == id, cancellationToken);
    }

    private static DocumentSummary ToSummary(DocumentRecord document)
    {
        return new DocumentSummary(
            document.Id,
            document.FileName,
            Enum.Parse<DocumentStatus>(document.Status),
            Enum.Parse<ReviewState>(document.ReviewState),
            Enum.Parse<ReinsuranceDocumentType>(document.DocumentType),
            document.Confidence,
            document.Exceptions.Count,
            document.CreatedAt,
            document.UpdatedAt);
    }

    private static DocumentDetail ToDetail(DocumentRecord document)
    {
        return new DocumentDetail(
            document.Id,
            document.FileName,
            document.Sha256Hash,
            Enum.Parse<DocumentStatus>(document.Status),
            Enum.Parse<ReviewState>(document.ReviewState),
            Enum.Parse<ReinsuranceDocumentType>(document.DocumentType),
            document.Confidence,
            document.ParsedMarkdown,
            document.ParserProfile,
            document.Fields.Select(field => new ExtractedField(field.Name, field.Value, field.Confidence, field.Source, field.IsCorrected)).ToList(),
            document.Tables.Select(ToExtractedTable).ToList(),
            document.Exceptions.Select(exception => new ExtractionIssue(
                Enum.Parse<ExceptionSeverity>(exception.Severity),
                exception.Message,
                exception.FieldName,
                exception.Detected,
                exception.Expected,
                exception.Confidence)).ToList(),
            document.CreatedAt,
            document.UpdatedAt);
    }

    private static ExtractedTable ToExtractedTable(DocumentTableRecord table)
    {
        var headers = JsonSerializer.Deserialize<IReadOnlyList<string>>(table.HeadersJson, SerializerOptions) ?? [];
        var rows = JsonSerializer.Deserialize<IReadOnlyList<Dictionary<string, string>>>(table.RowsJson, SerializerOptions) ?? [];
        return new ExtractedTable(table.Name, headers, rows.Select(row => (IReadOnlyDictionary<string, string>)row).ToList());
    }
}
