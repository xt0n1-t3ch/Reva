using Reva.Ai.Models;

namespace Reva.Ai;

public interface IModelRegistry
{
    Task<IReadOnlyList<ModelDescriptor>> ListAsync(CancellationToken ct);

    Task<string?> GetActiveModelAsync(CancellationToken ct);

    Task SetActiveModelAsync(string modelId, CancellationToken ct);

    Task<bool> IsOllamaAvailableAsync(CancellationToken ct);
}
