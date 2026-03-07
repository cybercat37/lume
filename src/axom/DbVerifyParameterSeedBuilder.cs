using System.Text.RegularExpressions;

namespace Axom.Cli;

internal static class DbVerifyParameterSeedBuilder
{
    private static readonly Regex IdentifierRegex = new(
        "^[A-Za-z_][A-Za-z0-9_]*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static IReadOnlyDictionary<string, object?> Build(string sql)
    {
        var seed = new Dictionary<string, object?>(StringComparer.Ordinal);
        var inSingleQuotedString = false;

        for (var i = 0; i < sql.Length; i++)
        {
            var current = sql[i];
            if (inSingleQuotedString)
            {
                if (current == '\'' && i + 1 < sql.Length && sql[i + 1] == '\'')
                {
                    i++;
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
                continue;
            }

            if (current != '{')
            {
                continue;
            }

            var end = sql.IndexOf('}', i + 1);
            if (end <= i + 1)
            {
                continue;
            }

            var placeholder = sql.Substring(i + 1, end - i - 1).Trim();
            if (placeholder.Length == 0)
            {
                i = end;
                continue;
            }

            if (!IdentifierRegex.IsMatch(placeholder))
            {
                i = end;
                continue;
            }

            if (char.IsUpper(placeholder[0]))
            {
                i = end;
                continue;
            }

            if (!seed.ContainsKey(placeholder))
            {
                seed[placeholder] = 0;
            }

            i = end;
        }

        return seed;
    }
}
