using System.Globalization;

namespace Axom.Tests;

[Collection("CliTests")]
public class CliInitTests
{
    [Fact]
    public void Init_creates_api_project_scaffold()
    {
        var tempDir = CreateTempDirectory();
        var projectPath = Path.Combine(tempDir, "myapp");
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "init", projectPath });

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            Assert.True(File.Exists(Path.Combine(projectPath, "main.axom")));
            Assert.True(File.Exists(Path.Combine(projectPath, "routes", "health_get.axom")));
            Assert.True(File.Exists(Path.Combine(projectPath, "routes", "users__id_int_get.axom")));
            Assert.True(File.Exists(Path.Combine(projectPath, "routes", "search_get.axom")));
            Assert.True(File.Exists(Path.Combine(projectPath, "routes", "request_info_get.axom")));
            Assert.True(File.Exists(Path.Combine(projectPath, "routes", "missing_get.axom")));
            Assert.True(File.Exists(Path.Combine(projectPath, "Dockerfile")));
            Assert.True(File.Exists(Path.Combine(projectPath, "docker-compose.yml")));
            Assert.True(File.Exists(Path.Combine(projectPath, "Makefile")));
            Assert.True(File.Exists(Path.Combine(projectPath, "api.http")));
            Assert.True(File.Exists(Path.Combine(projectPath, "scripts", "document-endpoints.ps1")));
            Assert.True(File.Exists(Path.Combine(projectPath, "ENDPOINTS.md")));
            Assert.True(File.Exists(Path.Combine(projectPath, ".gitignore")));
            Assert.True(File.Exists(Path.Combine(projectPath, "README.md")));
            Assert.Contains("Run Without Web", File.ReadAllText(Path.Combine(projectPath, "README.md")));
            Assert.Contains("Endpoint Documentation", File.ReadAllText(Path.Combine(projectPath, "README.md")));
            Assert.Contains("api.http", File.ReadAllText(Path.Combine(projectPath, "README.md")));
            Assert.Contains("dotnet tool install -g axom.cli", File.ReadAllText(Path.Combine(projectPath, "Dockerfile")));

            var checkExitCode = Axom.Cli.Program.Main(new[] { "check", Path.Combine(projectPath, "main.axom") });
            Assert.Equal(0, checkExitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Init_non_empty_directory_without_force_fails()
    {
        var tempDir = CreateTempDirectory();
        var projectPath = Path.Combine(tempDir, "myapp");
        Directory.CreateDirectory(projectPath);
        File.WriteAllText(Path.Combine(projectPath, "existing.txt"), "x");

        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "init", projectPath });

            Assert.Equal(1, exitCode);
            Assert.Contains("not empty", error.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(Path.Combine(projectPath, "existing.txt")));
            Assert.False(File.Exists(Path.Combine(projectPath, "main.axom")));
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            DeleteTempDirectory(tempDir);
        }
    }

    [Fact]
    public void Init_force_overwrites_scaffold_files()
    {
        var tempDir = CreateTempDirectory();
        var projectPath = Path.Combine(tempDir, "myapp");
        Directory.CreateDirectory(projectPath);
        File.WriteAllText(Path.Combine(projectPath, "main.axom"), "print \"old\"");

        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "init", projectPath, "--force" });

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            Assert.Contains("axom api bootstrap", File.ReadAllText(Path.Combine(projectPath, "main.axom")));
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            DeleteTempDirectory(tempDir);
        }
    }

    private static string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_init_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private static void DeleteTempDirectory(string tempDir)
    {
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, true);
        }
    }
}
