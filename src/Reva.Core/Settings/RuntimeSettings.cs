namespace Reva.Core.Settings;

// The single in-memory source of the current app settings, so static UI helpers (branding,
// confidence tiers) and the layout can read them without plumbing a service everywhere.
// Set once at startup and refreshed whenever settings are saved. Reads are a lock-free
// reference read; the value is an immutable record.
public static class RuntimeSettings
{
    private static volatile AppSettings _current = AppSettings.Default;

    public static AppSettings Current => _current;

    public static void Set(AppSettings settings) => _current = settings;
}
