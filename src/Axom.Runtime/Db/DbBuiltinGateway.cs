namespace Axom.Runtime.Db;

public static class DbBuiltinGateway
{
    private static readonly object Sync = new();
    private static AdoNetDbAdapter? adapter;

    public static void Configure(AdoNetDbAdapter dbAdapter)
    {
        ArgumentNullException.ThrowIfNull(dbAdapter);

        lock (Sync)
        {
            adapter = dbAdapter;
        }
    }

    public static void Reset()
    {
        lock (Sync)
        {
            adapter = null;
        }
    }

    public static bool TryExec(string sql, IReadOnlyDictionary<string, object?>? parameters, out int rowsAffected, out string? error)
    {
        rowsAffected = 0;
        error = null;

        var db = GetAdapterOrError(out error);
        if (db is null)
        {
            return false;
        }

        try
        {
            rowsAffected = db.Exec(sql, parameters);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool TryQuery(
        string sql,
        IReadOnlyDictionary<string, object?>? parameters,
        out IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        out string? error)
    {
        rows = Array.Empty<IReadOnlyDictionary<string, object?>>();
        error = null;

        var db = GetAdapterOrError(out error);
        if (db is null)
        {
            return false;
        }

        try
        {
            rows = db.Query(sql, parameters);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool TryScalar(
        string sql,
        IReadOnlyDictionary<string, object?>? parameters,
        out object? value,
        out string? error)
    {
        value = null;
        error = null;

        var db = GetAdapterOrError(out error);
        if (db is null)
        {
            return false;
        }

        try
        {
            value = db.Scalar<object>(sql, parameters);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static AdoNetDbAdapter? GetAdapterOrError(out string? error)
    {
        lock (Sync)
        {
            if (adapter is not null)
            {
                error = null;
                return adapter;
            }
        }

        error = "db adapter is not configured";
        return null;
    }
}
