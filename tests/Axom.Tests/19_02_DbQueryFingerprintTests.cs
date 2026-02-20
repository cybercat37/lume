using Axom.Runtime.Db;

namespace Axom.Tests;

public class DbQueryFingerprintTests
{
    [Fact]
    public void Normalize_collapses_whitespace_and_casing()
    {
        var normalized = DbQueryFingerprint.Normalize("SELECT   *\nFROM Users\tWHERE id = 42");

        Assert.Equal("select * from users where id = ?", normalized);
    }

    [Fact]
    public void Normalize_removes_string_and_numeric_literals()
    {
        var normalized = DbQueryFingerprint.Normalize(
            "select * from users where name = 'Alice' and age >= 30 and score = 18.5");

        Assert.Equal("select * from users where name = ? and age >= ? and score = ?", normalized);
    }

    [Fact]
    public void Normalize_standardizes_parameter_placeholders()
    {
        var sql = "select * from users where id = @id and org = :org and seq = $1 and flag = ?";

        var normalized = DbQueryFingerprint.Normalize(sql);

        Assert.Equal("select * from users where id = ? and org = ? and seq = ? and flag = ?", normalized);
    }

    [Fact]
    public void Create_query_id_is_stable_for_semantically_equal_queries()
    {
        var left = "select * from users where id = 42 and name = 'a'";
        var right = "SELECT * FROM users WHERE id = 7 and name = 'b'";

        var leftId = DbQueryFingerprint.CreateQueryId(left);
        var rightId = DbQueryFingerprint.CreateQueryId(right);

        Assert.Equal(leftId, rightId);
    }

    [Fact]
    public void Create_query_id_differs_for_structurally_different_queries()
    {
        var left = DbQueryFingerprint.CreateQueryId("select * from users where id = 1");
        var right = DbQueryFingerprint.CreateQueryId("select * from users where org_id = 1");

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Empty_query_maps_to_deterministic_hash()
    {
        var a = DbQueryFingerprint.CreateQueryId(string.Empty);
        var b = DbQueryFingerprint.CreateQueryId("   ");

        Assert.Equal(a, b);
        Assert.Equal(64, a.Length);
    }
}
