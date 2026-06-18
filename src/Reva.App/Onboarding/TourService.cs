using System;
using System.Collections.Generic;
using Avalonia.Threading;
using Reva.App.Services;
using Reva.App.ViewModels;

namespace Reva.App.Onboarding;

public sealed class TourService : ITourService
{
    private readonly INavigationService _navigation;
    private readonly ITourStateStore _stateStore;
    private readonly Func<ShellViewModel?> _shellAccessor;

    private int _currentIndex = -1;

    public TourService(
        INavigationService navigation,
        ITourStateStore stateStore,
        Func<ShellViewModel?> shellAccessor)
    {
        _navigation = navigation;
        _stateStore = stateStore;
        _shellAccessor = shellAccessor;
        Steps = TourScript.Steps;
    }

    public bool IsRunning { get; private set; }

    public int CurrentIndex => _currentIndex;

    public int StepCount => Steps.Count;

    public TourStep? CurrentStep =>
        _currentIndex >= 0 && _currentIndex < Steps.Count ? Steps[_currentIndex] : null;

    public IReadOnlyList<TourStep> Steps { get; }

    public event Action? StepChanged;

    public event Action? Stopped;

    public void Start()
    {
        if (Steps.Count == 0)
        {
            return;
        }

        IsRunning = true;
        MoveTo(0);
    }

    public void StartIfFirstRun()
    {
        if (_stateStore.HasSeenTour())
        {
            return;
        }

        Start();
    }

    public void Advance()
    {
        if (!IsRunning)
        {
            return;
        }

        if (_currentIndex >= Steps.Count - 1)
        {
            Finish();
            return;
        }

        MoveTo(_currentIndex + 1);
    }

    public void Back()
    {
        if (!IsRunning || _currentIndex <= 0)
        {
            return;
        }

        MoveTo(_currentIndex - 1);
    }

    public void Skip() => Finish();

    private void MoveTo(int index)
    {
        if (index < 0 || index >= Steps.Count)
        {
            return;
        }

        _currentIndex = index;
        ApplyStepContext(Steps[index]);
        StepChanged?.Invoke();
    }

    private void ApplyStepContext(TourStep step)
    {
        var shell = _shellAccessor();
        if (shell is not null)
        {
            shell.IsCopilotOpen = step.RequiresCopilotOpen;
        }

        if (!string.IsNullOrWhiteSpace(step.Route))
        {
            _navigation.NavigateTo(step.Route!);
        }
    }

    private void Finish()
    {
        if (!IsRunning)
        {
            return;
        }

        IsRunning = false;
        _currentIndex = -1;
        _stateStore.MarkTourSeen();

        if (Dispatcher.UIThread.CheckAccess())
        {
            Stopped?.Invoke();
            return;
        }

        Dispatcher.UIThread.Post(() => Stopped?.Invoke());
    }
}
