using Avalonia.Headless.XUnit;
using Reva.App.ViewModels;

namespace Reva.App.Tests;

public sealed class CopilotOfflineTests
{
    [AvaloniaFact]
    public async Task RefreshStatusWhenOllamaOfflineShowsOfflineHint()
    {
        var copilot = new CopilotViewModel(
            new SingleScopeFactory(),
            new FakeModelRegistry(online: false),
            new FakeAgentChatService());

        await copilot.RefreshStatusAsync();

        Assert.False(copilot.IsOnline);
        Assert.True(copilot.IsOffline);
        Assert.Equal("Model offline", copilot.ModelStatus);
        Assert.Equal(CopilotViewModel.OfflineNotice, copilot.Description);
    }

    [AvaloniaFact]
    public async Task RefreshStatusWhenOllamaOnlineShowsActiveModel()
    {
        var copilot = new CopilotViewModel(
            new SingleScopeFactory(),
            new FakeModelRegistry(online: true, activeModel: "qwen3-vl:8b"),
            new FakeAgentChatService());

        await copilot.RefreshStatusAsync();

        Assert.True(copilot.IsOnline);
        Assert.False(copilot.IsOffline);
        Assert.Equal("qwen3-vl:8b", copilot.ModelStatus);
    }

    [AvaloniaFact]
    public void OfflineNoticeIsNonEmptyGuidance()
    {
        Assert.False(string.IsNullOrWhiteSpace(CopilotViewModel.OfflineNotice));
        Assert.Contains("Ollama", CopilotViewModel.OfflineNotice, StringComparison.Ordinal);
    }
}
