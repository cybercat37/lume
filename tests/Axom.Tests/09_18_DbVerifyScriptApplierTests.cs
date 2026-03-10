using Axom.Cli;
using Microsoft.Data.Sqlite;

namespace Axom.Tests;

public class DbVerifyScriptApplierTests
{
    [Fact]
    public void Try_apply_runs_migrations_from_input_directory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_script_applier_{Guid.NewGuid():N}");
        var migrationsDir = Path.Combine(tempDir, "db", "migrations");
        Directory.CreateDirectory(migrationsDir);
        File.WriteAllText(Path.Combine(migrationsDir, "001_create_users.sql"), "create table users (id integer primary key, name text not null);");
        var inputPath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(inputPath, "print 1");

        try
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            var success = DbVerifyScriptApplier.TryApply(connection, inputPath, includeSeeds: false, out var error);

            Assert.True(success);
            Assert.Null(error);

            using var command = connection.CreateCommand();
            command.CommandText = "select count(*) from users;";
            var count = Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(0, count);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Try_apply_skips_seeds_when_flag_is_disabled()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_script_applier_{Guid.NewGuid():N}");
        var migrationsDir = Path.Combine(tempDir, "db", "migrations");
        var seedsDir = Path.Combine(tempDir, "db", "seeds");
        Directory.CreateDirectory(migrationsDir);
        Directory.CreateDirectory(seedsDir);
        File.WriteAllText(Path.Combine(migrationsDir, "001_create_users.sql"), "create table users (id integer primary key, name text not null);");
        File.WriteAllText(Path.Combine(seedsDir, "001_create_view.sql"), "create view seeded_users as select id, name from users;");
        var inputPath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(inputPath, "print 1");

        try
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            var success = DbVerifyScriptApplier.TryApply(connection, inputPath, includeSeeds: false, out var error);

            Assert.True(success);
            Assert.Null(error);

            using var command = connection.CreateCommand();
            command.CommandText = "select name from seeded_users;";
            var ex = Assert.Throws<SqliteException>(() => command.ExecuteScalar());
            Assert.NotEqual(0, ex.SqliteErrorCode);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Try_apply_returns_error_when_sql_script_fails()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"axom_script_applier_{Guid.NewGuid():N}");
        var migrationsDir = Path.Combine(tempDir, "db", "migrations");
        Directory.CreateDirectory(migrationsDir);
        File.WriteAllText(Path.Combine(migrationsDir, "001_invalid.sql"), "create table users (");
        var inputPath = Path.Combine(tempDir, "test.axom");
        File.WriteAllText(inputPath, "print 1");

        try
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            var success = DbVerifyScriptApplier.TryApply(connection, inputPath, includeSeeds: false, out var error);

            Assert.False(success);
            Assert.Contains("Failed to apply migration '001_invalid.sql'", error, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
