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
        var adapter = new AdoNetDbAdapter(() => new SqliteConnection(connectionString), observability);

        DbBuiltinGateway.Configure(adapter);
        return true;
    }
}
