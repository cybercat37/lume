using System.Globalization;

namespace Axom.Tests;

[Collection("CliTests")]
public class CliExamplesTests
{
    [Fact]
    public void Run_interpolation_example_succeeds()
    {
        var repoRoot = FindRepoRoot();
        var filePath = Path.Combine(repoRoot, "examples", "010_interpolation-format.axom");
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

    [Fact]
    public void Db_verify_sql_template_example_succeeds()
    {
        var repoRoot = FindRepoRoot();
        var filePath = Path.Combine(repoRoot, "examples", "040_db-verify-params.axom");

        var originalDirectory = Directory.GetCurrentDirectory();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Directory.SetCurrentDirectory(repoRoot);
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--report" });

            Assert.Equal(0, exitCode);
            Assert.Contains("total_queries_validated=1", output.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [Fact]
    public void Db_verify_sql_record_projection_example_succeeds_with_projection_mapping()
    {
        var repoRoot = FindRepoRoot();
        var filePath = Path.Combine(repoRoot, "examples", "041_db-verify-record.axom");

        var originalDirectory = Directory.GetCurrentDirectory();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var previousProjections = Environment.GetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS");
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Environment.SetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS", "User:id,name");
            Directory.SetCurrentDirectory(repoRoot);
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--report" });

            Assert.Equal(0, exitCode);
            Assert.Contains("total_queries_validated=1", output.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            Environment.SetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS", previousProjections);
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [Fact]
    public void Check_db_transaction_example_succeeds()
    {
        var repoRoot = FindRepoRoot();
        var filePath = Path.Combine(repoRoot, "examples", "042_db-transaction-run.axom");

        var originalDirectory = Directory.GetCurrentDirectory();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Directory.SetCurrentDirectory(repoRoot);
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "check", filePath, "--quiet" });

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [Fact]
    public void Run_db_transaction_example_succeeds_with_sqlite_bootstrap()
    {
        var repoRoot = FindRepoRoot();
        var filePath = Path.Combine(repoRoot, "examples", "042_db-transaction-run.axom");
        var outDir = Path.Combine(Path.GetTempPath(), $"axom_cli_examples_{Guid.NewGuid():N}");
        var dbPath = Path.Combine(Path.GetTempPath(), $"axom_cli_examples_tx_{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(outDir);

        var previousProvider = Environment.GetEnvironmentVariable("AXOM_DB_PROVIDER");
        var previousConnection = Environment.GetEnvironmentVariable("AXOM_DB_CONNECTION_STRING");
        var originalDirectory = Directory.GetCurrentDirectory();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", "sqlite");
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", $"Data Source={dbPath}");
            Directory.SetCurrentDirectory(repoRoot);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "run", filePath, "--quiet", "--out", outDir });

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            Assert.True(File.Exists(Path.Combine(outDir, "Program.cs")));
        }
        finally
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", previousProvider);
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", previousConnection);
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalDirectory);

            if (Directory.Exists(outDir))
            {
                Directory.Delete(outDir, true);
            }

            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public void Run_transaction_block_example_succeeds_with_sqlite_bootstrap()
    {
        var repoRoot = FindRepoRoot();
        var filePath = Path.Combine(repoRoot, "examples", "044_transaction-block-minimal.axom");
        var outDir = Path.Combine(Path.GetTempPath(), $"axom_cli_examples_{Guid.NewGuid():N}");
        var dbPath = Path.Combine(Path.GetTempPath(), $"axom_cli_examples_txblock_{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(outDir);

        var previousProvider = Environment.GetEnvironmentVariable("AXOM_DB_PROVIDER");
        var previousConnection = Environment.GetEnvironmentVariable("AXOM_DB_CONNECTION_STRING");
        var originalDirectory = Directory.GetCurrentDirectory();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", "sqlite");
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", $"Data Source={dbPath}");
            Directory.SetCurrentDirectory(repoRoot);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "run", filePath, "--quiet", "--out", outDir });

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            Assert.True(File.Exists(Path.Combine(outDir, "Program.cs")));
        }
        finally
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", previousProvider);
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", previousConnection);
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalDirectory);

            if (Directory.Exists(outDir))
            {
                Directory.Delete(outDir, true);
            }

            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
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
