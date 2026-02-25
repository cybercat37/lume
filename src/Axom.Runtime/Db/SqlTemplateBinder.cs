using System.Text;
using System.Text.RegularExpressions;

namespace Axom.Runtime.Db;

public static partial class SqlTemplateBinder
{
    private static readonly Regex IdentifierRegex = BuildIdentifierRegex();

    public static bool TryBind(
        string sql,
        IReadOnlyDictionary<string, object?>? parameters,
        ISqlRecordProjectionResolver? recordProjectionResolver,
        out string boundSql,
        out IReadOnlyDictionary<string, object?> boundParameters,
        out string? error)
    {
        boundSql = sql;
        boundParameters = parameters ?? new Dictionary<string, object?>(StringComparer.Ordinal);
        error = null;

        if (string.IsNullOrWhiteSpace(sql))
        {
            return true;
        }

        var usedParameters = new Dictionary<string, object?>(StringComparer.Ordinal);
        var builder = new StringBuilder(sql.Length);
        var inSingleQuotedString = false;
        var placeholderCount = 0;

        for (var i = 0; i < sql.Length; i++)
        {
            var current = sql[i];

            if (inSingleQuotedString)
            {
                builder.Append(current);
                if (current == '\'' && i + 1 < sql.Length && sql[i + 1] == '\'')
                {
                    builder.Append(sql[++i]);
                    continue;
                }

                if (current == '\'')
                {
                    inSingleQuotedString = false;
                }

                continue;
            }

            if (current == '\'')
            {
                inSingleQuotedString = true;
                builder.Append(current);
                continue;
            }

            if (current != '{')
            {
                builder.Append(current);
                continue;
            }

            var end = sql.IndexOf('}', i + 1);
            if (end <= i + 1)
            {
                error = "Unterminated SQL placeholder. Expected '}' after '{'.";
                return false;
            }

            var placeholder = sql.Substring(i + 1, end - i - 1).Trim();
            if (!IdentifierRegex.IsMatch(placeholder))
            {
                error = $"Invalid SQL placeholder '{{{placeholder}}}'. Use identifier placeholders like {{userId}}.";
                return false;
            }

            if (char.IsUpper(placeholder[0]))
            {
                if (recordProjectionResolver is null)
                {
                    error = $"Record mapping placeholder '{{{placeholder}}}' requires a record projection resolver.";
                    return false;
                }

                if (!recordProjectionResolver.TryResolve(placeholder, out var columns) || columns.Count == 0)
                {
                    error = $"Unknown SQL record placeholder '{{{placeholder}}}'.";
                    return false;
                }

                builder.Append(string.Join(", ", columns));
                i = end;
                continue;
            }

            if (!TryResolveParameter(parameters, placeholder, out var value))
            {
                error = $"Missing SQL parameter '{placeholder}'.";
                return false;
            }

            placeholderCount++;
            usedParameters[placeholder] = value;
            builder.Append('@').Append(placeholder);
            i = end;
        }

        boundSql = builder.ToString();
        boundParameters = placeholderCount == 0
            ? (parameters ?? new Dictionary<string, object?>(StringComparer.Ordinal))
            : usedParameters;
        return true;
    }

    public static bool TryBind(
        string sql,
        IReadOnlyDictionary<string, object?>? parameters,
        out string boundSql,
        out IReadOnlyDictionary<string, object?> boundParameters,
        out string? error)
    {
        return TryBind(sql, parameters, recordProjectionResolver: null, out boundSql, out boundParameters, out error);
    }

    private static bool TryResolveParameter(IReadOnlyDictionary<string, object?>? parameters, string name, out object? value)
    {
        value = null;
        if (parameters is null || parameters.Count == 0)
        {
            return false;
        }

        if (parameters.TryGetValue(name, out value)
            || parameters.TryGetValue("@" + name, out value)
            || parameters.TryGetValue(":" + name, out value)
            || parameters.TryGetValue("$" + name, out value))
        {
            return true;
        }

        return false;
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex BuildIdentifierRegex();
}
