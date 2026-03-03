using System.Text.Json;
using System.Text.Json.Serialization;

namespace Axom.Cli;

internal static class DbVerifySnapshotService
{
    public static void WriteMetricsSnapshot(
        IReadOnlyList<DbVerifiedQuery> queries,
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

    public static bool TryPrintSnapshotComparison(
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

        if (added.Count == 0 && removed.Count == 0 && planHashChanged.Count == 0)
        {
            Console.WriteLine("compare_status=ok");
            return true;
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

        return true;
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
}
