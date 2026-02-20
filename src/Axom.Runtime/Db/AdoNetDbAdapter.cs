using System.Data;
using System.Data.Common;

namespace Axom.Runtime.Db;

public sealed class AdoNetDbAdapter
{
    private readonly Func<DbConnection> connectionFactory;
    private readonly DbObservabilityRuntime observability;

    public AdoNetDbAdapter(
        Func<DbConnection> connectionFactory,
        DbObservabilityRuntime? observability = null)
    {
        this.connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        this.observability = observability ?? new DbObservabilityRuntime();
    }

    public int Exec(string sql, IReadOnlyDictionary<string, object?>? parameters = null)
    {
        return observability.ExecuteNonQuery(sql, parameters, ExecuteNonQueryCore);
    }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Query(string sql, IReadOnlyDictionary<string, object?>? parameters = null)
    {
        return observability.ExecuteQuery(sql, parameters, ExecuteQueryCore);
    }

    public T? Scalar<T>(string sql, IReadOnlyDictionary<string, object?>? parameters = null)
    {
        return observability.ExecuteScalar(sql, parameters, ExecuteScalarCore<T>);
    }

    public DbMetricsSnapshot GetMetricsSnapshot()
    {
        return observability.GetMetricsSnapshot();
    }

    private int ExecuteNonQueryCore(string sql, IReadOnlyDictionary<string, object?>? parameters)
    {
        using var connection = connectionFactory();
        connection.Open();
        using var command = CreateCommand(connection, sql, parameters);
        return command.ExecuteNonQuery();
    }

    private IReadOnlyList<IReadOnlyDictionary<string, object?>> ExecuteQueryCore(
        string sql,
        IReadOnlyDictionary<string, object?>? parameters)
    {
        using var connection = connectionFactory();
        connection.Open();
        using var command = CreateCommand(connection, sql, parameters);
        using var reader = command.ExecuteReader(CommandBehavior.SequentialAccess);

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.Ordinal);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[reader.GetName(i)] = value;
            }

            rows.Add(row);
        }

        return rows;
    }

    private T? ExecuteScalarCore<T>(string sql, IReadOnlyDictionary<string, object?>? parameters)
    {
        using var connection = connectionFactory();
        connection.Open();
        using var command = CreateCommand(connection, sql, parameters);

        var value = command.ExecuteScalar();
        if (value is null || value is DBNull)
        {
            return default;
        }

        if (value is T typed)
        {
            return typed;
        }

        return (T)Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static DbCommand CreateCommand(
        DbConnection connection,
        string sql,
        IReadOnlyDictionary<string, object?>? parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;

        if (parameters is null || parameters.Count == 0)
        {
            return command;
        }

        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = NormalizeParameterName(name);
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        return command;
    }

    private static string NormalizeParameterName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Parameter name cannot be null or empty.", nameof(name));
        }

        if (name[0] is '@' or ':' or '$')
        {
            return name;
        }

        return "@" + name;
    }
}
