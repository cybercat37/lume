using Axom.Runtime.Db;

namespace Axom.Cli;

internal sealed record DbVerifyEnvironment(
    string Provider,
    string? ConnectionString,
    ISqlRecordProjectionResolver? RecordProjectionResolver);

internal static class DbVerifyEnvironmentLoader
{
    public static bool TryLoad(out DbVerifyEnvironment environment, out string? error)
    {
        environment = null!;
        error = null;

        var configuredProvider = Environment.GetEnvironmentVariable("AXOM_DB_PROVIDER");
        var provider = string.IsNullOrWhiteSpace(configuredProvider)
            ? "sqlite"
            : configuredProvider.Trim().ToLowerInvariant();

        var configuredConnectionString = Environment.GetEnvironmentVariable("AXOM_DB_CONNECTION_STRING");
        var connectionString = string.IsNullOrWhiteSpace(configuredConnectionString)
            ? null
            : configuredConnectionString;

        if (!TryCreateRecordProjectionResolverFromEnvironment(out var recordProjectionResolver, out error))
        {
            return false;
        }

        environment = new DbVerifyEnvironment(provider, connectionString, recordProjectionResolver);
        return true;
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
}
