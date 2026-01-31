namespace Lume.Compiler.Binding;

public sealed class BoundScope
{
    private readonly Dictionary<string, VariableSymbol> symbols = new(StringComparer.Ordinal);

    public BoundScope? Parent { get; }

    public BoundScope(BoundScope? parent)
    {
        Parent = parent;
    }

    public VariableSymbol? TryDeclare(VariableSymbol symbol)
    {
        if (symbols.ContainsKey(symbol.Name))
        {
            return null;
        }

        symbols[symbol.Name] = symbol;
        return symbol;
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
