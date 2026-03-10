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
    public void Db_check_alias_behaves_like_db_verify()
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

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "check", filePath, "--report" });

            Assert.Equal(0, exitCode);
            Assert.Contains("total_queries_validated=1", output.ToString(), StringComparison.Ordinal);
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
    public void Db_verify_report_uses_canonical_line_contract()
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
            Assert.Equal(string.Empty, error.ToString());

            var lines = ReadNonEmptyLines(output.ToString());
            Assert.Equal(2, lines.Length);
            Assert.Equal("total_queries_validated=1", lines[0]);
            Assert.Equal("average_duration_ms=0", lines[1]);
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
    public void Db_verify_report_with_no_sql_literals_prints_zero_totals()
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

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--report" });

            Assert.Equal(0, exitCode);
            Assert.Contains("total_queries_validated=0", output.ToString(), StringComparison.Ordinal);
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
    public void Db_verify_report_quiet_suppresses_metrics_output()
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

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--report", "--quiet" });

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
    public void Db_verify_report_counts_each_validated_sql_literal()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_db_verify_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(filePath, "print sql\"\"\"select 1\"\"\".one()\nprint sql\"\"\"select 1\"\"\".one()");

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
            Assert.Contains("total_queries_validated=2", output.ToString(), StringComparison.Ordinal);
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
    public void Db_verify_report_and_compare_print_both_outputs_when_snapshot_matches()
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

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--report", "--compare" });

            Assert.Equal(0, exitCode);
            Assert.Contains("total_queries_validated=1", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("average_duration_ms=0", output.ToString(), StringComparison.Ordinal);
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
    public void Db_verify_plan_uses_ephemeral_sqlite_without_env_configuration()
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

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--plan" });

            Assert.Equal(0, exitCode);
            Assert.Contains("plan query_id=", output.ToString(), StringComparison.Ordinal);
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
    public void Db_verify_plan_prints_sqlite_explain_output()
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

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--plan" });

            Assert.Equal(0, exitCode);
            Assert.Contains("plan query_id=", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("plan detail=", output.ToString(), StringComparison.Ordinal);
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
    public void Db_verify_fails_for_unsupported_provider()
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
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", "mysql");
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", null);
            Directory.SetCurrentDirectory(tempDir);
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath });

            Assert.Equal(1, exitCode);
            Assert.Contains("Unsupported AXOM_DB_PROVIDER", error.ToString(), StringComparison.Ordinal);
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
    public void Db_verify_postgres_requires_connection_string()
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
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", "postgres");
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", null);
            Directory.SetCurrentDirectory(tempDir);
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath });

            Assert.Equal(1, exitCode);
            Assert.Contains("AXOM_DB_CONNECTION_STRING is required when AXOM_DB_PROVIDER=postgres", error.ToString(), StringComparison.Ordinal);
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
    public void Db_verify_postgres_happy_path_when_test_connection_is_available()
    {
        var integrationConnectionString = Environment.GetEnvironmentVariable("AXOM_TEST_POSTGRES_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(integrationConnectionString))
        {
            return;
        }

        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_db_verify_pg_{Guid.NewGuid():N}");
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
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", "postgres");
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", integrationConnectionString);
            Directory.SetCurrentDirectory(tempDir);
            Console.SetOut(output);
            Console.SetError(error);

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--report", "--plan" });

            Assert.Equal(0, exitCode);
            Assert.Contains("total_queries_validated=1", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("plan query_id=", output.ToString(), StringComparison.Ordinal);
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
    public void Db_verify_compare_missing_snapshot_uses_canonical_warning_line()
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

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--compare" });

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());

            var lines = ReadNonEmptyLines(output.ToString());
            Assert.Single(lines);
            Assert.Equal("compare_warning=snapshot_missing", lines[0]);
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
    public void Db_verify_compare_quiet_still_returns_error_for_invalid_snapshot_json()
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

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--compare", "--quiet" });

            Assert.Equal(1, exitCode);
            Assert.Contains("Invalid snapshot file", error.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, output.ToString());
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
        var filePath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(filePath, "print sql\"\"\"select {id}\"\"\".one([\"id\": \"1\"])");

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

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--plan", "--verbose" });

            Assert.Equal(0, exitCode);
            Assert.Contains("plan query_id=", output.ToString(), StringComparison.Ordinal);
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
    public void Db_verify_snapshot_with_plan_includes_plan_hash()
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

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--plan", "--snapshot" });

            Assert.Equal(0, exitCode);
            var snapshotPath = Path.Combine(tempDir, ".axom", "query-metrics.json");
            var snapshot = File.ReadAllText(snapshotPath);
            Assert.Contains("plan_hash", snapshot, StringComparison.Ordinal);
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
    public void Db_verify_compare_with_plan_warns_when_snapshot_plan_hash_differs()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_db_verify_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "test.axom");
        var sql = "select 1";
        var queryId = Axom.Runtime.Db.DbQueryFingerprint.CreateQueryId(sql);
        File.WriteAllText(filePath, "print sql\"\"\"select 1\"\"\".one()");
        Directory.CreateDirectory(Path.Combine(tempDir, ".axom"));
        File.WriteAllText(
            Path.Combine(tempDir, ".axom", "query-metrics.json"),
            $"[{{\"query_id\":\"{queryId}\",\"average_duration\":0,\"execution_count\":0,\"plan_hash\":\"deadbeef\"}}]");

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

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--compare", "--plan" });

            Assert.Equal(0, exitCode);
            Assert.Contains("compare_warning=plan_hash_changed count=1", output.ToString(), StringComparison.Ordinal);
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
    public void Db_verify_applies_migrations_next_to_input_file()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_db_verify_{Guid.NewGuid():N}");
        var migrationsDir = Path.Combine(tempDir, "db", "migrations");
        Directory.CreateDirectory(migrationsDir);
        File.WriteAllText(Path.Combine(migrationsDir, "001_create_users.sql"), "create table users (id integer primary key, name text not null);");

        var filePath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(filePath, "print sql\"\"\"select name from users\"\"\".all()");

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
    public void Db_verify_returns_error_when_migration_application_fails()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_db_verify_{Guid.NewGuid():N}");
        var migrationsDir = Path.Combine(tempDir, "db", "migrations");
        Directory.CreateDirectory(migrationsDir);
        File.WriteAllText(Path.Combine(migrationsDir, "001_invalid.sql"), "create table users (");

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

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath });

            Assert.Equal(1, exitCode);
            Assert.Contains("Failed to apply migration", error.ToString(), StringComparison.Ordinal);
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
    public void Db_verify_fails_when_query_depends_on_seed_without_seeds_flag()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_db_verify_{Guid.NewGuid():N}");
        var migrationsDir = Path.Combine(tempDir, "db", "migrations");
        var seedsDir = Path.Combine(tempDir, "db", "seeds");
        Directory.CreateDirectory(migrationsDir);
        Directory.CreateDirectory(seedsDir);
        File.WriteAllText(Path.Combine(migrationsDir, "001_create_users.sql"), "create table users (id integer primary key, name text not null);");
        File.WriteAllText(Path.Combine(seedsDir, "001_create_view.sql"), "create view seeded_users as select id, name from users;");

        var filePath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(filePath, "print sql\"\"\"select name from seeded_users\"\"\".all()");

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

            Assert.Equal(1, exitCode);
            Assert.Contains("db verify failed", error.ToString(), StringComparison.Ordinal);
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
    public void Db_verify_applies_seed_scripts_when_seeds_flag_is_enabled()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_db_verify_{Guid.NewGuid():N}");
        var migrationsDir = Path.Combine(tempDir, "db", "migrations");
        var seedsDir = Path.Combine(tempDir, "db", "seeds");
        Directory.CreateDirectory(migrationsDir);
        Directory.CreateDirectory(seedsDir);
        File.WriteAllText(Path.Combine(migrationsDir, "001_create_users.sql"), "create table users (id integer primary key, name text not null);");
        File.WriteAllText(Path.Combine(seedsDir, "001_create_view.sql"), "create view seeded_users as select id, name from users;");

        var filePath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(filePath, "print sql\"\"\"select name from seeded_users\"\"\".all()");

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

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--seeds", "--report" });

            Assert.Equal(0, exitCode);
            Assert.Contains("total_queries_validated=1", output.ToString(), StringComparison.Ordinal);
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
    public void Db_verify_returns_error_when_seed_application_fails()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_cli_db_verify_{Guid.NewGuid():N}");
        var migrationsDir = Path.Combine(tempDir, "db", "migrations");
        var seedsDir = Path.Combine(tempDir, "db", "seeds");
        Directory.CreateDirectory(migrationsDir);
        Directory.CreateDirectory(seedsDir);
        File.WriteAllText(Path.Combine(migrationsDir, "001_create_users.sql"), "create table users (id integer primary key, name text not null);");
        File.WriteAllText(Path.Combine(seedsDir, "001_invalid_seed.sql"), "insert into users (");

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

            var exitCode = Axom.Cli.Program.Main(new[] { "db", "verify", filePath, "--seeds" });

            Assert.Equal(1, exitCode);
            Assert.Contains("Failed to apply seed 'seeds/001_invalid_seed.sql'", error.ToString(), StringComparison.Ordinal);
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

    private static string[] ReadNonEmptyLines(string value)
    {
        return value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
