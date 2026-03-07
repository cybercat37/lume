using Axom.Cli;

namespace Axom.Tests;

public class DbVerifyParameterSeedBuilderTests
{
    [Fact]
    public void Build_collects_lowercase_placeholders_once()
    {
        var sql = "select * from users where id = {id} and status = {status} and id = {id}";

        var seed = DbVerifyParameterSeedBuilder.Build(sql);

        Assert.Equal(2, seed.Count);
        Assert.Contains("id", seed.Keys, StringComparer.Ordinal);
        Assert.Contains("status", seed.Keys, StringComparer.Ordinal);
    }

    [Fact]
    public void Build_ignores_record_projection_placeholders()
    {
        var sql = "select {UserRecord} from users where id = {id}";

        var seed = DbVerifyParameterSeedBuilder.Build(sql);

        Assert.Single(seed);
        Assert.Contains("id", seed.Keys, StringComparer.Ordinal);
    }

    [Fact]
    public void Build_ignores_braces_inside_single_quoted_literals()
    {
        var sql = "select '{ignored}', name from users where id = {id} and note = '{also_ignored}'";

        var seed = DbVerifyParameterSeedBuilder.Build(sql);

        Assert.Single(seed);
        Assert.Contains("id", seed.Keys, StringComparer.Ordinal);
    }

    [Fact]
    public void Build_ignores_non_identifier_placeholders()
    {
        var sql = "select * from users where id = {user-id} and role = {role}";

        var seed = DbVerifyParameterSeedBuilder.Build(sql);

        Assert.Single(seed);
        Assert.Contains("role", seed.Keys, StringComparer.Ordinal);
    }
}
