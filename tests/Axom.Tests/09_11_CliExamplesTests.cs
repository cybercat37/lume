using System.Globalization;

namespace Axom.Tests;

[Collection("CliTests")]
public class CliExamplesTests
{
    [Fact]
    public void Run_interpolation_example_succeeds()
    {
        var repoRoot = FindRepoRoot();
        var filePath = Path.Combine(repoRoot, "examples", "interpolation-format.axom");
        var outDir = Path.Combine(Path.GetTempPath(), $"axom_cli_examples_{Guid.NewGuid():N}");
        Directory.CreateDirectory(outDir);

        var originalDirectory = Directory.GetCurrentDirectory();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Directory.SetCurrentDirectory(repoRoot);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "run", filePath, "--quiet", "--out", outDir });

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            Assert.True(File.Exists(Path.Combine(outDir, "Program.cs")));
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalDirectory);
            if (Directory.Exists(outDir))
            {
                Directory.Delete(outDir, true);
            }
        }
    }

    [Fact]
    public void Run_module_type_alias_example_succeeds()
    {
        var repoRoot = FindRepoRoot();
        var filePath = Path.Combine(repoRoot, "examples", "modules", "valid", "app", "from_import_type_alias.axom");
        var outDir = Path.Combine(Path.GetTempPath(), $"axom_cli_examples_{Guid.NewGuid():N}");
        Directory.CreateDirectory(outDir);

        var originalDirectory = Directory.GetCurrentDirectory();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Directory.SetCurrentDirectory(repoRoot);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "run", filePath, "--quiet", "--out", outDir });

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            Assert.True(File.Exists(Path.Combine(outDir, "Program.cs")));
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalDirectory);
            if (Directory.Exists(outDir))
            {
                Directory.Delete(outDir, true);
            }
        }
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Axom.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate repository root.");
    }
}
