using Reva.App.Services;
using Reva.App.ViewModels;
using Reva.Ai.Models;
using Reva.Core.Contracts;
using Reva.Core.Documents;
using Reva.Core.Export;
using Reva.Core.Reinsurance;
using Reva.Core.Settings;

namespace Reva.Unit;

public sealed class DashboardViewModelTests
{
    [Fact]
    public async Task LoadAsyncWithEmptyListSetsHasDocumentsFalse()
    {
        var client = new StubRevaClient([]);
        var nav = new StubNavigationService();
        var vm = new DashboardViewModel(client, nav);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.False(vm.HasDocuments);
        Assert.Equal(0, vm.TotalDocuments);
    }

    [Fact]
    public async Task LoadAsyncSetsTotalDocumentsFromList()
    {
        var docs = new[]
        {
            MakeSummary(ReviewState.Pending, DocumentStatus.Extracted),
            MakeSummary(ReviewState.Approved, DocumentStatus.Extracted),
            MakeSummary(ReviewState.Rejected, DocumentStatus.Extracted)
        };
        var client = new StubRevaClient(docs);
        var nav = new StubNavigationService();
        var vm = new DashboardViewModel(client, nav);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(3, vm.TotalDocuments);
        Assert.True(vm.HasDocuments);
    }

    [Fact]
    public async Task LoadAsyncCountsNeedsReviewAsPendingAndNeedsCorrection()
    {
        var docs = new[]
        {
            MakeSummary(ReviewState.Pending, DocumentStatus.Extracted),
            MakeSummary(ReviewState.NeedsCorrection, DocumentStatus.Extracted),
            MakeSummary(ReviewState.Approved, DocumentStatus.Extracted)
        };
        var client = new StubRevaClient(docs);
        var nav = new StubNavigationService();
        var vm = new DashboardViewModel(client, nav);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.NeedsReviewCount);
    }

    [Fact]
    public async Task LoadAsyncSumsExceptionCounts()
    {
        var docs = new[]
        {
            MakeSummary(ReviewState.Pending, DocumentStatus.Extracted, exceptionCount: 3),
            MakeSummary(ReviewState.Approved, DocumentStatus.Extracted, exceptionCount: 2)
        };
        var client = new StubRevaClient(docs);
        var nav = new StubNavigationService();
        var vm = new DashboardViewModel(client, nav);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(5, vm.ExceptionCount);
    }

    [Fact]
    public async Task LoadAsyncClampsNegativeExceptionCountsToZero()
    {
        var docs = new[]
        {
            MakeSummary(ReviewState.Pending, DocumentStatus.Extracted, exceptionCount: -1),
            MakeSummary(ReviewState.Approved, DocumentStatus.Extracted, exceptionCount: 2)
        };
        var client = new StubRevaClient(docs);
        var nav = new StubNavigationService();
        var vm = new DashboardViewModel(client, nav);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.ExceptionCount);
    }

    [Fact]
    public async Task LoadAsyncBuildsFourPipelineStages()
    {
        var docs = new[]
        {
            MakeSummary(ReviewState.Approved, DocumentStatus.Extracted),
            MakeSummary(ReviewState.Pending, DocumentStatus.Extracted)
        };
        var client = new StubRevaClient(docs);
        var nav = new StubNavigationService();
        var vm = new DashboardViewModel(client, nav);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(4, vm.PipelineStages.Count);
    }

    [Fact]
    public async Task LoadAsyncPipelineUploadedMatchesTotalDocuments()
    {
        var docs = new[]
        {
            MakeSummary(ReviewState.Approved, DocumentStatus.Extracted),
            MakeSummary(ReviewState.Pending, DocumentStatus.Extracted)
        };
        var client = new StubRevaClient(docs);
        var nav = new StubNavigationService();
        var vm = new DashboardViewModel(client, nav);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.UploadedCount);
    }

    [Fact]
    public async Task LoadAsyncPipelineExtractedCountsOnlyExtractedStatus()
    {
        var docs = new[]
        {
            MakeSummary(ReviewState.Pending, DocumentStatus.Extracted),
            MakeSummary(ReviewState.Pending, DocumentStatus.Uploaded)
        };
        var client = new StubRevaClient(docs);
        var nav = new StubNavigationService();
        var vm = new DashboardViewModel(client, nav);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(1, vm.ExtractedCount);
    }

    [Fact]
    public async Task LoadAsyncReviewedCountsApprovedAndRejected()
    {
        var docs = new[]
        {
            MakeSummary(ReviewState.Approved, DocumentStatus.Extracted),
            MakeSummary(ReviewState.Rejected, DocumentStatus.Extracted),
            MakeSummary(ReviewState.Pending, DocumentStatus.Extracted)
        };
        var client = new StubRevaClient(docs);
        var nav = new StubNavigationService();
        var vm = new DashboardViewModel(client, nav);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.ReviewedCount);
    }

    [Fact]
    public async Task LoadAsyncExportedCountsOnlyApproved()
    {
        var docs = new[]
        {
            MakeSummary(ReviewState.Approved, DocumentStatus.Extracted),
            MakeSummary(ReviewState.Rejected, DocumentStatus.Extracted),
            MakeSummary(ReviewState.Approved, DocumentStatus.Extracted)
        };
        var client = new StubRevaClient(docs);
        var nav = new StubNavigationService();
        var vm = new DashboardViewModel(client, nav);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.ExportedCount);
    }

    [Fact]
    public async Task LoadAsyncRecentDocumentsLimitedToEight()
    {
        var docs = new DocumentSummary[12];
        for (var i = 0; i < docs.Length; i++)
        {
            docs[i] = MakeSummary(ReviewState.Pending, DocumentStatus.Extracted,
                updatedAt: DateTimeOffset.UtcNow.AddMinutes(-i));
        }
        var client = new StubRevaClient(docs);
        var nav = new StubNavigationService();
        var vm = new DashboardViewModel(client, nav);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(8, vm.RecentDocuments.Count);
    }

    [Fact]
    public async Task LoadAsyncRecentDocumentsSortedNewestFirst()
    {
        var older = MakeSummary(ReviewState.Pending, DocumentStatus.Extracted,
            updatedAt: DateTimeOffset.UtcNow.AddHours(-2));
        var newer = MakeSummary(ReviewState.Pending, DocumentStatus.Extracted,
            updatedAt: DateTimeOffset.UtcNow.AddMinutes(-5));
        var client = new StubRevaClient([older, newer]);
        var nav = new StubNavigationService();
        var vm = new DashboardViewModel(client, nav);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(newer.Id, vm.RecentDocuments[0].Id);
    }

    [Fact]
    public async Task LoadAsyncWhenClientThrowsProjectsEmptyState()
    {
        var client = new ThrowingRevaClient();
        var nav = new StubNavigationService();
        var vm = new DashboardViewModel(client, nav);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.False(vm.HasDocuments);
        Assert.Equal(0, vm.TotalDocuments);
    }

    [Fact]
    public async Task LoadAsyncSetsIsLoadingFalseAfterCompletion()
    {
        var client = new StubRevaClient([]);
        var nav = new StubNavigationService();
        var vm = new DashboardViewModel(client, nav);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task LoadAsyncFormatsAverageConfidenceAsDashWhenEmpty()
    {
        var client = new StubRevaClient([]);
        var nav = new StubNavigationService();
        var vm = new DashboardViewModel(client, nav);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal("—", vm.AverageConfidence);
    }

    [Fact]
    public async Task LoadAsyncFormatsAverageConfidenceAsPercentage()
    {
        var docs = new[]
        {
            MakeSummary(ReviewState.Pending, DocumentStatus.Extracted, confidence: 1.0),
            MakeSummary(ReviewState.Pending, DocumentStatus.Extracted, confidence: 0.0)
        };
        var client = new StubRevaClient(docs);
        var nav = new StubNavigationService();
        var vm = new DashboardViewModel(client, nav);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal("50%", vm.AverageConfidence);
    }

    private static DocumentSummary MakeSummary(
        ReviewState reviewState,
        DocumentStatus status,
        int exceptionCount = 0,
        double confidence = 0.9,
        DateTimeOffset? updatedAt = null) =>
        new(
            Guid.NewGuid(),
            "test.pdf",
            status,
            reviewState,
            ReinsuranceDocumentType.FacultativeSlip,
            confidence,
            exceptionCount,
            DateTimeOffset.UtcNow.AddDays(-1),
            updatedAt ?? DateTimeOffset.UtcNow);

    private sealed class StubRevaClient(IReadOnlyList<DocumentSummary> documents) : IRevaClient
    {
        public Task<IReadOnlyList<DocumentSummary>> ListDocumentsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(documents);

        public Task<DocumentDetail?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<DocumentDetail?>(null);

        public Task<BdxReviewPayload?> GetReviewPayloadAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<BdxReviewPayload?>(null);

        public Task<DocumentUploadResult> UploadAsync(string filePath, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DocumentUploadResult> UploadAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DocumentDetail?> SaveReviewAsync(Guid id, ReviewDecision decision, CancellationToken cancellationToken = default)
            => Task.FromResult<DocumentDetail?>(null);

        public Task<IReadOnlyList<ExportTemplate>> ListTemplatesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ExportTemplate>>([]);

        public Task<ExportTemplate?> GetTemplateAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<ExportTemplate?>(null);

        public Task<ExportTemplate> CreateTemplateAsync(ExportTemplateDraft draft, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ExportTemplate?> UpdateTemplateAsync(Guid id, ExportTemplateDraft draft, CancellationToken cancellationToken = default)
            => Task.FromResult<ExportTemplate?>(null);

        public Task<ExportTemplate?> DuplicateTemplateAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<ExportTemplate?>(null);

        public Task<bool> DeleteTemplateAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<ExportPreview?> PreviewExportAsync(Guid documentId, Guid templateId, CancellationToken cancellationToken = default)
            => Task.FromResult<ExportPreview?>(null);

        public Task<ExportFile?> ExportAsync(Guid documentId, Guid templateId, CancellationToken cancellationToken = default)
            => Task.FromResult<ExportFile?>(null);

        public Task<AppSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(AppSettings.Default);

        public Task<AppSettings> SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
            => Task.FromResult(settings);

        public Task<IReadOnlyList<ModelDescriptor>> ListModelsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ModelDescriptor>>([]);

        public Task<string?> GetActiveModelAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task SetActiveModelAsync(string modelId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<bool> IsOllamaAvailableAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }

    private sealed class ThrowingRevaClient : IRevaClient
    {
        public Task<IReadOnlyList<DocumentSummary>> ListDocumentsAsync(CancellationToken cancellationToken = default)
            => Task.FromException<IReadOnlyList<DocumentSummary>>(new InvalidOperationException("simulated failure"));

        public Task<DocumentDetail?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<DocumentDetail?>(null);

        public Task<BdxReviewPayload?> GetReviewPayloadAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<BdxReviewPayload?>(null);

        public Task<DocumentUploadResult> UploadAsync(string filePath, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DocumentUploadResult> UploadAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DocumentDetail?> SaveReviewAsync(Guid id, ReviewDecision decision, CancellationToken cancellationToken = default)
            => Task.FromResult<DocumentDetail?>(null);

        public Task<IReadOnlyList<ExportTemplate>> ListTemplatesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ExportTemplate>>([]);

        public Task<ExportTemplate?> GetTemplateAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<ExportTemplate?>(null);

        public Task<ExportTemplate> CreateTemplateAsync(ExportTemplateDraft draft, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ExportTemplate?> UpdateTemplateAsync(Guid id, ExportTemplateDraft draft, CancellationToken cancellationToken = default)
            => Task.FromResult<ExportTemplate?>(null);

        public Task<ExportTemplate?> DuplicateTemplateAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<ExportTemplate?>(null);

        public Task<bool> DeleteTemplateAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<ExportPreview?> PreviewExportAsync(Guid documentId, Guid templateId, CancellationToken cancellationToken = default)
            => Task.FromResult<ExportPreview?>(null);

        public Task<ExportFile?> ExportAsync(Guid documentId, Guid templateId, CancellationToken cancellationToken = default)
            => Task.FromResult<ExportFile?>(null);

        public Task<AppSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(AppSettings.Default);

        public Task<AppSettings> SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
            => Task.FromResult(settings);

        public Task<IReadOnlyList<ModelDescriptor>> ListModelsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ModelDescriptor>>([]);

        public Task<string?> GetActiveModelAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task SetActiveModelAsync(string modelId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<bool> IsOllamaAvailableAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }

    private sealed class StubNavigationService : INavigationService
    {
        public Reva.App.ViewModels.ViewModelBase? Current => null;

        public string? CurrentRoute => null;

#pragma warning disable CS0067
        public event Action<Reva.App.ViewModels.ViewModelBase>? CurrentChanged;
#pragma warning restore CS0067

        public void NavigateTo(string route) { }

        public void OpenDocument(Guid documentId) { }
    }
}
