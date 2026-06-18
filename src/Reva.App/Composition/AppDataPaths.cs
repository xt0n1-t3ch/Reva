using System;
using System.IO;

namespace Reva.App.Composition;

public static class AppDataPaths
{
    private const string AppFolderName = "Reva";
    private const string DatabaseFileName = "reva.db";
    private const string UploadsFolderName = "uploads";

    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppFolderName);

    public static string DatabasePath { get; } = Path.Combine(Root, DatabaseFileName);

    public static string UploadRoot { get; } = Path.Combine(Root, UploadsFolderName);

    public static string ConnectionString { get; } = $"Data Source={DatabasePath}";

    public static void Ensure()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(UploadRoot);
    }
}
