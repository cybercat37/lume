namespace Lume.Compiler.Binding;

public sealed class BoundScope
{
    private readonly Dictionary<string, VariableSymbol> symbols = new(StringComparer.Ordinal);

    public BoundScope? Parent { get; }

    public BoundScope(BoundScope? parent)
    {
        Parent = parent;
    }

    public bool TryDeclare(VariableSymbol symbol)
    {
        if (symbols.ContainsKey(symbol.Name))
        {
            return false;
        }

        symbols[symbol.Name] = symbol;
        return true;
    }

    public VariableSymbol? TryLookup(string name)
    {
        if (symbols.TryGetValue(name, out var symbol))
        {
            return symbol;
        }

        return Parent?.TryLookup(name);
    }
}
