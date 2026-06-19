using Reva.Ai.Models;
using Reva.App.Services;
using Reva.App.ViewModels;
using Reva.Core.Contracts;
using Reva.Core.Export;
using Reva.Core.Settings;

namespace Reva.Unit;

public sealed class SettingsViewModelTests
{
    [Fact]
    public void ConstructorPopulatesThreeThemeOptions()
    {
        var vm = new SettingsViewModel(new NullRevaClient());

        Assert.Equal(3, vm.Themes.Count);
    }

    [Fact]
    public void ConstructorDefaultsSelectedThemeToLight()
    {
        var vm = new SettingsViewModel(new NullRevaClient());

        Assert.NotNull(vm.SelectedTheme);
        Assert.Equal(AppTheme.Light, vm.SelectedTheme!.Value);
    }

    [Fact]
    public void ModelOptionDisplayNameFallsBackToIdWhenBlank()
    {
        var descriptor = new ModelDescriptor("custom:7b", string.Empty, ModelKinds.Text, string.Empty, false);
        var option = new ModelOption(descriptor);

        Assert.Equal("custom:7b", option.DisplayName);
    }

    [Fact]
    public void ModelOptionDisplayNameUsesDescriptorNameWhenProvided()
    {
        var descriptor = new ModelDescriptor("qwen3-vl:8b", "Qwen3-VL 8B", ModelKinds.Vision, "note", false);
        var option = new ModelOption(descriptor);

        Assert.Equal("Qwen3-VL 8B", option.DisplayName);
    }

    [Fact]
    public void ModelOptionHasNotesIsFalseWhenNotesBlank()
    {
        var descriptor = new ModelDescriptor("id", "Name", ModelKinds.Text, string.Empty, false);
        var option = new ModelOption(descriptor);

        Assert.False(option.HasNotes);
    }

    [Fact]
    public void ModelOptionHasNotesIsTrueWhenNotesPresent()
    {
        var descriptor = new ModelDescriptor("id", "Name", ModelKinds.Text, "some note", true);
        var option = new ModelOption(descriptor);

        Assert.True(option.HasNotes);
    }

    [Fact]
    public void ModelOptionInstalledLabelReflectsFlag()
    {
        var installed = new ModelOption(new ModelDescriptor("a", "A", ModelKinds.Text, string.Empty, true));
        var notInstalled = new ModelOption(new ModelDescriptor("b", "B", ModelKinds.Text, string.Empty, false));

        Assert.Equal("Installed", installed.InstalledLabel);
        Assert.Equal("Not pulled", notInstalled.InstalledLabel);
    }

    [Fact]
    public void IsVisionToggleEnabledFalseWhenOllamaNotResolved()
    {
        var vm = new SettingsViewModel(new NullRevaClient());

        Assert.False(vm.IsVisionToggleEnabled);
    }

    [Fact]
    public async Task InitializeAsyncPopulatesModelsFromClient()
    {
        var descriptors = new[]
        {
            new ModelDescriptor("qwen3-vl:8b", "Qwen3-VL 8B", ModelKinds.Vision, string.Empty, true),
            new ModelDescriptor("llama4", "Llama 4", ModelKinds.Text, string.Empty, false)
        };
        var client = new StubModelsClient(descriptors, activeModel: "qwen3-vl:8b", ollamaOnline: false);
        var vm = new SettingsViewModel(client);

        await vm.InitializeAsync(CancellationToken.None);

        Assert.Equal(2, vm.Models.Count);
    }

    [Fact]
    public async Task InitializeAsyncSelectsActiveModelByIdWhenPresent()
    {
        var descriptors = new[]
        {
            new ModelDescriptor("llama4", "Llama 4", ModelKinds.Text, string.Empty, false),
            new ModelDescriptor("qwen3-vl:8b", "Qwen3-VL 8B", ModelKinds.Vision, string.Empty, true)
        };
        var client = new StubModelsClient(descriptors, activeModel: "qwen3-vl:8b", ollamaOnline: false);
        var vm = new SettingsViewModel(client);

        await vm.InitializeAsync(CancellationToken.None);

        Assert.NotNull(vm.SelectedModel);
        Assert.Equal("qwen3-vl:8b", vm.SelectedModel!.Id);
    }

