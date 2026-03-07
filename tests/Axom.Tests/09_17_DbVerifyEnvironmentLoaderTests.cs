using Axom.Cli;

namespace Axom.Tests;

[Collection("CliTests")]
public class DbVerifyEnvironmentLoaderTests
{
    [Fact]
    public void Try_load_defaults_to_sqlite_when_provider_is_missing()
    {
        var previousProvider = Environment.GetEnvironmentVariable("AXOM_DB_PROVIDER");
        var previousConnectionString = Environment.GetEnvironmentVariable("AXOM_DB_CONNECTION_STRING");
        var previousRecordProjections = Environment.GetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS");

        try
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", null);
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", null);
            Environment.SetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS", null);

            var success = DbVerifyEnvironmentLoader.TryLoad(out var environment, out var error);

            Assert.True(success);
            Assert.Null(error);
            Assert.Equal("sqlite", environment.Provider);
            Assert.Null(environment.ConnectionString);
            Assert.Null(environment.RecordProjectionResolver);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", previousProvider);
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", previousConnectionString);
            Environment.SetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS", previousRecordProjections);
        }
    }

    [Fact]
    public void Try_load_normalizes_provider_and_keeps_connection_string()
    {
        var previousProvider = Environment.GetEnvironmentVariable("AXOM_DB_PROVIDER");
        var previousConnectionString = Environment.GetEnvironmentVariable("AXOM_DB_CONNECTION_STRING");
        var previousRecordProjections = Environment.GetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS");

        try
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", "  POSTGRES ");
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", "Host=localhost;Database=axom");
            Environment.SetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS", null);

            var success = DbVerifyEnvironmentLoader.TryLoad(out var environment, out var error);

            Assert.True(success);
            Assert.Null(error);
            Assert.Equal("postgres", environment.Provider);
            Assert.Equal("Host=localhost;Database=axom", environment.ConnectionString);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", previousProvider);
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", previousConnectionString);
            Environment.SetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS", previousRecordProjections);
        }
    }

    [Fact]
    public void Try_load_returns_error_for_invalid_record_projection_entry()
    {
        var previousProvider = Environment.GetEnvironmentVariable("AXOM_DB_PROVIDER");
        var previousConnectionString = Environment.GetEnvironmentVariable("AXOM_DB_CONNECTION_STRING");
        var previousRecordProjections = Environment.GetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS");

        try
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", null);
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", null);
            Environment.SetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS", "InvalidEntry");

            var success = DbVerifyEnvironmentLoader.TryLoad(out _, out var error);

            Assert.False(success);
            Assert.Contains("Invalid AXOM_DB_RECORD_PROJECTIONS entry", error, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", previousProvider);
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", previousConnectionString);
            Environment.SetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS", previousRecordProjections);
        }
    }

    [Fact]
    public void Try_load_builds_record_projection_resolver_when_mapping_is_valid()
    {
        var previousProvider = Environment.GetEnvironmentVariable("AXOM_DB_PROVIDER");
        var previousConnectionString = Environment.GetEnvironmentVariable("AXOM_DB_CONNECTION_STRING");
        var previousRecordProjections = Environment.GetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS");

        try
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", null);
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", null);
            Environment.SetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS", "User:id,name");

            var success = DbVerifyEnvironmentLoader.TryLoad(out var environment, out var error);

            Assert.True(success);
            Assert.Null(error);
            Assert.NotNull(environment.RecordProjectionResolver);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AXOM_DB_PROVIDER", previousProvider);
            Environment.SetEnvironmentVariable("AXOM_DB_CONNECTION_STRING", previousConnectionString);
            Environment.SetEnvironmentVariable("AXOM_DB_RECORD_PROJECTIONS", previousRecordProjections);
        }
    }
}
