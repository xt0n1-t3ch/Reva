using System.Runtime.CompilerServices;

namespace Reva.Integration;

internal static class TestPaths
{
    public static string RepositoryRoot([CallerFilePath] string sourceFile = "")
    {
        foreach (var start in CandidateStarts(sourceFile))
        {
            var current = start;
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "Reva.slnx")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException("Repository root containing Reva.slnx was not found.");
    }

    public static string SamplePath(string name)
    {
        var candidate = Path.Combine(RepositoryRoot(), "samples", name);
        return File.Exists(candidate)
            ? candidate
            : throw new FileNotFoundException($"Sample file was not found: {name}.");
    }

    public static string ContractPath(string name)
    {
        var candidate = Path.Combine(RepositoryRoot(), "contracts", name);
        return File.Exists(candidate)
            ? candidate
            : throw new FileNotFoundException($"Contract file was not found: {name}.");
    }

    private static IEnumerable<DirectoryInfo> CandidateStarts(string sourceFile)
    {
        yield return new DirectoryInfo(AppContext.BaseDirectory);
        yield return new DirectoryInfo(Directory.GetCurrentDirectory());
        var sourceDirectory = Path.GetDirectoryName(sourceFile);
        if (!string.IsNullOrWhiteSpace(sourceDirectory))
        {
            yield return new DirectoryInfo(sourceDirectory);
        }
    }
}
