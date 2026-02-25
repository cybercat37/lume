using Axom.Runtime.Db;
using Microsoft.Data.Sqlite;

namespace Axom.Cli;

public static class DbRuntimeBootstrap
{
    public static bool ConfigureFromEnvironment(TextWriter? errorWriter = null)
    {
        errorWriter ??= Console.Error;

        var provider = Environment.GetEnvironmentVariable("AXOM_DB_PROVIDER");
        var connectionString = Environment.GetEnvironmentVariable("AXOM_DB_CONNECTION_STRING");
        var projectionMapValue = Environment.GetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS");

        if (string.IsNullOrWhiteSpace(provider) && string.IsNullOrWhiteSpace(connectionString))
        {
            DbBuiltinGateway.Reset();
            return false;
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            errorWriter.WriteLine("AXOM_DB_PROVIDER is required when AXOM_DB_CONNECTION_STRING is set.");
            DbBuiltinGateway.Reset();
            return false;
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errorWriter.WriteLine("AXOM_DB_CONNECTION_STRING is required when AXOM_DB_PROVIDER is set.");
            DbBuiltinGateway.Reset();
            return false;
        }

        if (!string.Equals(provider, "sqlite", StringComparison.OrdinalIgnoreCase))
        {
            errorWriter.WriteLine($"Unsupported AXOM_DB_PROVIDER '{provider}'. Supported providers: sqlite.");
            DbBuiltinGateway.Reset();
            return false;
        }

        var options = DbObservabilityOptions.FromEnvironment();
        IDbObservabilitySink sink = options.IsQueryLoggingEnabled
            ? new ConsoleDbObservabilitySink(errorWriter)
            : NullDbObservabilitySink.Instance;
        var observability = new DbObservabilityRuntime(options, sink);

        if (!TryParseRecordProjectionResolver(projectionMapValue, out var projectionResolver, out var projectionError))
        {
            errorWriter.WriteLine(projectionError);
            DbBuiltinGateway.Reset();
            return false;
        }

        var adapter = new AdoNetDbAdapter(
            () => new SqliteConnection(connectionString),
            observability,
            projectionResolver);

        DbBuiltinGateway.Configure(adapter);
        return true;
    }

    private static bool TryParseRecordProjectionResolver(
        string? raw,
        out ISqlRecordProjectionResolver? resolver,
        out string? error)
    {
        resolver = null;
        error = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        var records = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var declarations = raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var declaration in declarations)
        {
            var parts = declaration.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                error = $"Invalid AXOM_DB_RECORD_PROJECTIONS entry '{declaration}'. Expected format Record:col1,col2.";
                return false;
            }

            var recordName = parts[0];
            var columns = parts[1]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();

            if (columns.Length == 0)
            {
                error = $"Invalid AXOM_DB_RECORD_PROJECTIONS entry '{declaration}'. Record '{recordName}' must list at least one column.";
                return false;
            }

            records[recordName] = columns;
        }

        try
        {
            resolver = new DictionarySqlRecordProjectionResolver(records);
            return true;
        }
        catch (ArgumentException ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
