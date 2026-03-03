using Axom.Runtime.Db;
using Microsoft.Data.Sqlite;

namespace Axom.Tests;

public class AdoNetDbAdapterTests
{
    [Fact]
    public void Adapter_exec_query_and_scalar_work_with_observability_logging()
    {
        using var fixture = SqliteFixture.Create();
        var sink = new TestSink();
        var options = new DbObservabilityOptions(
            DbQueryLogMode.All,
            LogSql: false,
            LogParameters: false,
            Profile: true,
            SlowThresholdMs: null,
            Plan: false,
            PlanAnalyze: false);

        var adapter = new AdoNetDbAdapter(
            fixture.CreateConnection,
            new DbObservabilityRuntime(options, sink));

        adapter.Exec("create table users (id integer primary key, name text not null)");
        adapter.Exec(
            "insert into users (id, name) values (@id, @name)",
            new Dictionary<string, object?>
            {
                ["id"] = 42,
                ["name"] = "Ada"
            });

        var rows = adapter.Query(
            "select id, name from users where id = @id",
            new Dictionary<string, object?> { ["id"] = 42 });

        var count = adapter.Scalar<int>("select count(*) from users");

        Assert.Single(rows);
        Assert.Equal(42L, rows[0]["id"]);
        Assert.Equal("Ada", rows[0]["name"]);
        Assert.Equal(1, count);

        Assert.Equal(4, sink.Entries.Count);
        Assert.All(sink.Entries, entry => Assert.False(string.IsNullOrWhiteSpace(entry.QueryId)));
        Assert.All(sink.Entries, entry => Assert.Null(entry.Sql));

        var metrics = adapter.GetMetricsSnapshot();
        Assert.Equal(4, metrics.TotalQueryCount);
    }

    [Fact]
    public void Adapter_uses_observability_wrapper_without_changing_sql_or_parameters()
    {
        using var fixture = SqliteFixture.Create();
        var options = new DbObservabilityOptions(
            DbQueryLogMode.All,
            LogSql: true,
            LogParameters: true,
            Profile: true,
            SlowThresholdMs: null,
            Plan: false,
            PlanAnalyze: false);

        var sink = new TestSink();
        var adapter = new AdoNetDbAdapter(
            fixture.CreateConnection,
            new DbObservabilityRuntime(options, sink));

        adapter.Exec("create table users (id integer primary key, secret text)");

        var sql = "insert into users (id, secret) values (:id, :secret)";
        var parameters = new Dictionary<string, object?>
        {
            [":id"] = 7,
            [":secret"] = "top-secret-value"
        };

        var affected = adapter.Exec(sql, parameters);

        Assert.Equal(1, affected);

        var entry = sink.Entries.Last();
        Assert.Equal(sql, entry.Sql);
        Assert.NotNull(entry.Parameters);
        Assert.Equal("7", entry.Parameters![":id"]);
        Assert.NotEqual("top-secret-value", entry.Parameters[":secret"]);

        var roundtrip = adapter.Scalar<string>(
            "select secret from users where id = :id",
            new Dictionary<string, object?> { [":id"] = 7 });

        Assert.Equal("top-secret-value", roundtrip);
    }

    [Fact]
    public void Adapter_error_path_is_logged_and_rethrown()
    {
        using var fixture = SqliteFixture.Create();
        var sink = new TestSink();
        var options = new DbObservabilityOptions(
            DbQueryLogMode.All,
            LogSql: true,
            LogParameters: false,
            Profile: true,
            SlowThresholdMs: null,
            Plan: false,
            PlanAnalyze: false);

        var adapter = new AdoNetDbAdapter(
            fixture.CreateConnection,
            new DbObservabilityRuntime(options, sink));

        Assert.Throws<SqliteException>(() => adapter.Query("select * from missing_table"));

        var entry = Assert.Single(sink.Entries);
        Assert.True(entry.ErrorFlag);
        Assert.Equal("SqliteException", entry.ErrorType);
        Assert.Equal("select * from missing_table", entry.Sql);

        var snapshot = adapter.GetMetricsSnapshot();
        Assert.Equal(1, snapshot.TotalQueryCount);
        Assert.Equal(1.0, snapshot.ErrorRate);
    }

