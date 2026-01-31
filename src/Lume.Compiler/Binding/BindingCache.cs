using Lume.Compiler.Parsing;

namespace Lume.Compiler.Binding;

public sealed class BindingCache
{
    private readonly Dictionary<int, BinderResult> cache = new();

    public bool TryGet(SyntaxTree syntaxTree, out BinderResult? result) =>
        cache.TryGetValue(syntaxTree.GetHashCode(), out result);

    public void Store(SyntaxTree syntaxTree, BinderResult result)
    {
        cache[syntaxTree.GetHashCode()] = result;
    }
}
