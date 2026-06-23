namespace PaleCommunion.Sim.Tests;

/// <summary>Locates repo content from the test output directory by walking up the tree.</summary>
internal static class TestPaths
{
    public static string Content(string relative)
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            string candidate = Path.Combine(dir.FullName, "content", relative);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate content/{relative} from {AppContext.BaseDirectory}");
    }
}
