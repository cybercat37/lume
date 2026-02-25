using System.Globalization;

namespace Axom.Runtime.Db;

public sealed class ConsoleDbObservabilitySink : IDbObservabilitySink
{
    private readonly TextWriter writer;

    public ConsoleDbObservabilitySink(TextWriter? writer = null)
    {
        this.writer = writer ?? Console.Error;
    }

    public void Write(DbQueryLogEntry entry)
    {
        var rowInfo = entry.RowsReturned is not null
            ? $"rows_returned={entry.RowsReturned.Value.ToString(CultureInfo.InvariantCulture)}"
            : (entry.RowsAffected is not null
                ? $"rows_affected={entry.RowsAffected.Value.ToString(CultureInfo.InvariantCulture)}"
                : "rows=n/a");

        writer.WriteLine($"db query_id={entry.QueryId} duration_ms={entry.DurationMs.ToString(CultureInfo.InvariantCulture)} {rowInfo} error={(entry.ErrorFlag ? "1" : "0")}");

        if (!string.IsNullOrWhiteSpace(entry.Sql))
        {
            writer.WriteLine($"db sql={entry.Sql}");
        }

        if (entry.Parameters is { Count: > 0 })
        {
            var serialized = string.Join(", ", entry.Parameters.Select(pair => $"{pair.Key}={pair.Value}"));
            writer.WriteLine($"db params={serialized}");
        }

        if (entry.ErrorFlag && !string.IsNullOrWhiteSpace(entry.ErrorType))
        {
            writer.WriteLine($"db error_type={entry.ErrorType}");
        }
    }
}
