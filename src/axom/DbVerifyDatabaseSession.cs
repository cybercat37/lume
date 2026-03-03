using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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

        var configuredProvider = Environment.GetEnvironmentVariable("AXOM_DB_PROVIDER");
        var provider = string.IsNullOrWhiteSpace(configuredProvider)
            ? "sqlite"
            : configuredProvider.Trim().ToLowerInvariant();
        var sqliteTempRoot = string.Empty;
        string? postgresSchemaName = null;

        if (!TryCreateRecordProjectionResolverFromEnvironment(out var recordResolver, out var resolverError))
        {
            error = resolverError;
            return false;
        }

        try
        {
            if (!TryCreateVerifyConnection(provider, out var connection, out sqliteTempRoot, out var createError))
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
                    var seedParameters = BuildVerificationParameterSeed(sql);
                    if (!SqlTemplateBinder.TryBind(sql, seedParameters, recordResolver, out var boundSql, out var boundParameters, out var bindError))
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
                && TryCreatePostgresCleanupConnection(out var cleanupConnection))
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
        string provider,
        out DbConnection connection,
        out string sqliteTempRoot,
        out string? error)
    {
        connection = null!;
        sqliteTempRoot = string.Empty;
        error = null;

        if (string.Equals(provider, "sqlite", StringComparison.Ordinal))
        {
            var configuredConnectionString = Environment.GetEnvironmentVariable("AXOM_DB_CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(configuredConnectionString))
            {
                connection = new SqliteConnection(configuredConnectionString);
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
            var connectionString = Environment.GetEnvironmentVariable("AXOM_DB_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                error = "AXOM_DB_CONNECTION_STRING is required when AXOM_DB_PROVIDER=postgres for db verify.";
                return false;
            }

            connection = new NpgsqlConnection(connectionString);
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

    private static bool TryCreatePostgresCleanupConnection(out DbConnection cleanupConnection)
    {
        cleanupConnection = null!;
        var connectionString = Environment.GetEnvironmentVariable("AXOM_DB_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        cleanupConnection = new NpgsqlConnection(connectionString);
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

    private static IReadOnlyDictionary<string, object?> BuildVerificationParameterSeed(string sql)
    {
        var seed = new Dictionary<string, object?>(StringComparer.Ordinal);
        var inSingleQuotedString = false;

        for (var i = 0; i < sql.Length; i++)
        {
            var current = sql[i];
            if (inSingleQuotedString)
            {
                if (current == '\'' && i + 1 < sql.Length && sql[i + 1] == '\'')
                {
                    i++;
                    continue;
                }

                if (current == '\'')
                {
                    inSingleQuotedString = false;
                }

                continue;
            }

            if (current == '\'')
            {
                inSingleQuotedString = true;
                continue;
            }

            if (current != '{')
            {
                continue;
            }

            var end = sql.IndexOf('}', i + 1);
            if (end <= i + 1)
            {
                continue;
            }

            var placeholder = sql.Substring(i + 1, end - i - 1).Trim();
            if (placeholder.Length == 0)
            {
                i = end;
                continue;
            }

            if (!Regex.IsMatch(placeholder, "^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant))
            {
                i = end;
                continue;
            }

            if (char.IsUpper(placeholder[0]))
            {
                i = end;
                continue;
            }

            if (!seed.ContainsKey(placeholder))
            {
                seed[placeholder] = 0;
            }

            i = end;
        }

        return seed;
    }

    private static bool TryCreateRecordProjectionResolverFromEnvironment(
        out ISqlRecordProjectionResolver? resolver,
        out string? error)
    {
        resolver = null;
        error = null;

        var raw = Environment.GetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        var map = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var declarations = raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var declaration in declarations)
        {
            var parts = declaration.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                error = $"Invalid AXOM_DB_RECORD_PROJECTIONS entry '{declaration}'. Expected format Record:col1,col2.";
                return false;
            }

            var columns = parts[1]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();
            if (columns.Length == 0)
            {
                error = $"Invalid AXOM_DB_RECORD_PROJECTIONS entry '{declaration}'. Record '{parts[0]}' must list at least one column.";
                return false;
            }

            map[parts[0]] = columns;
        }

        try
        {
            resolver = new DictionarySqlRecordProjectionResolver(map);
            return true;
        }
        catch (ArgumentException ex)
        {
            error = ex.Message;
            return false;
        }
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