    [Fact]
    public async Task InitializeAsyncFallsBackToInstalledModelWhenActiveIdMissing()
    {
        var descriptors = new[]
        {
            new ModelDescriptor("llama4", "Llama 4", ModelKinds.Text, string.Empty, false),
            new ModelDescriptor("qwen3-vl:8b", "Qwen3-VL 8B", ModelKinds.Vision, string.Empty, true)
        };
        var client = new StubModelsClient(descriptors, activeModel: null, ollamaOnline: false);
        var vm = new SettingsViewModel(client);

        await vm.InitializeAsync(CancellationToken.None);

        Assert.NotNull(vm.SelectedModel);
        Assert.Equal("qwen3-vl:8b", vm.SelectedModel!.Id);
    }

    [Fact]
    public async Task InitializeAsyncFallsBackToFirstModelWhenNoneInstalled()
    {
        var descriptors = new[]
        {
            new ModelDescriptor("llama4", "Llama 4", ModelKinds.Text, string.Empty, false),
            new ModelDescriptor("gemma4", "Gemma 4", ModelKinds.Text, string.Empty, false)
        };
        var client = new StubModelsClient(descriptors, activeModel: null, ollamaOnline: false);
        var vm = new SettingsViewModel(client);

        await vm.InitializeAsync(CancellationToken.None);

        Assert.NotNull(vm.SelectedModel);
        Assert.Equal("llama4", vm.SelectedModel!.Id);
    }

    [Fact]
    public async Task InitializeAsyncLoadsTemplatesIncludingNoneOption()
    {
        var templates = new[]
        {
            new ExportTemplate(Guid.NewGuid(), "Default CSV", ExportFormat.Csv, [], false)
        };
        var client = new StubModelsClient([], activeModel: null, ollamaOnline: false, templates: templates);
        var vm = new SettingsViewModel(client);

        await vm.InitializeAsync(CancellationToken.None);

        Assert.Equal(2, vm.Templates.Count);
        Assert.Null(vm.Templates[0].Id);
    }

    [Fact]
    public async Task InitializeAsyncSetsIsBusyFalseOnCompletion()
    {
        var vm = new SettingsViewModel(new NullRevaClient());

        await vm.InitializeAsync(CancellationToken.None);

        Assert.False(vm.IsBusy);
    }

    [Fact]
    public void ThemeOptionCarriesValueAndName()
    {
        var option = new ThemeOption(AppTheme.Dark, "Dark");

        Assert.Equal(AppTheme.Dark, option.Value);
        Assert.Equal("Dark", option.Name);
    }

    private sealed class NullRevaClient : IRevaClient
    {
        public Task<IReadOnlyList<DocumentSummary>> ListDocumentsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DocumentSummary>>([]);

        public Task<Reva.Core.Contracts.DocumentDetail?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Reva.Core.Contracts.DocumentDetail?>(null);

        public Task<Reva.Core.Contracts.BdxReviewPayload?> GetReviewPayloadAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Reva.Core.Contracts.BdxReviewPayload?>(null);

        public Task<DocumentUploadResult> UploadAsync(string filePath, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DocumentUploadResult> UploadAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Reva.Core.Contracts.DocumentDetail?> SaveReviewAsync(Guid id, ReviewDecision decision, CancellationToken cancellationToken = default)
            => Task.FromResult<Reva.Core.Contracts.DocumentDetail?>(null);

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

    private sealed class StubModelsClient(
        IReadOnlyList<ModelDescriptor> models,
        string? activeModel,
        bool ollamaOnline,
        IReadOnlyList<ExportTemplate>? templates = null) : IRevaClient
    {
        public Task<IReadOnlyList<DocumentSummary>> ListDocumentsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DocumentSummary>>([]);

        public Task<Reva.Core.Contracts.DocumentDetail?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Reva.Core.Contracts.DocumentDetail?>(null);

        public Task<Reva.Core.Contracts.BdxReviewPayload?> GetReviewPayloadAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Reva.Core.Contracts.BdxReviewPayload?>(null);

        public Task<DocumentUploadResult> UploadAsync(string filePath, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DocumentUploadResult> UploadAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Reva.Core.Contracts.DocumentDetail?> SaveReviewAsync(Guid id, ReviewDecision decision, CancellationToken cancellationToken = default)
            => Task.FromResult<Reva.Core.Contracts.DocumentDetail?>(null);

        public Task<IReadOnlyList<ExportTemplate>> ListTemplatesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(templates ?? (IReadOnlyList<ExportTemplate>)[]);

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
            => Task.FromResult(models);

        public Task<string?> GetActiveModelAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(activeModel);

        public Task SetActiveModelAsync(string modelId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<bool> IsOllamaAvailableAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(ollamaOnline);
    }
}
