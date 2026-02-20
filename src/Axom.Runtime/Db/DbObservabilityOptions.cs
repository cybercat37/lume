namespace Axom.Runtime.Db;

public enum DbQueryLogMode
{
    Off,
    Slow,
    All
}

public sealed record DbObservabilityOptions(
    DbQueryLogMode QueryLogMode,
    bool LogSql,
    bool LogParameters,
    bool Profile,
    int? SlowThresholdMs,
    bool Plan,
    bool PlanAnalyze)
{
    public static DbObservabilityOptions Default { get; } = new(
        DbQueryLogMode.Off,
        LogSql: false,
        LogParameters: false,
        Profile: false,
        SlowThresholdMs: null,
        Plan: false,
        PlanAnalyze: false);

    public bool IsQueryLoggingEnabled => QueryLogMode != DbQueryLogMode.Off;

    public bool IsPlanAnalyzeEnabled => Plan && PlanAnalyze;

    public bool ShouldLogQuery(int? durationMs)
    {
        if (QueryLogMode == DbQueryLogMode.All)
        {
            return true;
        }

        if (QueryLogMode != DbQueryLogMode.Slow || SlowThresholdMs is null || durationMs is null)
        {
            return false;
        }

        return durationMs.Value >= SlowThresholdMs.Value;
    }

    public static DbObservabilityOptions FromEnvironment(Func<string, string?>? readVariable = null)
    {
        readVariable ??= Environment.GetEnvironmentVariable;

        return new DbObservabilityOptions(
            QueryLogMode: ParseLogMode(readVariable("AXOM_DB_LOG")),
            LogSql: ParseBinaryFlag(readVariable("AXOM_DB_LOG_SQL")),
            LogParameters: ParseBinaryFlag(readVariable("AXOM_DB_LOG_PARAMS")),
            Profile: ParseBinaryFlag(readVariable("AXOM_DB_PROFILE")),
            SlowThresholdMs: ParsePositiveInt(readVariable("AXOM_DB_SLOW_MS")),
            Plan: ParseBinaryFlag(readVariable("AXOM_DB_PLAN")),
            PlanAnalyze: ParseBinaryFlag(readVariable("AXOM_DB_PLAN_ANALYZE")));
    }

    private static DbQueryLogMode ParseLogMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DbQueryLogMode.Off;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "off" => DbQueryLogMode.Off,
            "slow" => DbQueryLogMode.Slow,
            "all" => DbQueryLogMode.All,
            _ => DbQueryLogMode.Off
        };
    }

    private static bool ParseBinaryFlag(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return string.Equals(value.Trim(), "1", StringComparison.Ordinal);
    }

    private static int? ParsePositiveInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!int.TryParse(value.Trim(), out var parsed) || parsed <= 0)
        {
            return null;
        }

        return parsed;
    }
}
