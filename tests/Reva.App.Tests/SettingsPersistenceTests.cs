using Avalonia.Headless.XUnit;
using Reva.Ai.Models;
using Reva.App.ViewModels;
using Reva.Core.Settings;

namespace Reva.App.Tests;

public sealed class SettingsPersistenceTests
{
    [AvaloniaFact]
    public async Task InitializeAsyncSelectsActiveModelFromClient()
    {
        var client = new FakeRevaClient
        {
            Models =
            [
                new ModelDescriptor("llama4", "Llama 4", ModelKinds.Text, string.Empty, false),
                new ModelDescriptor("qwen3-vl:8b", "Qwen3-VL 8B", ModelKinds.Vision, string.Empty, true)
            ],
            OllamaOnline = true
        };
        await client.SetActiveModelAsync("qwen3-vl:8b");
        var vm = new SettingsViewModel(client);

        await vm.InitializeAsync(CancellationToken.None);

        Assert.Equal(2, vm.Models.Count);
        Assert.NotNull(vm.SelectedModel);
        Assert.Equal("qwen3-vl:8b", vm.SelectedModel!.Id);
    }

    [AvaloniaFact]
    public async Task SavePersistsSelectedModelAndSettings()
    {
        var client = new FakeRevaClient
        {
            Models =
            [
                new ModelDescriptor("llama4", "Llama 4", ModelKinds.Text, string.Empty, true),
                new ModelDescriptor("qwen3-vl:8b", "Qwen3-VL 8B", ModelKinds.Vision, string.Empty, true)
            ],
            OllamaOnline = true
        };
        var vm = new SettingsViewModel(client);
        await vm.InitializeAsync(CancellationToken.None);

        vm.SelectedModel = vm.Models.First(model => model.Id == "qwen3-vl:8b");
        vm.SelectedTheme = vm.Themes.First(theme => theme.Value == AppTheme.Dark);
        vm.ProductName = "Acme Reva";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("qwen3-vl:8b", client.LastSetModelId);
        Assert.NotNull(client.LastSavedSettings);
        Assert.Equal(AppTheme.Dark, client.LastSavedSettings!.Theme);
        Assert.Equal("Acme Reva", client.LastSavedSettings.ProductName);
        Assert.Equal("Settings saved.", vm.StatusMessage);
        Assert.True(vm.HasStatusMessage);
    }

    [AvaloniaFact]
    public async Task InitializeAsyncLoadsTemplatesWithLeadingNoneOption()
    {
        var client = new FakeRevaClient
        {
            Templates =
            [
                new Reva.Core.Contracts.ExportTemplate(
                    Guid.NewGuid(),
                    "Default CSV",
                    Reva.Core.Export.ExportFormat.Csv,
                    [],
                    true)
            ]
        };
        var vm = new SettingsViewModel(client);

        await vm.InitializeAsync(CancellationToken.None);

        Assert.Equal(2, vm.Templates.Count);
        Assert.Null(vm.Templates[0].Id);
        Assert.Equal("Default CSV", vm.Templates[1].Name);
    }
}
