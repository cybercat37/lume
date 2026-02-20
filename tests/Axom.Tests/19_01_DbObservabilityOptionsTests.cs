using Axom.Runtime.Db;

namespace Axom.Tests;

public class DbObservabilityOptionsTests
{
    [Fact]
    public void From_environment_defaults_to_silent_mode()
    {
        var options = DbObservabilityOptions.FromEnvironment(_ => null);

        Assert.Equal(DbQueryLogMode.Off, options.QueryLogMode);
        Assert.False(options.LogSql);
        Assert.False(options.LogParameters);
        Assert.False(options.Profile);
        Assert.Null(options.SlowThresholdMs);
        Assert.False(options.Plan);
        Assert.False(options.PlanAnalyze);
        Assert.False(options.IsPlanAnalyzeEnabled);
        Assert.False(options.IsQueryLoggingEnabled);
        Assert.False(options.ShouldLogQuery(500));
    }

    [Fact]
    public void From_environment_parses_explicit_opt_in_flags()
    {
        var env = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AXOM_DB_LOG"] = "all",
            ["AXOM_DB_LOG_SQL"] = "1",
            ["AXOM_DB_LOG_PARAMS"] = "1",
            ["AXOM_DB_PROFILE"] = "1",
            ["AXOM_DB_SLOW_MS"] = "200",
            ["AXOM_DB_PLAN"] = "1",
            ["AXOM_DB_PLAN_ANALYZE"] = "1"
        };

        var options = DbObservabilityOptions.FromEnvironment(name =>
            env.TryGetValue(name, out var value) ? value : null);

        Assert.Equal(DbQueryLogMode.All, options.QueryLogMode);
        Assert.True(options.LogSql);
        Assert.True(options.LogParameters);
        Assert.True(options.Profile);
        Assert.Equal(200, options.SlowThresholdMs);
        Assert.True(options.Plan);
        Assert.True(options.PlanAnalyze);
        Assert.True(options.IsPlanAnalyzeEnabled);
        Assert.True(options.IsQueryLoggingEnabled);
        Assert.True(options.ShouldLogQuery(null));
    }

    [Fact]
    public void Slow_log_mode_requires_threshold_and_duration()
    {
        var env = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AXOM_DB_LOG"] = "slow"
        };

        var withoutThreshold = DbObservabilityOptions.FromEnvironment(name =>
            env.TryGetValue(name, out var value) ? value : null);

        Assert.Equal(DbQueryLogMode.Slow, withoutThreshold.QueryLogMode);
        Assert.False(withoutThreshold.ShouldLogQuery(1000));

        env["AXOM_DB_SLOW_MS"] = "250";
        var withThreshold = DbObservabilityOptions.FromEnvironment(name =>
            env.TryGetValue(name, out var value) ? value : null);

        Assert.False(withThreshold.ShouldLogQuery(null));
        Assert.False(withThreshold.ShouldLogQuery(249));
        Assert.True(withThreshold.ShouldLogQuery(250));
        Assert.True(withThreshold.ShouldLogQuery(500));
    }

    [Fact]
    public void Invalid_values_fall_back_to_safe_defaults()
    {
        var env = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AXOM_DB_LOG"] = "loud",
            ["AXOM_DB_LOG_SQL"] = "true",
            ["AXOM_DB_LOG_PARAMS"] = "-1",
            ["AXOM_DB_PROFILE"] = "yes",
            ["AXOM_DB_SLOW_MS"] = "0",
            ["AXOM_DB_PLAN"] = "2",
            ["AXOM_DB_PLAN_ANALYZE"] = "1"
        };

        var options = DbObservabilityOptions.FromEnvironment(name =>
            env.TryGetValue(name, out var value) ? value : null);

        Assert.Equal(DbQueryLogMode.Off, options.QueryLogMode);
        Assert.False(options.LogSql);
        Assert.False(options.LogParameters);
        Assert.False(options.Profile);
        Assert.Null(options.SlowThresholdMs);
        Assert.False(options.Plan);
        Assert.True(options.PlanAnalyze);
        Assert.False(options.IsPlanAnalyzeEnabled);
    }
}
