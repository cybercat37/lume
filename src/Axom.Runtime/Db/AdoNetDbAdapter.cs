using System.Data;
using System.Data.Common;

namespace Axom.Runtime.Db;

public sealed class AdoNetDbAdapter
{
    private readonly Func<DbConnection> connectionFactory;
    private readonly DbObservabilityRuntime observability;
    private readonly ISqlRecordProjectionResolver? recordProjectionResolver;
    private readonly object transactionSync = new();
    private DbConnection? transactionConnection;
    private DbTransaction? transaction;

    public AdoNetDbAdapter(
        Func<DbConnection> connectionFactory,
        DbObservabilityRuntime? observability = null,
        ISqlRecordProjectionResolver? recordProjectionResolver = null)
    {
        this.connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        this.observability = observability ?? new DbObservabilityRuntime();
        this.recordProjectionResolver = recordProjectionResolver;
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

    public bool TryBeginTransaction(out string? error)
    {
        lock (transactionSync)
        {
            if (transaction is not null)
            {
                error = "transaction is already active";
                return false;
            }

            try
            {
                var connection = connectionFactory();
                connection.Open();
                var started = connection.BeginTransaction();
                transactionConnection = connection;
                transaction = started;
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                DisposeTransactionState();
                error = ex.Message;
                return false;
            }
        }
    }

    public bool TryCommitTransaction(out string? error)
    {
        lock (transactionSync)
        {
            if (transaction is null)
            {
                error = "transaction is not active";
                return false;
            }

            try
            {
                transaction.Commit();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            finally
            {
                DisposeTransactionState();
            }
        }
    }

    public bool TryRollbackTransaction(out string? error)
    {
        lock (transactionSync)
        {
            if (transaction is null)
            {
                error = "transaction is not active";
                return false;
            }

            try
            {
                transaction.Rollback();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            finally
            {
                DisposeTransactionState();
            }
        }
    }

    public DbMetricsSnapshot GetMetricsSnapshot()
    {
        return observability.GetMetricsSnapshot();
    }

    private int ExecuteNonQueryCore(string sql, IReadOnlyDictionary<string, object?>? parameters)
    {
        var (boundSql, boundParameters) = BindSqlTemplate(sql, parameters);
        return ExecuteWithCommand(boundSql, boundParameters, static command => command.ExecuteNonQuery());
    }

    private IReadOnlyList<IReadOnlyDictionary<string, object?>> ExecuteQueryCore(
        string sql,
        IReadOnlyDictionary<string, object?>? parameters)
    {
        var (boundSql, boundParameters) = BindSqlTemplate(sql, parameters);
        return ExecuteWithCommand(boundSql, boundParameters, static command =>
        {
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

            return (IReadOnlyList<IReadOnlyDictionary<string, object?>>)rows;
        });
    }

    private T? ExecuteScalarCore<T>(string sql, IReadOnlyDictionary<string, object?>? parameters)
    {
        var (boundSql, boundParameters) = BindSqlTemplate(sql, parameters);
        return ExecuteWithCommand(boundSql, boundParameters, static command =>
        {
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
        });
    }

    private (string sql, IReadOnlyDictionary<string, object?> parameters) BindSqlTemplate(
        string sql,
        IReadOnlyDictionary<string, object?>? parameters)
    {
        if (SqlTemplateBinder.TryBind(sql, parameters, recordProjectionResolver, out var boundSql, out var boundParameters, out var error))
        {
            return (boundSql, boundParameters);
        }

        throw new ArgumentException(error ?? "Invalid SQL template.", nameof(sql));
    }

    private static DbCommand CreateCommand(
        DbConnection connection,
        DbTransaction? transaction,
        string sql,
        IReadOnlyDictionary<string, object?>? parameters)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
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

    private TResult ExecuteWithCommand<TResult>(
        string sql,
        IReadOnlyDictionary<string, object?>? parameters,
        Func<DbCommand, TResult> execute)
    {
        lock (transactionSync)
        {
            if (transactionConnection is not null && transaction is not null)
            {
                using var command = CreateCommand(transactionConnection, transaction, sql, parameters);
                return execute(command);
            }
        }

        using var connection = connectionFactory();
        connection.Open();
        using var nonTransactionCommand = CreateCommand(connection, transaction: null, sql, parameters);
        return execute(nonTransactionCommand);
    }

    private void DisposeTransactionState()
    {
        transaction?.Dispose();
        transaction = null;

        transactionConnection?.Dispose();
        transactionConnection = null;
    }
}
