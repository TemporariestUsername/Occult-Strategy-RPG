namespace PaleCommunion.Sim.Tests;

/// <summary>Locates repo files from the test output directory by walking up to the repo root.</summary>
internal static class TestPaths
{
    public static string RepoFile(string relative)
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "schemas", "event.schema.json")))
            {
                return Path.Combine(dir.FullName, relative);
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate repo root from {AppContext.BaseDirectory}");
    }

    public static string Content(string relative) => RepoFile(Path.Combine("content", relative));
}
