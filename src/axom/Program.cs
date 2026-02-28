using Axom.Compiler;
using Axom.Compiler.Http.Routing;
using Axom.Runtime.Db;
using Axom.Runtime.Http;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Axom.Cli;

public class Program
{
    public static int Main(string[] args)
    {
        DbRuntimeBootstrap.ConfigureFromEnvironment();

        const string usage =
            "Usage: axom <build|run|check|serve|init> <file.axom|project-name> [options]\n" +
            "   or: axom db <verify|check> <file.axom> [--report] [--plan] [--snapshot] [--compare] [--seeds] [--quiet|--verbose] [--cache]\n" +
            "\n" +
            "Options:\n" +
            "  --out <dir>   Override output directory (default: out)\n" +
            "  --host <addr> Bind host for serve (default: 127.0.0.1)\n" +
            "  --port <n>    Bind port for serve (default: 8080)\n" +
            "  --force       Overwrite scaffold files for init\n" +
            "  --quiet       Suppress non-error output\n" +
            "  --verbose     Include extra context\n" +
            "  --cache       Enable compilation cache\n" +
            "  --help, -h    Show usage\n" +
            "  --version     Show version\n" +
            "\n" +
            "Examples:\n" +
            "  axom init myapp\n" +
            "  axom check hello.axom\n" +
            "  axom db verify hello.axom --report\n" +
            "  axom db verify hello.axom --seeds\n" +
            "  axom db check hello.axom --report\n" +
            "  axom build hello.axom --out out\n" +
            "  axom run hello.axom --cache\n" +
            "  axom serve hello.axom --host 127.0.0.1 --port 8080\n";

        if (args.Length == 1)
        {
            if (args[0] == "--help" || args[0] == "-h")
            {
                Console.WriteLine(usage);
                return 0;
            }

            if (args[0] == "--version")
            {
                var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0";
                Console.WriteLine($"axom {version}");
                return 0;
            }
        }

        if (args.Length == 0)
        {
            Console.Error.WriteLine(usage);
            return 1;
        }

        var command = args[0];
        if (command == "init")
        {
            return InitProject(args, usage);
        }

        if (command == "db")
        {
            return HandleDbCommand(args, usage);
        }

        if (args.Length < 2)
        {
            Console.Error.WriteLine(usage);
            return 1;
        }

        var inputPath = args[1];

        if (command != "build" && command != "run" && command != "check" && command != "serve")
        {
            Console.Error.WriteLine(usage);
            return 1;
        }

        var outputDir = "out";
        var quiet = false;
        var verbose = false;
        var useCache = false;
        var host = "127.0.0.1";
        var port = 8080;

        for (var i = 2; i < args.Length; i++)
        {
            var argument = args[i];
            if (argument == "--out")
            {
                if (i + 1 >= args.Length)
                {
                    Console.Error.WriteLine(usage);
                    return 1;
                }

                outputDir = args[i + 1];
                if (string.IsNullOrWhiteSpace(outputDir))
                {
                    Console.Error.WriteLine(usage);
                    return 1;
                }

                i++;
                continue;
            }

            if (argument == "--host")
            {
                if (i + 1 >= args.Length)
                {
                    Console.Error.WriteLine(usage);
                    return 1;
                }

                host = args[i + 1];
                if (string.IsNullOrWhiteSpace(host))
                {
                    Console.Error.WriteLine(usage);
                    return 1;
                }

                i++;
                continue;
            }

            if (argument == "--port")
            {
                if (i + 1 >= args.Length)
                {
                    Console.Error.WriteLine(usage);
                    return 1;
                }

                if (!int.TryParse(args[i + 1], out port) || port is < 1 or > 65535)
                {
                    Console.Error.WriteLine(usage);
                    return 1;
                }

                i++;
                continue;
            }

            if (argument == "--quiet")
            {
                quiet = true;
                continue;
            }

            if (argument == "--verbose")
            {
                verbose = true;
                continue;
            }

            if (argument == "--cache")
            {
                useCache = true;
                continue;
            }

            Console.Error.WriteLine(usage);
            return 1;
        }

        if (quiet && verbose)
        {
            Console.Error.WriteLine(usage);
            return 1;
        }

        if (command == "serve" && !string.IsNullOrWhiteSpace(outputDir) && outputDir != "out")
        {
            Console.Error.WriteLine(usage);
            return 1;
        }

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"File not found: {inputPath}");
            return 1;
        }

        var source = File.ReadAllText(inputPath);

        var compiler = new CompilerDriver();
        var result = useCache
            ? compiler.CompileCached(source, inputPath, new CompilerCache())
            : compiler.Compile(source, inputPath);

        if (!result.Success)
        {
            foreach (var d in result.Diagnostics)
                Console.Error.WriteLine(d);

            return 1;
        }

        if (command == "check")
        {
            return 0;
        }

        if (command == "serve")
        {
            return ServeProgram(inputPath, host, port, quiet, verbose);
        }

        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, "Program.cs");
        File.WriteAllText(outputPath, result.GeneratedCode);

        if (command == "build")
        {
            if (!quiet)
            {
                if (verbose)
                {
                    Console.WriteLine($"Output: {outputPath}");
                }

                Console.WriteLine("Build succeeded.");
            }
            return 0;
        }

        // run command: compile and execute
        return RunGeneratedCode(outputDir);
    }

    private static int HandleDbCommand(string[] args, string usage)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine(usage);
            return 1;
        }

        var dbSubcommand = args[1];
        if (!string.Equals(dbSubcommand, "verify", StringComparison.Ordinal)
            && !string.Equals(dbSubcommand, "check", StringComparison.Ordinal))
        {
            Console.Error.WriteLine(usage);
            return 1;
        }

        var inputPath = args[2];
        var quiet = false;
        var verbose = false;
        var useCache = false;
        var report = false;
        var plan = false;
        var snapshot = false;
        var compare = false;
        var seeds = false;
        Dictionary<string, string>? currentPlanHashes = null;
        List<PreparedSqlQuery>? preparedQueries = null;

        for (var i = 3; i < args.Length; i++)
        {
            var argument = args[i];
            if (argument == "--quiet")
            {
                quiet = true;
                continue;
            }

            if (argument == "--verbose")
            {
                verbose = true;
                continue;
            }

            if (argument == "--cache")
            {
                useCache = true;
                continue;
            }

            if (argument == "--report")
            {
                report = true;
                continue;
            }

            if (argument == "--plan")
            {
                plan = true;
                continue;
            }

            if (argument == "--snapshot")
            {
                snapshot = true;
                continue;
            }

            if (argument == "--compare")
            {
                compare = true;
                continue;
            }

            if (argument == "--seeds")
            {
                seeds = true;
                continue;
            }

            Console.Error.WriteLine(usage);
            return 1;
        }

        if (quiet && verbose)
        {
            Console.Error.WriteLine(usage);
            return 1;
        }

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"File not found: {inputPath}");
            return 1;
        }

        var source = File.ReadAllText(inputPath);
        var compiler = new CompilerDriver();
        var result = useCache
            ? compiler.CompileCached(source, inputPath, new CompilerCache())
            : compiler.Compile(source, inputPath);

        if (!result.Success)
        {
            foreach (var d in result.Diagnostics)
            {
                Console.Error.WriteLine(d);
            }

            return 1;
        }

        var sqlLiterals = ExtractSqlLiterals(source);
        if (!TryValidateSqlAgainstEphemeralDatabase(
                inputPath,
                sqlLiterals,
                includeSeeds: seeds,
                includePlan: plan,
                emitPlanOutput: plan && !quiet,
                verbose,
                out preparedQueries,
                out currentPlanHashes,
                out var verifyError))
        {
            Console.Error.WriteLine(verifyError ?? "Failed to verify SQL queries.");
            return 1;
        }

        if (report && !quiet)
        {
            Console.WriteLine($"total_queries_validated={preparedQueries.Count}");
            Console.WriteLine("average_duration_ms=0");
        }

        if (snapshot)
        {
            WriteMetricsSnapshot(preparedQueries, currentPlanHashes);
            if (!quiet)
            {
                Console.WriteLine("snapshot_written=.axom/query-metrics.json");
            }
        }

        if (compare && !quiet)
        {
            var currentQueryIds = preparedQueries
                .Select(query => query.QueryId)
                .Distinct(StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal);

            if (!TryPrintSnapshotComparison(currentQueryIds, currentPlanHashes, verbose, out var compareError))
            {
                Console.Error.WriteLine(compareError ?? "Failed to compare query metrics snapshot.");
                return 1;
            }
        }

        if (verbose && !quiet)
        {
            Console.WriteLine($"db_verify_file={inputPath}");
        }

        return 0;
    }

    private static List<string> ExtractSqlLiterals(string source)
    {
        var values = new List<string>();
        var searchStart = 0;
        while (searchStart < source.Length)
        {
            var start = source.IndexOf("sql\"\"\"", searchStart, StringComparison.Ordinal);
            if (start < 0)
            {
                break;
            }

            var valueStart = start + 6;
            var end = source.IndexOf("\"\"\"", valueStart, StringComparison.Ordinal);
            if (end < 0)
            {
                break;
            }

            values.Add(source.Substring(valueStart, end - valueStart));
            searchStart = end + 3;
        }

        return values;
    }

    private static void WriteMetricsSnapshot(
        IReadOnlyList<PreparedSqlQuery> queries,
        IReadOnlyDictionary<string, string>? planHashes)
    {
        var payload = queries
            .Select(query => new
            {
                query_id = query.QueryId,
                average_duration = 0,
                execution_count = 0,
                plan_hash = TryGetPlanHash(query.QueryId, planHashes)
            })
            .ToList();

        Directory.CreateDirectory(".axom");
        var snapshotPath = Path.Combine(".axom", "query-metrics.json");
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(snapshotPath, json);
    }

    private static bool TryPrintSnapshotComparison(
        IReadOnlySet<string> currentQueryIds,
        IReadOnlyDictionary<string, string>? currentPlanHashes,
        bool verbose,
        out string? error)
    {
        error = null;
        var snapshotPath = Path.Combine(".axom", "query-metrics.json");
        if (!File.Exists(snapshotPath))
        {
            Console.WriteLine("compare_warning=snapshot_missing");
            return true;
        }

        HashSet<string> snapshotQueryIds;
        List<QueryMetricSnapshotEntry> entries;
        try
        {
            var json = File.ReadAllText(snapshotPath);
            entries = JsonSerializer.Deserialize<List<QueryMetricSnapshotEntry>>(json) ?? new List<QueryMetricSnapshotEntry>();
            snapshotQueryIds = entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.QueryId))
                .Select(entry => entry.QueryId!)
                .Distinct(StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal);
        }
        catch (Exception ex)
        {
            error = $"Invalid snapshot file '{snapshotPath}': {ex.Message}";
            return false;
        }

        var added = currentQueryIds
            .Where(queryId => !snapshotQueryIds.Contains(queryId))
            .OrderBy(queryId => queryId, StringComparer.Ordinal)
            .ToList();
        var removed = snapshotQueryIds
            .Where(queryId => !currentQueryIds.Contains(queryId))
            .OrderBy(queryId => queryId, StringComparer.Ordinal)
            .ToList();

        var planHashChanged = new List<string>();
        if (currentPlanHashes is not null)
        {
            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.QueryId)
                    || string.IsNullOrWhiteSpace(entry.PlanHash)
                    || !currentPlanHashes.TryGetValue(entry.QueryId!, out var currentPlanHash)
                    || string.IsNullOrWhiteSpace(currentPlanHash))
                {
                    continue;
                }

                if (!string.Equals(entry.PlanHash, currentPlanHash, StringComparison.Ordinal))
                {
                    planHashChanged.Add(entry.QueryId!);
                }
            }
        }

        if (added.Count == 0 && removed.Count == 0)
        {
            if (planHashChanged.Count == 0)
            {
                Console.WriteLine("compare_status=ok");
                return true;
            }
        }

        if (added.Count > 0)
        {
            Console.WriteLine($"compare_warning=query_added count={added.Count}");
            if (verbose)
            {
                foreach (var queryId in added)
                {
                    Console.WriteLine($"compare_added_query_id={queryId}");
                }
            }
        }

        if (removed.Count > 0)
        {
            Console.WriteLine($"compare_warning=query_removed count={removed.Count}");
            if (verbose)
            {
                foreach (var queryId in removed)
                {
                    Console.WriteLine($"compare_removed_query_id={queryId}");
                }
            }
        }

        if (planHashChanged.Count > 0)
        {
            Console.WriteLine($"compare_warning=plan_hash_changed count={planHashChanged.Count}");
            if (verbose)
            {
                foreach (var queryId in planHashChanged.OrderBy(queryId => queryId, StringComparer.Ordinal))
                {
                    Console.WriteLine($"compare_plan_hash_changed_query_id={queryId}");
                }
            }
        }

        if (added.Count == 0 && removed.Count == 0 && planHashChanged.Count == 0)
        {
            Console.WriteLine("compare_status=ok");
        }

        return true;
    }

    private static bool TryValidateSqlAgainstEphemeralDatabase(
        string inputPath,
        IReadOnlyList<string> sqlLiterals,
        bool includeSeeds,
        bool includePlan,
        bool emitPlanOutput,
        bool verbose,
        out List<PreparedSqlQuery> preparedQueries,
        out Dictionary<string, string>? planHashes,
        out string? error)
    {
        preparedQueries = new List<PreparedSqlQuery>();
        planHashes = includePlan ? new Dictionary<string, string>(StringComparer.Ordinal) : null;
        error = null;

        var tempRoot = Path.Combine(Path.GetTempPath(), "axom_db_verify", Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture));
        Directory.CreateDirectory(tempRoot);
        var dbPath = Path.Combine(tempRoot, "verify.db");
        var connectionString = $"Data Source={dbPath}";

        if (!TryCreateRecordProjectionResolverFromEnvironment(out var recordResolver, out var resolverError))
        {
            error = resolverError;
            return false;
        }

        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

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
                foreach (var (name, value) in boundParameters)
                {
                    var parameter = prepareCommand.CreateParameter();
                    parameter.ParameterName = name.StartsWith('@') ? name : "@" + name;
                    parameter.Value = value ?? 0;
                    prepareCommand.Parameters.Add(parameter);
                }

                try
                {
                    prepareCommand.Prepare();
                }
                catch (Exception ex)
                {
                    error = $"db verify failed for query_id={queryId}: {ex.Message}";
                    return false;
                }

                preparedQueries.Add(new PreparedSqlQuery(queryId, sql, boundSql, boundParameters));

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
                command.CommandText = "EXPLAIN QUERY PLAN " + boundSql;
                foreach (var (name, value) in boundParameters)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = name.StartsWith('@') ? name : "@" + name;
                    parameter.Value = value ?? 0;
                    command.Parameters.Add(parameter);
                }

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
                    var detail = reader.FieldCount > 3 && !reader.IsDBNull(3)
                        ? Convert.ToString(reader.GetValue(3))
                        : string.Empty;
                    details.Add(detail ?? string.Empty);
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
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static bool TryApplyMigrations(
        SqliteConnection connection,
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
        SqliteConnection connection,
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

    private static string? TryGetPlanHash(string queryId, IReadOnlyDictionary<string, string>? planHashes)
    {
        if (planHashes is null)
        {
            return null;
        }

        return planHashes.TryGetValue(queryId, out var planHash)
            ? planHash
            : null;
    }

    private sealed record PreparedSqlQuery(
        string QueryId,
        string OriginalSql,
        string BoundSql,
        IReadOnlyDictionary<string, object?> Parameters);

    private sealed record QueryMetricSnapshotEntry
    {
        [JsonPropertyName("query_id")]
        public string? QueryId { get; init; }

        [JsonPropertyName("average_duration")]
        public int AverageDuration { get; init; }

        [JsonPropertyName("execution_count")]
        public int ExecutionCount { get; init; }

        [JsonPropertyName("plan_hash")]
        public string? PlanHash { get; init; }
    }

    private static int InitProject(string[] args, string usage)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine(usage);
            return 1;
        }

        var projectName = args[1];
        if (string.IsNullOrWhiteSpace(projectName))
        {
            Console.Error.WriteLine("Project name cannot be empty.");
            return 1;
        }

        var force = false;
        for (var i = 2; i < args.Length; i++)
        {
            if (args[i] == "--force")
            {
                force = true;
                continue;
            }

            Console.Error.WriteLine(usage);
            return 1;
        }

        var projectPath = Path.GetFullPath(projectName);
        if (!ProjectScaffolder.TryScaffoldApiProject(projectPath, force, out var error))
        {
            Console.Error.WriteLine(error ?? "Failed to scaffold project.");
            return 1;
        }

        Console.WriteLine($"Initialized Axom API project at {projectPath}");
        Console.WriteLine("Next steps:");
        Console.WriteLine($"  cd {projectName}");
        Console.WriteLine("  axom serve main.axom --host 127.0.0.1 --port 8080");
        Console.WriteLine("  curl -i http://127.0.0.1:8080/health");
        return 0;
    }

    private static int ServeProgram(string inputPath, string host, int port, bool quiet, bool verbose)
    {
        var routeDiscovery = new RouteDiscovery();
        var routeResult = routeDiscovery.Discover(inputPath);
        if (!routeResult.Success)
        {
            foreach (var diagnostic in routeResult.Diagnostics)
            {
                Console.Error.WriteLine(diagnostic);
            }

            return 1;
        }

        using var cancellationTokenSource = new CancellationTokenSource();
        var runtimeRoutes = routeResult.Routes
            .Select(RouteHandlerFactory.CreateEndpoint)
            .ToList();

        ConsoleCancelEventHandler handler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        Console.CancelKeyPress += handler;

        try
        {
            var httpHost = new AxomHttpHost();

            if (!quiet)
            {
                if (verbose)
                {
                    Console.WriteLine($"Serving source: {inputPath}");
                    Console.WriteLine($"Discovered routes: {routeResult.Routes.Count}");

                    foreach (var route in routeResult.Routes.OrderBy(route => route.Template, StringComparer.Ordinal))
                    {
                        Console.WriteLine($"  {route.Method} {route.Template}");
                    }
                }

                Console.WriteLine($"Listening on http://{host}:{port}");
                Console.WriteLine("Press Ctrl+C to stop.");
            }

            httpHost.RunAsync(host, port, runtimeRoutes, cancellationTokenSource.Token).GetAwaiter().GetResult();
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error serving code: {ex.Message}");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }
    }

    static int RunGeneratedCode(string outputDir)
    {
        var tempDir = Path.Combine(outputDir, ".run");
        
        try
        {
            // Clean up previous run
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
            Directory.CreateDirectory(tempDir);

            // Create a temporary console project
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "new console -n TempRun --force",
                    WorkingDirectory = tempDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Console.Error.WriteLine("Failed to create temporary project.");
                return 1;
            }

            // Copy generated Program.cs
            var generatedProgramPath = Path.Combine(outputDir, "Program.cs");
            var tempProgramPath = Path.Combine(tempDir, "TempRun", "Program.cs");
            File.Copy(generatedProgramPath, tempProgramPath, true);

            var generatedSource = File.ReadAllText(generatedProgramPath);
            if (generatedSource.Contains("using Microsoft.Data.Sqlite;", StringComparison.Ordinal))
            {
                var addPackageProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "add package Microsoft.Data.Sqlite --version 8.0.8",
                        WorkingDirectory = Path.Combine(tempDir, "TempRun"),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };

                addPackageProcess.Start();
                addPackageProcess.WaitForExit();

                if (addPackageProcess.ExitCode != 0)
                {
                    Console.Error.WriteLine("Failed to add Microsoft.Data.Sqlite package for generated run project.");
                    return 1;
                }
            }

            // Build and run
            var runProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "run",
                    WorkingDirectory = Path.Combine(tempDir, "TempRun"),
                    UseShellExecute = false
                }
            };

            runProcess.Start();
            runProcess.WaitForExit();
            return runProcess.ExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error running code: {ex.Message}");
            return 1;
        }
        finally
        {
            // Cleanup is optional - comment out if you want to inspect generated code
            // if (Directory.Exists(tempDir))
            // {
            //     Directory.Delete(tempDir, true);
            // }
        }
    }
}
