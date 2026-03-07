using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Axom.Runtime.Db;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Axom.Cli;

internal sealed record DbVerifiedQuery(string QueryId);

internal static class DbVerifyDatabaseSession
{
    public static bool TryValidateSqlAgainstEphemeralDatabase(
        string inputPath,
        IReadOnlyList<string> sqlLiterals,
        bool includeSeeds,
        bool includePlan,
        bool emitPlanOutput,
        bool verbose,
        out List<DbVerifiedQuery> verifiedQueries,
        out Dictionary<string, string>? planHashes,
        out string? error)
    {
        verifiedQueries = new List<DbVerifiedQuery>();
        planHashes = includePlan ? new Dictionary<string, string>(StringComparer.Ordinal) : null;
        error = null;

        if (!DbVerifyEnvironmentLoader.TryLoad(out var environment, out var loadError))
        {
            error = loadError;
            return false;
        }

        var provider = environment.Provider;
        var sqliteTempRoot = string.Empty;
        string? postgresSchemaName = null;

        try
        {
            if (!TryCreateVerifyConnection(environment, out var connection, out sqliteTempRoot, out var createError))
            {
                error = createError;
                return false;
            }

            using (connection)
            {
                connection.Open();

                if (string.Equals(provider, "postgres", StringComparison.Ordinal)
                    && !TryInitializePostgresSchema(connection, out postgresSchemaName, out var schemaError))
                {
                    error = schemaError;
                    return false;
                }

                if (!TryApplyMigrations(connection, inputPath, includeSeeds, out var migrationError))
                {
                    error = migrationError;
                    return false;
                }

                foreach (var sql in sqlLiterals)
                {
                    var trimmed = sql.TrimStart();
                    if (trimmed.Length == 0)
                    {
                        continue;
                    }

                    var queryId = DbQueryFingerprint.CreateQueryId(sql);
                    var seedParameters = DbVerifyParameterSeedBuilder.Build(sql);
                    if (!SqlTemplateBinder.TryBind(sql, seedParameters, environment.RecordProjectionResolver, out var boundSql, out var boundParameters, out var bindError))
                    {
                        error = $"db verify failed for query_id={queryId}: {bindError}";
                        return false;
                    }

                    using var prepareCommand = connection.CreateCommand();
                    prepareCommand.CommandText = boundSql;
                    AddParameters(prepareCommand, boundParameters);

                    try
                    {
                        prepareCommand.Prepare();
                    }
                    catch (Exception ex)
                    {
                        error = $"db verify failed for query_id={queryId}: {ex.Message}";
                        return false;
                    }

                    verifiedQueries.Add(new DbVerifiedQuery(queryId));

                    if (!includePlan)
                    {
                        continue;
                    }

                    if (!CanExplainSql(trimmed))
                    {
                        if (verbose)
                        {
                            Console.WriteLine($"plan skipped=statement_kind query_id={queryId}");
                        }

                        continue;
                    }

                    using var command = connection.CreateCommand();
                    command.CommandText = GetExplainPrefix(provider) + boundSql;
                    AddParameters(command, boundParameters);

                    using var reader = command.ExecuteReader();
                    var details = new List<string>();
                    if (emitPlanOutput)
                    {
                        Console.WriteLine($"plan query_id={queryId}");
                    }

                    var hasRows = false;
                    while (reader.Read())
                    {
                        hasRows = true;
                        var detail = ReadExplainDetail(provider, reader);
                        details.Add(detail);
                        if (emitPlanOutput)
                        {
                            Console.WriteLine($"plan detail={detail}");
                        }
                    }

                    if (!hasRows)
                    {
                        details.Add("<empty>");
                        if (emitPlanOutput)
                        {
                            Console.WriteLine("plan detail=<empty>");
                        }
                    }

                    var planHash = ComputePlanHash(details);
                    planHashes![queryId] = planHash;
                    if (emitPlanOutput && verbose)
                    {
                        Console.WriteLine($"plan hash={planHash}");
                    }
                }

                return true;
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(postgresSchemaName)
                && TryCreatePostgresCleanupConnection(environment, out var cleanupConnection))
            {
                using (cleanupConnection)
                {
                    cleanupConnection.Open();
                    using var cleanupCommand = cleanupConnection.CreateCommand();
                    cleanupCommand.CommandText = $"DROP SCHEMA IF EXISTS \"{postgresSchemaName}\" CASCADE;";
                    cleanupCommand.ExecuteNonQuery();
                }
            }

            if (!string.IsNullOrWhiteSpace(sqliteTempRoot) && Directory.Exists(sqliteTempRoot))
            {
                Directory.Delete(sqliteTempRoot, recursive: true);
            }
        }
    }

