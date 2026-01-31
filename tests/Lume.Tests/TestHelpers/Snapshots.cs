using Lume.Compiler.Diagnostics;

namespace Lume.Tests;

public static class Snapshots
{
    public static string Read(string fileName)
    {
        var root = FindRepoRoot(AppContext.BaseDirectory);
        var path = Path.Combine(root, "tests", "Lume.Tests", "Snapshots", fileName);
        return File.ReadAllText(path);
    }

    public static string Format(IEnumerable<Diagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(d => d.ToString())) + Environment.NewLine;
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
