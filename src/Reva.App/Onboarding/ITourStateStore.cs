namespace Reva.App.Onboarding;

public interface ITourStateStore
{
    bool HasSeenTour();

    void MarkTourSeen();
}
