namespace Reva.App.Onboarding;

public sealed record TourStep(
    string Title,
    string Body,
    string? Route,
    string? TargetName,
    TourPlacement Placement = TourPlacement.Auto,
    bool RequiresCopilotOpen = false);
