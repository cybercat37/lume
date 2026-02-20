using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Axom.Runtime.Db;

public static partial class DbQueryFingerprint
{
    private static readonly Regex WhitespaceRegex = BuildWhitespaceRegex();
    private static readonly Regex ParameterRegex = BuildParameterRegex();
    private static readonly Regex NumberRegex = BuildNumberRegex();

    public static string CreateQueryId(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return ComputeHash("<empty>");
        }

        var normalized = Normalize(sql);
        return ComputeHash(normalized);
    }

    public static string Normalize(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return string.Empty;
        }

        var withoutStrings = ReplaceQuotedStringLiterals(sql);
        var withStandardParameters = ParameterRegex.Replace(withoutStrings, "?");
        var withoutNumbers = NumberRegex.Replace(withStandardParameters, "?");
        var collapsedWhitespace = WhitespaceRegex.Replace(withoutNumbers, " ").Trim();
        return collapsedWhitespace.ToLowerInvariant();
    }

    private static string ReplaceQuotedStringLiterals(string text)
    {
        var builder = new StringBuilder(text.Length);
        var inSingleQuotedString = false;

        for (var i = 0; i < text.Length; i++)
        {
            var current = text[i];
            if (inSingleQuotedString)
            {
                if (current == '\'' && i + 1 < text.Length && text[i + 1] == '\'')
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
                builder.Append('?');
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }

    private static string ComputeHash(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [GeneratedRegex("\\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex BuildWhitespaceRegex();

    [GeneratedRegex("(?<![A-Za-z0-9_])(?:@[A-Za-z_][A-Za-z0-9_]*|:[A-Za-z_][A-Za-z0-9_]*|\\$[0-9]+|\\?)(?![A-Za-z0-9_])", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex BuildParameterRegex();

    [GeneratedRegex("(?<![A-Za-z0-9_])[-+]?(?:\\d+\\.\\d+|\\d+)(?![A-Za-z0-9_])", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex BuildNumberRegex();
}
