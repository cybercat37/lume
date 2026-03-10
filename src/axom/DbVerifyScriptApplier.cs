using System.Data.Common;

namespace Axom.Cli;

internal static class DbVerifyScriptApplier
{
    public static bool TryApply(
        DbConnection connection,
        string inputPath,
        bool includeSeeds,
        out string? error)
    {
        error = null;

        var sourceDir = Path.GetDirectoryName(Path.GetFullPath(inputPath));
        if (string.IsNullOrWhiteSpace(sourceDir))
        {
            return true;
        }

        var migrationsDir = Path.Combine(sourceDir, "db", "migrations");
        if (!TryApplySqlScripts(connection, migrationsDir, "migration", out error))
        {
            return false;
        }

        if (!includeSeeds)
        {
            return true;
        }

        var seedsDir = Path.Combine(sourceDir, "db", "seeds");
        return TryApplySqlScripts(connection, seedsDir, "seed", out error);
    }

    private static bool TryApplySqlScripts(
        DbConnection connection,
        string directory,
        string scriptKind,
        out string? error)
    {
        error = null;
        if (!Directory.Exists(directory))
        {
            return true;
        }

        var files = Directory
            .GetFiles(directory, "*.sql")
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        foreach (var file in files)
        {
            try
            {
                var script = File.ReadAllText(file);
                if (string.IsNullOrWhiteSpace(script))
                {
                    continue;
                }

                using var command = connection.CreateCommand();
                command.CommandText = script;
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                error = $"Failed to apply {scriptKind} '{Path.GetFileName(file)}': {ex.Message}";
                return false;
            }
        }

        return true;
    }
}
