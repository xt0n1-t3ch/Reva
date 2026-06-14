using Reva.Core.Settings;

namespace Reva.Infrastructure.Settings;

public interface ISettingsStore
{
    Task<AppSettings> GetAsync(CancellationToken cancellationToken);

    // Persists the settings (sanitized) and refreshes RuntimeSettings. Returns the stored value.
    Task<AppSettings> SaveAsync(AppSettings settings, CancellationToken cancellationToken);
}
