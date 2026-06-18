using System;
using System.Collections.Generic;

namespace Reva.App.Onboarding;

public interface ITourService
{
    bool IsRunning { get; }

    int CurrentIndex { get; }

    int StepCount { get; }

    TourStep? CurrentStep { get; }

    IReadOnlyList<TourStep> Steps { get; }

    event Action? StepChanged;

    event Action? Stopped;

    void Start();

    void StartIfFirstRun();

    void Advance();

    void Back();

    void Skip();
}
