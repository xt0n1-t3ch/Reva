using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Reva.App.Services;
using Reva.App.ViewModels;
using Reva.Core.Documents;

namespace Reva.App.Tests;

public sealed class DashboardLoadTests
{
    [AvaloniaFact]
    public async Task LoadAsyncWithDocumentsProjectsKpisAndSetsHasDocuments()
    {
        var client = new FakeRevaClient
        {
            Documents =
            [
                TestData.Summary(ReviewState.Pending, DocumentStatus.Extracted, exceptionCount: 2, confidence: 1.0),
                TestData.Summary(ReviewState.NeedsCorrection, DocumentStatus.Extracted, exceptionCount: 1, confidence: 0.0),
                TestData.Summary(ReviewState.Approved, DocumentStatus.Uploaded, confidence: 1.0)
            ]
        };
        var vm = new DashboardViewModel(client, NoNavigation.Instance);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.True(vm.HasDocuments);
        Assert.Equal(3, vm.TotalDocuments);
        Assert.Equal(2, vm.NeedsReviewCount);
        Assert.Equal(3, vm.ExceptionCount);
        Assert.Equal("67%", vm.AverageConfidence);
    }

    [AvaloniaFact]
    public async Task LoadAsyncBuildsPipelineWithRealThemeBrushes()
    {
        var client = new FakeRevaClient
        {
            Documents =
            [
                TestData.Summary(ReviewState.Approved, DocumentStatus.Extracted),
                TestData.Summary(ReviewState.Pending, DocumentStatus.Extracted)
            ]
        };
        var vm = new DashboardViewModel(client, NoNavigation.Instance);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(4, vm.PipelineStages.Count);
        Assert.Equal(2, vm.UploadedCount);
        Assert.Equal(2, vm.ExtractedCount);
        Assert.Equal(1, vm.ReviewedCount);
        Assert.Equal(1, vm.ExportedCount);
        Assert.All(vm.PipelineStages, stage => Assert.IsAssignableFrom<IBrush>(stage.Accent));
        Assert.Contains(vm.PipelineStages, stage => stage.Accent is ISolidColorBrush solid && solid.Color != Colors.Transparent);
    }

    [AvaloniaFact]
    public async Task LoadAsyncProjectsRecentRowsNewestFirstCappedAtEight()
    {
        var documents = Enumerable.Range(0, 12)
            .Select(index => TestData.Summary(
                ReviewState.Pending,
                DocumentStatus.Extracted,
                updatedAt: DateTimeOffset.UtcNow.AddMinutes(-index)))
            .ToArray();
        var client = new FakeRevaClient { Documents = documents };
        var vm = new DashboardViewModel(client, NoNavigation.Instance);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(8, vm.RecentDocuments.Count);
        Assert.Equal(documents[0].Id, vm.RecentDocuments[0].Id);
        Assert.Equal("Facultative slip", vm.RecentDocuments[0].DocumentType);
    }

    [AvaloniaFact]
    public async Task LoadAsyncWithEmptyListReportsNoDocuments()
    {
        var vm = new DashboardViewModel(new FakeRevaClient(), NoNavigation.Instance);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.False(vm.HasDocuments);
        Assert.Equal(0, vm.TotalDocuments);
        Assert.Equal("—", vm.AverageConfidence);
        Assert.Empty(vm.RecentDocuments);
    }

    private sealed class NoNavigation : INavigationService
    {
        public static readonly NoNavigation Instance = new();

        public ViewModelBase? Current => null;

        public string? CurrentRoute => null;

#pragma warning disable CS0067
        public event Action<ViewModelBase>? CurrentChanged;
#pragma warning restore CS0067

        public void NavigateTo(string route)
        {
        }

        public void OpenDocument(Guid documentId)
        {
        }
    }
}
