using Axom.Runtime.Db;

namespace Axom.Tests;

public class SqlTemplateBinderTests
{
    [Fact]
    public void Try_bind_rewrites_placeholders_to_named_parameters()
    {
        var sql = "select * from users where id = {id} and name = {name}";
        var parameters = new Dictionary<string, object?>
        {
            ["id"] = 7,
            ["name"] = "Ada",
            ["ignored"] = "x"
        };

        var ok = SqlTemplateBinder.TryBind(sql, parameters, out var boundSql, out var boundParameters, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal("select * from users where id = @id and name = @name", boundSql);
        Assert.Equal(2, boundParameters.Count);
        Assert.Equal(7, boundParameters["id"]);
        Assert.Equal("Ada", boundParameters["name"]);
    }

    [Fact]
    public void Try_bind_ignores_braces_inside_single_quoted_strings()
    {
        var sql = "select '{id}' as literal, id from users where id = {id}";
        var parameters = new Dictionary<string, object?> { ["id"] = 3 };

        var ok = SqlTemplateBinder.TryBind(sql, parameters, out var boundSql, out _, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal("select '{id}' as literal, id from users where id = @id", boundSql);
    }

    [Fact]
    public void Try_bind_fails_for_missing_parameter()
    {
        var ok = SqlTemplateBinder.TryBind(
            "select * from users where id = {id}",
            new Dictionary<string, object?>(),
            out _,
            out _,
            out var error);

        Assert.False(ok);
        Assert.Contains("Missing SQL parameter 'id'", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Try_bind_fails_for_record_placeholder_until_mapping_is_implemented()
    {
        var ok = SqlTemplateBinder.TryBind(
            "select {User} from users",
            new Dictionary<string, object?>(),
            out _,
            out _,
            out var error);

        Assert.False(ok);
        Assert.Contains("requires a record projection resolver", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Try_bind_expands_record_placeholder_with_projection_resolver()
    {
        var resolver = new DictionarySqlRecordProjectionResolver(
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                ["User"] = new[] { "id", "name" }
            });

        var ok = SqlTemplateBinder.TryBind(
            "select {User} from users where id = {id}",
            new Dictionary<string, object?> { ["id"] = 1 },
            resolver,
            out var boundSql,
            out var boundParameters,
            out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal("select id, name from users where id = @id", boundSql);
        Assert.Single(boundParameters);
        Assert.Equal(1, boundParameters["id"]);
    }
}