    [Fact]
    public void Adapter_fails_fast_when_sql_template_parameter_is_missing()
    {
        using var fixture = SqliteFixture.Create();
        var adapter = new AdoNetDbAdapter(fixture.CreateConnection);

        var ex = Assert.Throws<ArgumentException>(() =>
            adapter.Query("select * from users where id = {id}"));

        Assert.Contains("Missing SQL parameter 'id'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Adapter_expands_record_projection_when_resolver_is_configured()
    {
        using var fixture = SqliteFixture.Create();
        var resolver = new DictionarySqlRecordProjectionResolver(
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                ["User"] = new[] { "id", "name" }
            });

        var adapter = new AdoNetDbAdapter(fixture.CreateConnection, recordProjectionResolver: resolver);
        adapter.Exec("create table users (id integer primary key, name text not null)");
        adapter.Exec("insert into users (id, name) values (1, 'Ada')");

        var rows = adapter.Query("select {User} from users where id = {id}", new Dictionary<string, object?> { ["id"] = 1 });

        Assert.Single(rows);
        Assert.Equal(1L, rows[0]["id"]);
        Assert.Equal("Ada", rows[0]["name"]);
    }

    [Fact]
    public void Adapter_transaction_rollback_discards_changes_and_commit_persists()
    {
        using var fixture = SqliteFixture.Create();
        var adapter = new AdoNetDbAdapter(fixture.CreateConnection);

        adapter.Exec("create table users (id integer primary key, name text not null)");

        Assert.True(adapter.TryBeginTransaction(out var beginError), beginError);
        adapter.Exec("insert into users (id, name) values (1, 'Ada')");
        Assert.True(adapter.TryRollbackTransaction(out var rollbackError), rollbackError);

        var afterRollback = adapter.Scalar<long>("select count(*) from users");
        Assert.Equal(0L, afterRollback);

        Assert.True(adapter.TryBeginTransaction(out var beginAgainError), beginAgainError);
        adapter.Exec("insert into users (id, name) values (2, 'Bob')");
        Assert.True(adapter.TryCommitTransaction(out var commitError), commitError);

        var afterCommit = adapter.Scalar<long>("select count(*) from users");
        Assert.Equal(1L, afterCommit);
    }

    [Fact]
    public void Adapter_rejects_nested_transaction_begin()
    {
        using var fixture = SqliteFixture.Create();
        var adapter = new AdoNetDbAdapter(fixture.CreateConnection);

        Assert.True(adapter.TryBeginTransaction(out var beginError), beginError);
        Assert.False(adapter.TryBeginTransaction(out var nestedError));
        Assert.Contains("already active", nestedError, StringComparison.Ordinal);
        Assert.True(adapter.TryRollbackTransaction(out var rollbackError), rollbackError);
    }

    [Fact]
    public void Adapter_commit_without_active_transaction_returns_error()
    {
        using var fixture = SqliteFixture.Create();
        var adapter = new AdoNetDbAdapter(fixture.CreateConnection);

        Assert.False(adapter.TryCommitTransaction(out var error));
        Assert.Contains("not active", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Adapter_rollback_without_active_transaction_returns_error()
    {
        using var fixture = SqliteFixture.Create();
        var adapter = new AdoNetDbAdapter(fixture.CreateConnection);

        Assert.False(adapter.TryRollbackTransaction(out var error));
        Assert.Contains("not active", error, StringComparison.Ordinal);
    }

    private sealed class TestSink : IDbObservabilitySink
    {
        public List<DbQueryLogEntry> Entries { get; } = new();

        public void Write(DbQueryLogEntry entry)
        {
            Entries.Add(entry);
        }
    }

    private sealed class SqliteFixture : IDisposable
    {
        private readonly string dbPath;

        private SqliteFixture(string dbPath)
        {
            this.dbPath = dbPath;
        }

        public static SqliteFixture Create()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "axom_sqlite_tests", Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture));
            Directory.CreateDirectory(tempRoot);
            var dbPath = Path.Combine(tempRoot, "test.db");
            return new SqliteFixture(dbPath);
        }

        public SqliteConnection CreateConnection()
        {
            return new SqliteConnection($"Data Source={dbPath}");
        }

        public void Dispose()
        {
            var parent = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent))
            {
                Directory.Delete(parent, recursive: true);
            }
        }
    }
}