    private static bool TryApplyMigrations(
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

    private static bool TryCreateVerifyConnection(
        DbVerifyEnvironment environment,
        out DbConnection connection,
        out string sqliteTempRoot,
        out string? error)
    {
        connection = null!;
        sqliteTempRoot = string.Empty;
        error = null;

        var provider = environment.Provider;

        if (string.Equals(provider, "sqlite", StringComparison.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(environment.ConnectionString))
            {
                connection = new SqliteConnection(environment.ConnectionString);
                return true;
            }

            sqliteTempRoot = Path.Combine(Path.GetTempPath(), "axom_db_verify", Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture));
            Directory.CreateDirectory(sqliteTempRoot);
            var dbPath = Path.Combine(sqliteTempRoot, "verify.db");
            connection = new SqliteConnection($"Data Source={dbPath}");
            return true;
        }

        if (string.Equals(provider, "postgres", StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(environment.ConnectionString))
            {
                error = "AXOM_DB_CONNECTION_STRING is required when AXOM_DB_PROVIDER=postgres for db verify.";
                return false;
            }

            connection = new NpgsqlConnection(environment.ConnectionString);
            return true;
        }

        error = $"Unsupported AXOM_DB_PROVIDER '{provider}' for db verify. Supported providers: sqlite, postgres.";
        return false;
    }

    private static bool TryInitializePostgresSchema(DbConnection connection, out string? schemaName, out string? error)
    {
        schemaName = "axom_verify_" + Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);
        error = null;

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"CREATE SCHEMA \"{schemaName}\"; SET search_path TO \"{schemaName}\";";
            command.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            error = $"Failed to initialize postgres verify schema: {ex.Message}";
            schemaName = null;
            return false;
        }
    }

    private static bool TryCreatePostgresCleanupConnection(DbVerifyEnvironment environment, out DbConnection cleanupConnection)
    {
        cleanupConnection = null!;
        if (string.IsNullOrWhiteSpace(environment.ConnectionString))
        {
            return false;
        }

        cleanupConnection = new NpgsqlConnection(environment.ConnectionString);
        return true;
    }

    private static void AddParameters(DbCommand command, IReadOnlyDictionary<string, object?> parameters)
    {
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name.StartsWith('@') ? name : "@" + name;
            parameter.Value = value ?? 0;
            command.Parameters.Add(parameter);
        }
    }

    private static string GetExplainPrefix(string provider)
    {
        return string.Equals(provider, "postgres", StringComparison.Ordinal)
            ? "EXPLAIN "
            : "EXPLAIN QUERY PLAN ";
    }

    private static string ReadExplainDetail(string provider, DbDataReader reader)
    {
        if (string.Equals(provider, "postgres", StringComparison.Ordinal))
        {
            return reader.FieldCount > 0 && !reader.IsDBNull(0)
                ? Convert.ToString(reader.GetValue(0)) ?? string.Empty
                : string.Empty;
        }

        return reader.FieldCount > 3 && !reader.IsDBNull(3)
            ? Convert.ToString(reader.GetValue(3)) ?? string.Empty
            : string.Empty;
    }

    private static bool CanExplainSql(string sql)
    {
        return sql.StartsWith("select", StringComparison.OrdinalIgnoreCase)
            || sql.StartsWith("with", StringComparison.OrdinalIgnoreCase)
            || sql.StartsWith("delete", StringComparison.OrdinalIgnoreCase)
            || sql.StartsWith("update", StringComparison.OrdinalIgnoreCase)
            || sql.StartsWith("insert", StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputePlanHash(IReadOnlyList<string> details)
    {
        var normalized = string.Join("\n", details).Trim();
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
