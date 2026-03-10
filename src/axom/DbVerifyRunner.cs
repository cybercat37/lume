using Axom.Compiler;

namespace Axom.Cli;

internal sealed record DbVerifyOptions(
    bool Quiet,
    bool Verbose,
    bool UseCache,
    bool Report,
    bool Plan,
    bool Snapshot,
    bool Compare,
    bool Seeds);

internal static class DbVerifyRunner
{
    public static int Run(string inputPath, DbVerifyOptions options)
    {
        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"File not found: {inputPath}");
            return 1;
        }

        var source = File.ReadAllText(inputPath);
        var compiler = new CompilerDriver();
        var result = options.UseCache
            ? compiler.CompileCached(source, inputPath, new CompilerCache())
            : compiler.Compile(source, inputPath);

        if (!result.Success)
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                Console.Error.WriteLine(diagnostic);
            }

            return 1;
        }

        var sqlLiterals = ExtractSqlLiterals(source);
        if (!DbVerifyDatabaseSession.TryValidateSqlAgainstEphemeralDatabase(
                inputPath,
                sqlLiterals,
                includeSeeds: options.Seeds,
                includePlan: options.Plan,
                emitPlanOutput: options.Plan && !options.Quiet,
                options.Verbose,
                out var verifiedQueries,
                out var currentPlanHashes,
                out var verifyError))
        {
            Console.Error.WriteLine(verifyError ?? "Failed to verify SQL queries.");
            return 1;
        }

        if (options.Report && !options.Quiet)
        {
            Console.WriteLine($"total_queries_validated={verifiedQueries.Count}");
            Console.WriteLine("average_duration_ms=0");
        }

        if (options.Snapshot)
        {
            DbVerifySnapshotService.WriteMetricsSnapshot(verifiedQueries, currentPlanHashes);
            if (!options.Quiet)
            {
                Console.WriteLine("snapshot_written=.axom/query-metrics.json");
            }
        }

        if (options.Compare)
        {
            var currentQueryIds = verifiedQueries
                .Select(query => query.QueryId)
                .Distinct(StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal);

            if (!DbVerifySnapshotService.TryPrintSnapshotComparison(
                    currentQueryIds,
                    currentPlanHashes,
                    options.Verbose,
                    emitOutput: !options.Quiet,
                    out var compareError))
            {
                Console.Error.WriteLine(compareError ?? "Failed to compare query metrics snapshot.");
                return 1;
            }
        }

        if (options.Verbose && !options.Quiet)
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
}
