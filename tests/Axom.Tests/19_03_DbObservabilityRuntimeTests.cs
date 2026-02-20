using Axom.Runtime.Db;

namespace Axom.Tests;

public class DbObservabilityRuntimeTests
{
    [Fact]
    public void Default_silent_mode_does_not_emit_logs_or_metrics()
    {
        var sink = new TestSink();
        var runtime = new DbObservabilityRuntime(DbObservabilityOptions.Default, sink);

        var result = runtime.ExecuteScalar("select 1", null, static (sql, _) => sql.Length);

        Assert.Equal(8, result);
        Assert.Empty(sink.Entries);

        var snapshot = runtime.GetMetricsSnapshot();
        Assert.Equal(0, snapshot.TotalQueryCount);
        Assert.Equal(0, snapshot.TotalExecutionTimeMs);
        Assert.Equal(0.0, snapshot.ErrorRate);
        Assert.Empty(snapshot.PerQuery);
    }

    [Fact]
    public void All_mode_emits_query_log_without_sql_when_not_requested()
    {
        var sink = new TestSink();
        var options = new DbObservabilityOptions(
            DbQueryLogMode.All,
            LogSql: false,
            LogParameters: false,
            Profile: false,
            SlowThresholdMs: null,
            Plan: false,
            PlanAnalyze: false);

        var runtime = new DbObservabilityRuntime(options, sink);
        var sql = "select * from users where id = 42";
        var rows = runtime.ExecuteQuery(sql, null, static (_, _) => (IReadOnlyList<int>)new[] { 1, 2, 3 });

        Assert.Equal(3, rows.Count);
        Assert.Single(sink.Entries);

        var entry = sink.Entries[0];
        Assert.Equal(DbQueryFingerprint.CreateQueryId(sql), entry.QueryId);
        Assert.Equal(3, entry.RowsReturned);
        Assert.False(entry.ErrorFlag);
        Assert.Null(entry.Sql);
        Assert.Null(entry.Parameters);
    }

    [Fact]
    public void Parameter_logging_masks_sensitive_values()
    {
        var sink = new TestSink();
        var options = new DbObservabilityOptions(
            DbQueryLogMode.All,
            LogSql: true,
            LogParameters: true,
            Profile: false,
            SlowThresholdMs: null,
            Plan: false,
            PlanAnalyze: false);

        var runtime = new DbObservabilityRuntime(options, sink);
        var parameters = new Dictionary<string, object?>
        {
            ["password"] = "super-secret",
            ["token"] = "abc",
            ["payload"] = new string('x', 80),
            ["blob"] = new byte[] { 1, 2, 3, 4, 5 }
        };

        runtime.ExecuteNonQuery(
            "update users set password = @password",
            parameters,
            static (_, _) => 1);

        var entry = Assert.Single(sink.Entries);
        Assert.Equal("update users set password = @password", entry.Sql);
        Assert.NotNull(entry.Parameters);
        Assert.Equal("***", entry.Parameters!["password"]);
        Assert.Equal("***", entry.Parameters["token"]);
        Assert.EndsWith("...", entry.Parameters["payload"], StringComparison.Ordinal);
        Assert.Equal("<bytes:5>", entry.Parameters["blob"]);
    }

    [Fact]
    public void Slow_mode_logs_only_queries_above_threshold()
    {
        var sink = new TestSink();
        var options = new DbObservabilityOptions(
            DbQueryLogMode.Slow,
            LogSql: false,
            LogParameters: false,
            Profile: false,
            SlowThresholdMs: 10,
            Plan: false,
            PlanAnalyze: false);

        var runtime = new DbObservabilityRuntime(options, sink);

        runtime.ExecuteScalar("select 1", null, static (_, _) => 1);
        runtime.ExecuteScalar("select pg_sleep", null, static (_, _) =>
        {
            Thread.Sleep(20);
            return 1;
        });

        Assert.Single(sink.Entries);
    }

    [Fact]
    public void Profile_mode_tracks_metrics_when_logging_is_off()
    {
        var options = new DbObservabilityOptions(
            DbQueryLogMode.Off,
            LogSql: false,
            LogParameters: false,
            Profile: true,
            SlowThresholdMs: null,
            Plan: false,
            PlanAnalyze: false);

        var runtime = new DbObservabilityRuntime(options, NullDbObservabilitySink.Instance);
        var sql = "select * from users where id = 42";

        runtime.ExecuteScalar(sql, null, static (_, _) => 1);
        runtime.ExecuteScalar(sql, null, static (_, _) => 2);

        var snapshot = runtime.GetMetricsSnapshot();
        Assert.Equal(2, snapshot.TotalQueryCount);
        Assert.Single(snapshot.PerQuery);
        Assert.Equal(2, snapshot.PerQuery[0].ExecutionCount);
        Assert.Equal(0, snapshot.PerQuery[0].ErrorCount);
    }

    [Fact]
    public void Wrapper_preserves_sql_and_exception_semantics()
    {
        var options = new DbObservabilityOptions(
            DbQueryLogMode.All,
            LogSql: true,
            LogParameters: false,
            Profile: true,
            SlowThresholdMs: null,
            Plan: false,
            PlanAnalyze: false);
        var sink = new TestSink();
        var runtime = new DbObservabilityRuntime(options, sink);

        var sql = "select * from users where id = @id";
        var parameters = new Dictionary<string, object?> { ["id"] = 7 };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            runtime.ExecuteScalar<int>(sql, parameters, (receivedSql, receivedParams) =>
            {
                Assert.Equal(sql, receivedSql);
                Assert.True(ReferenceEquals(parameters, receivedParams));
                throw new InvalidOperationException("boom");
            }));

        Assert.Equal("boom", exception.Message);

        var entry = Assert.Single(sink.Entries);
        Assert.True(entry.ErrorFlag);
        Assert.Equal("InvalidOperationException", entry.ErrorType);

        var snapshot = runtime.GetMetricsSnapshot();
        Assert.Equal(1, snapshot.TotalQueryCount);
        Assert.Equal(1, snapshot.PerQuery[0].ErrorCount);
        Assert.Equal(1.0, snapshot.ErrorRate);
    }

    private sealed class TestSink : IDbObservabilitySink
    {
        public List<DbQueryLogEntry> Entries { get; } = new();

        public void Write(DbQueryLogEntry entry)
        {
            Entries.Add(entry);
        }
    }
}
