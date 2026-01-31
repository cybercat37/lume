namespace Lume.Tests;

public static class GoldenFiles
{
    public static string Read(string fileName)
    {
        var root = FindRepoRoot(AppContext.BaseDirectory);
        var goldenPath = Path.Combine(root, "tests", "Lume.Tests", "Golden", fileName);
        return File.ReadAllText(goldenPath);
    }

    private static string FindRepoRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Lume.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
