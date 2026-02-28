using System.Globalization;

namespace Axom.Tests;

[Collection("CliTests")]
public class CliDbVerifyCommandTests
{
    [Fact]
    public void Db_verify_valid_file_returns_zero_and_no_output_by_default()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_db_verify_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(filePath, "print 1");

        var originalDirectory = Directory.GetCurrentDirectory();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath });

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, output.ToString());
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalDirectory);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Db_verify_report_prints_aggregated_metrics()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_db_verify_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(filePath, "print sql\"\"\"select 1\"\"\".one()");

        var originalDirectory = Directory.GetCurrentDirectory();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--report" });

            Assert.Equal(0, exitCode);
            Assert.Contains("total_queries_validated=1", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("average_duration_ms=0", output.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalDirectory);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Db_verify_snapshot_writes_query_metrics_file()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_db_verify_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(filePath, "print sql\"\"\"select 1\"\"\".one()");

        var originalDirectory = Directory.GetCurrentDirectory();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--snapshot" });

            Assert.Equal(0, exitCode);
            var snapshotPath = Path.Combine(tempDir, ".axom", "query-metrics.json");
            Assert.True(File.Exists(snapshotPath));
            var snapshot = File.ReadAllText(snapshotPath);
            Assert.Contains("query_id", snapshot, StringComparison.Ordinal);
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalDirectory);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Db_verify_plan_requires_db_environment_configuration()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_db_verify_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(filePath, "print sql\"\"\"select 1\"\"\".one()");

        var previousProvider = Environment.GetEnvironmentVariable("AXOM_DB_PROVIDER");
        var previousConnectionString = Environment.GetEnvironmentVariable("AXOM_DB_CONNECTION_STRING");
        var originalDirectory = Directory.GetCurrentDirectory();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", null);
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", null);
            Directory.SetCurrentDirectory(tempDir);
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--plan" });

            Assert.Equal(1, exitCode);
            Assert.Contains("--plan requires AXOM_DB_PROVIDER and AXOM_DB_CONNECTION_STRING", error.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", previousProvider);
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", previousConnectionString);
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalDirectory);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Db_verify_plan_prints_sqlite_explain_output()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_db_verify_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "test.db");
        var filePath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(filePath, "print sql\"\"\"select 1\"\"\".one()");

        var previousProvider = Environment.GetEnvironmentVariable("AXOM_DB_PROVIDER");
        var previousConnectionString = Environment.GetEnvironmentVariable("AXOM_DB_CONNECTION_STRING");
        var originalDirectory = Directory.GetCurrentDirectory();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", "sqlite");
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", $"Data Source={dbPath}");
            Directory.SetCurrentDirectory(tempDir);
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--plan" });

            Assert.Equal(0, exitCode);
            Assert.Contains("plan query_id=", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("plan detail=", output.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", previousProvider);
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", previousConnectionString);
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalDirectory);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Db_verify_compare_reports_ok_when_snapshot_matches_current_queries()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_db_verify_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(filePath, "print sql\"\"\"select 1\"\"\".one()");

        var originalDirectory = Directory.GetCurrentDirectory();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            _ = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--snapshot", "--quiet" });

            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--compare" });

            Assert.Equal(0, exitCode);
            Assert.Contains("compare_status=ok", output.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalDirectory);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Db_verify_compare_reports_added_queries_when_current_set_changes()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_db_verify_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var baselinePath = Path.Combine(tempDir, "baseline.axom");
        var changedPath = Path.Combine(tempDir, "changed.axom");
        File.WriteAllText(baselinePath, "print sql\"\"\"select 1\"\"\".one()");
        File.WriteAllText(changedPath, "print sql\"\"\"select 1\"\"\".one()\nprint sql\"\"\"select 1 as second\"\"\".one()");

        var originalDirectory = Directory.GetCurrentDirectory();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            _ = Axom.Cli.Program.Main(new[] { "db", "verify", baselinePath, "--snapshot", "--quiet" });

            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", changedPath, "--compare" });

            Assert.Equal(0, exitCode);
            Assert.Contains("compare_warning=query_added count=1", output.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalDirectory);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Db_verify_compare_reports_removed_queries_when_current_set_changes()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_db_verify_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var baselinePath = Path.Combine(tempDir, "baseline.axom");
        var changedPath = Path.Combine(tempDir, "changed.axom");
        File.WriteAllText(baselinePath, "print sql\"\"\"select 1\"\"\".one()\nprint sql\"\"\"select 1 as second\"\"\".one()");
        File.WriteAllText(changedPath, "print sql\"\"\"select 1\"\"\".one()");

        var originalDirectory = Directory.GetCurrentDirectory();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            _ = Axom.Cli.Program.Main(new[] { "db", "verify", baselinePath, "--snapshot", "--quiet" });

            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", changedPath, "--compare" });

            Assert.Equal(0, exitCode);
            Assert.Contains("compare_warning=query_removed count=1", output.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalDirectory);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Db_verify_compare_verbose_prints_added_and_removed_query_ids()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_db_verify_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var baselinePath = Path.Combine(tempDir, "baseline.axom");
        var changedPath = Path.Combine(tempDir, "changed.axom");
        File.WriteAllText(baselinePath, "print sql\"\"\"select 1\"\"\".one()\nprint sql\"\"\"select 1 as old_alias\"\"\".one()");
        File.WriteAllText(changedPath, "print sql\"\"\"select 1\"\"\".one()\nprint sql\"\"\"select 1 as new_alias\"\"\".one()");

        var originalDirectory = Directory.GetCurrentDirectory();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            _ = Axom.Cli.Program.Main(new[] { "db", "verify", baselinePath, "--snapshot", "--quiet" });

            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", changedPath, "--compare", "--verbose" });

            Assert.Equal(0, exitCode);
            Assert.Contains("compare_warning=query_added", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("compare_warning=query_removed", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("compare_added_query_id=", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("compare_removed_query_id=", output.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalDirectory);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Db_verify_compare_returns_error_for_invalid_snapshot_json()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_db_verify_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(filePath, "print sql\"\"\"select 1\"\"\".one()");
        Directory.CreateDirectory(Path.Combine(tempDir, ".axom"));
        File.WriteAllText(Path.Combine(tempDir, ".axom", "query-metrics.json"), "{ not-valid-json ");

        var originalDirectory = Directory.GetCurrentDirectory();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--compare" });

            Assert.Equal(1, exitCode);
            Assert.Contains("Invalid snapshot file", error.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalDirectory);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Db_verify_plan_verbose_skips_template_queries_without_failing()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_db_verify_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "test.db");
        var filePath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(filePath, "print sql\"\"\"select {id}\"\"\".one([\"id\": \"1\"])");

        var previousProvider = Environment.GetEnvironmentVariable("AXOM_DB_PROVIDER");
        var previousConnectionString = Environment.GetEnvironmentVariable("AXOM_DB_CONNECTION_STRING");
        var originalDirectory = Directory.GetCurrentDirectory();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var error = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", "sqlite");
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", $"Data Source={dbPath}");
            Directory.SetCurrentDirectory(tempDir);
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--plan", "--verbose" });

            Assert.Equal(0, exitCode);
            Assert.Contains("plan skipped=query_template", output.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", previousProvider);
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", previousConnectionString);
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalDirectory);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
