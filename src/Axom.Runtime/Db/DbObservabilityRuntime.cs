using System.Diagnostics;

namespace Axom.Runtime.Db;

public interface IDbObservabilitySink
{
    void Write(DbQueryLogEntry entry);
}

public sealed class NullDbObservabilitySink : IDbObservabilitySink
{
    public static NullDbObservabilitySink Instance { get; } = new();

    private NullDbObservabilitySink()
    {
    }

    public void Write(DbQueryLogEntry entry)
    {
    }
}

public sealed record DbQueryLogEntry(
    string QueryId,
    int DurationMs,
    int? RowsReturned,
    int? RowsAffected,
    bool ErrorFlag,
    string? ErrorType,
    string? Sql,
    IReadOnlyDictionary<string, string>? Parameters);

public sealed record DbQueryMetric(
    string QueryId,
    int ExecutionCount,
    long TotalDurationMs,
    double AverageDurationMs,
    int ErrorCount);

public sealed record DbMetricsSnapshot(
    int TotalQueryCount,
    long TotalExecutionTimeMs,
    double ErrorRate,
    IReadOnlyList<DbQueryMetric> PerQuery);

public sealed class DbObservabilityRuntime
{
    private readonly DbObservabilityOptions options;
    private readonly IDbObservabilitySink sink;
    private readonly object metricsLock = new();
    private readonly Dictionary<string, MetricAccumulator> metrics = new(StringComparer.Ordinal);
    private int totalQueryCount;
    private long totalExecutionTimeMs;
    private int totalErrorCount;

    public DbObservabilityRuntime(DbObservabilityOptions? options = null, IDbObservabilitySink? sink = null)
    {
        this.options = options ?? DbObservabilityOptions.FromEnvironment();
        this.sink = sink ?? NullDbObservabilitySink.Instance;
    }

    public int ExecuteNonQuery(
        string sql,
        IReadOnlyDictionary<string, object?>? parameters,
        Func<string, IReadOnlyDictionary<string, object?>?, int> execute)
    {
        return ExecuteCore(sql, parameters, execute, rowsAffected => (null, rowsAffected));
    }

    public IReadOnlyList<T> ExecuteQuery<T>(
        string sql,
        IReadOnlyDictionary<string, object?>? parameters,
        Func<string, IReadOnlyDictionary<string, object?>?, IReadOnlyList<T>> execute)
    {
        return ExecuteCore(sql, parameters, execute, rows => (rows.Count, null));
    }

    public T ExecuteScalar<T>(
        string sql,
        IReadOnlyDictionary<string, object?>? parameters,
        Func<string, IReadOnlyDictionary<string, object?>?, T> execute)
    {
        return ExecuteCore(sql, parameters, execute, _ => ((int?)null, null));
    }

    public DbMetricsSnapshot GetMetricsSnapshot()
    {
        lock (metricsLock)
        {
            var perQuery = metrics
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => entry.Value.ToMetric(entry.Key))
                .ToList();

            var errorRate = totalQueryCount == 0
                ? 0.0
                : (double)totalErrorCount / totalQueryCount;

            return new DbMetricsSnapshot(
                TotalQueryCount: totalQueryCount,
                TotalExecutionTimeMs: totalExecutionTimeMs,
                ErrorRate: errorRate,
                PerQuery: perQuery);
        }
    }

    private T ExecuteCore<T>(
        string sql,
        IReadOnlyDictionary<string, object?>? parameters,
        Func<string, IReadOnlyDictionary<string, object?>?, T> execute,
        Func<T, (int? rowsReturned, int? rowsAffected)> rowCounter)
    {
        var queryId = DbQueryFingerprint.CreateQueryId(sql);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = execute(sql, parameters);
            stopwatch.Stop();

            var durationMs = (int)stopwatch.ElapsedMilliseconds;
            var (rowsReturned, rowsAffected) = rowCounter(result);

            RecordMetric(queryId, durationMs, error: false);
            MaybeLog(queryId, durationMs, rowsReturned, rowsAffected, errorFlag: false, errorType: null, sql, parameters);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var durationMs = (int)stopwatch.ElapsedMilliseconds;

            RecordMetric(queryId, durationMs, error: true);
            MaybeLog(queryId, durationMs, rowsReturned: null, rowsAffected: null, errorFlag: true, errorType: ex.GetType().Name, sql, parameters);

            throw;
        }
    }

    private void RecordMetric(string queryId, int durationMs, bool error)
    {
        if (!options.Profile)
        {
            return;
        }

        lock (metricsLock)
        {
            totalQueryCount++;
            totalExecutionTimeMs += durationMs;
            if (error)
            {
                totalErrorCount++;
            }

            if (!metrics.TryGetValue(queryId, out var accumulator))
            {
                accumulator = new MetricAccumulator();
                metrics[queryId] = accumulator;
            }

            accumulator.ExecutionCount++;
            accumulator.TotalDurationMs += durationMs;
            if (error)
            {
                accumulator.ErrorCount++;
            }
        }
    }

    private void MaybeLog(
        string queryId,
        int durationMs,
        int? rowsReturned,
        int? rowsAffected,
        bool errorFlag,
        string? errorType,
        string sql,
        IReadOnlyDictionary<string, object?>? parameters)
    {
        if (!options.ShouldLogQuery(durationMs))
        {
            return;
        }

        var loggedSql = options.LogSql ? sql : null;
        var loggedParams = options.LogParameters ? DbParameterMasker.Mask(parameters) : null;

        sink.Write(new DbQueryLogEntry(
            QueryId: queryId,
            DurationMs: durationMs,
            RowsReturned: rowsReturned,
            RowsAffected: rowsAffected,
            ErrorFlag: errorFlag,
            ErrorType: errorType,
            Sql: loggedSql,
            Parameters: loggedParams));
    }

    private sealed class MetricAccumulator
    {
        public int ExecutionCount { get; set; }

        public long TotalDurationMs { get; set; }

        public int ErrorCount { get; set; }

        public DbQueryMetric ToMetric(string queryId)
        {
            var average = ExecutionCount == 0
                ? 0.0
                : (double)TotalDurationMs / ExecutionCount;

            return new DbQueryMetric(queryId, ExecutionCount, TotalDurationMs, average, ErrorCount);
        }
    }
}

public static class DbParameterMasker
{
    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "pwd",
        "secret",
        "token",
        "access_token",
        "refresh_token",
        "api_key",
        "apikey",
        "authorization",
        "auth"
    };

    public static IReadOnlyDictionary<string, string>? Mask(IReadOnlyDictionary<string, object?>? parameters)
    {
        if (parameters is null || parameters.Count == 0)
        {
            return null;
        }

        var result = new Dictionary<string, string>(parameters.Count, StringComparer.Ordinal);
        foreach (var (name, value) in parameters)
        {
            result[name] = MaskValue(name, value);
        }

        return result;
    }

    private static string MaskValue(string name, object? value)
    {
        if (SensitiveNames.Contains(NormalizeSensitiveKey(name)))
        {
            return "***";
        }

        if (value is null)
        {
            return "null";
        }

        if (value is string text)
        {
            return text.Length <= 64 ? text : text[..64] + "...";
        }

        if (value is byte[] bytes)
        {
            return $"<bytes:{bytes.Length}>";
        }

        return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "<unprintable>";
    }

    private static string NormalizeSensitiveKey(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        return name.TrimStart('@', ':', '$');
    }
}
