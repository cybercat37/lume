using System.Text.RegularExpressions;

namespace Axom.Runtime.Db;

public interface ISqlRecordProjectionResolver
{
    bool TryResolve(string recordName, out IReadOnlyList<string> columns);
}

public sealed partial class DictionarySqlRecordProjectionResolver : ISqlRecordProjectionResolver
{
    private static readonly Regex IdentifierRegex = BuildIdentifierRegex();
    private readonly Dictionary<string, IReadOnlyList<string>> projections;

    public DictionarySqlRecordProjectionResolver(IReadOnlyDictionary<string, IReadOnlyList<string>> projections)
    {
        ArgumentNullException.ThrowIfNull(projections);

        this.projections = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var (recordName, columns) in projections)
        {
            if (string.IsNullOrWhiteSpace(recordName))
            {
                throw new ArgumentException("Record name cannot be empty.", nameof(projections));
            }

            if (!IdentifierRegex.IsMatch(recordName))
            {
                throw new ArgumentException($"Invalid record name '{recordName}'.", nameof(projections));
            }

            if (columns is null || columns.Count == 0)
            {
                throw new ArgumentException($"Record '{recordName}' must declare at least one column.", nameof(projections));
            }

            foreach (var column in columns)
            {
                if (string.IsNullOrWhiteSpace(column) || !IdentifierRegex.IsMatch(column))
                {
                    throw new ArgumentException($"Invalid column '{column}' for record '{recordName}'.", nameof(projections));
                }
            }

            this.projections[recordName] = columns;
        }
    }

    public bool TryResolve(string recordName, out IReadOnlyList<string> columns)
    {
        return projections.TryGetValue(recordName, out columns!);
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex BuildIdentifierRegex();
}
