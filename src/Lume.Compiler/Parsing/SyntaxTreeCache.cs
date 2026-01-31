namespace Lume.Compiler.Parsing;

public sealed class SyntaxTreeCache
{
    private readonly Dictionary<string, SyntaxTree> cache = new(StringComparer.Ordinal);

    public bool TryGet(string key, out SyntaxTree? tree) =>
        cache.TryGetValue(key, out tree);

    public void Store(string key, SyntaxTree tree)
    {
        cache[key] = tree;
    }
}
