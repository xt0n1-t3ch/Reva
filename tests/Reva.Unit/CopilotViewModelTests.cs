using Reva.App.ViewModels;

namespace Reva.Unit;

public sealed class CopilotViewModelTests
{
    [Fact]
    public void CopilotMessageViewModelUserRoleIsUser()
    {
        var msg = new CopilotMessageViewModel(CopilotRole.User, "hello");

        Assert.True(msg.IsUser);
        Assert.False(msg.IsAssistant);
    }

    [Fact]
    public void CopilotMessageViewModelAssistantRoleIsAssistant()
    {
        var msg = new CopilotMessageViewModel(CopilotRole.Assistant, string.Empty);

        Assert.True(msg.IsAssistant);
        Assert.False(msg.IsUser);
    }

    [Fact]
    public void CopilotMessageViewModelAssistantWithEmptyTextIsStreaming()
    {
        var msg = new CopilotMessageViewModel(CopilotRole.Assistant, string.Empty);

        Assert.True(msg.IsStreaming);
    }

    [Fact]
    public void CopilotMessageViewModelUserIsNotStreaming()
    {
        var msg = new CopilotMessageViewModel(CopilotRole.User, "query");

        Assert.False(msg.IsStreaming);
    }

    [Fact]
    public void AppendTextAccumulatesText()
    {
        var msg = new CopilotMessageViewModel(CopilotRole.Assistant, string.Empty);

        msg.AppendText("Hello");
        msg.AppendText(", world");

        Assert.Equal("Hello, world", msg.Text);
    }

    [Fact]
    public void AppendTextIgnoresEmptyDelta()
    {
        var msg = new CopilotMessageViewModel(CopilotRole.Assistant, "start");

        msg.AppendText(string.Empty);

        Assert.Equal("start", msg.Text);
    }

    [Fact]
    public void CompleteStopsStreaming()
    {
        var msg = new CopilotMessageViewModel(CopilotRole.Assistant, string.Empty);

        msg.Complete();

        Assert.False(msg.IsStreaming);
    }

    [Fact]
    public void MarkCancelledWritesCancelledTextWhenEmpty()
    {
        var msg = new CopilotMessageViewModel(CopilotRole.Assistant, string.Empty);

        msg.MarkCancelled();

        Assert.Equal("Cancelled.", msg.Text);
    }

    [Fact]
    public void MarkCancelledDoesNotOverwriteExistingText()
    {
        var msg = new CopilotMessageViewModel(CopilotRole.Assistant, string.Empty);
        msg.AppendText("partial answer");

        msg.MarkCancelled();

        Assert.Equal("partial answer", msg.Text);
    }

    [Fact]
    public void AddStepReturnsStepWithDisplayName()
    {
        var msg = new CopilotMessageViewModel(CopilotRole.Assistant, string.Empty);

        var step = msg.AddStep("call-1", "list_documents", string.Empty);

        Assert.Equal("list documents", step.DisplayName);
    }

    [Fact]
    public void AddStepAppearsInStepsCollection()
    {
        var msg = new CopilotMessageViewModel(CopilotRole.Assistant, string.Empty);

        msg.AddStep("call-1", "list_documents", string.Empty);

        Assert.True(msg.HasSteps);
        Assert.Single(msg.Steps);
    }

    [Fact]
    public void CompleteStepMarksMatchingStepComplete()
    {
        var msg = new CopilotMessageViewModel(CopilotRole.Assistant, string.Empty);
        msg.AddStep("call-1", "get_document", string.Empty);

        msg.CompleteStep("call-1", "success");

        Assert.True(msg.Steps[0].IsComplete);
        Assert.Equal("success", msg.Steps[0].Result);
    }

    [Fact]
    public void CompleteStepWithUnknownCallIdDoesNotThrow()
    {
        var msg = new CopilotMessageViewModel(CopilotRole.Assistant, string.Empty);

        var ex = Record.Exception(() => msg.CompleteStep("unknown", "result"));

        Assert.Null(ex);
    }

    [Fact]
    public void CopilotStepViewModelDefaultsToRunning()
    {
        var step = new CopilotStepViewModel("my_tool", "arg=value");

        Assert.False(step.IsComplete);
        Assert.Equal("Running", step.StatusText);
    }

    [Fact]
    public void CopilotStepViewModelMarkCompleteUpdatesState()
    {
        var step = new CopilotStepViewModel("my_tool", string.Empty);

        step.MarkComplete("ok");

        Assert.True(step.IsComplete);
        Assert.Equal("Done", step.StatusText);
        Assert.Equal("ok", step.Result);
        Assert.True(step.HasResult);
    }

    [Fact]
    public void CopilotStepViewModelHasArgumentsIsFalseWhenEmpty()
    {
        var step = new CopilotStepViewModel("my_tool", string.Empty);

        Assert.False(step.HasArguments);
    }

    [Fact]
    public void CopilotStepViewModelHasArgumentsIsTrueWhenNonEmpty()
    {
        var step = new CopilotStepViewModel("my_tool", "id=abc");

        Assert.True(step.HasArguments);
    }

    [Fact]
    public void CopilotStepViewModelFallsBackToDefaultToolNameWhenBlank()
    {
        var step = new CopilotStepViewModel(string.Empty, string.Empty);

        Assert.Equal("tool", step.Name);
    }

    [Fact]
    public void CopilotStepViewModelDisplayNameReplacesUnderscoresWithSpaces()
    {
        var step = new CopilotStepViewModel("save_review_decision", string.Empty);

        Assert.Equal("save review decision", step.DisplayName);
    }
}
