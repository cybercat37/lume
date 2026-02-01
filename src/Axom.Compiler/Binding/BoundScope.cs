namespace Axom.Compiler.Binding;

public sealed class BoundScope
{
    private readonly Dictionary<string, VariableSymbol> symbols = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FunctionSymbol> functions = new(StringComparer.Ordinal);

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

    public FunctionSymbol? TryDeclareFunction(FunctionSymbol symbol)
    {
        if (functions.ContainsKey(symbol.Name))
        {
            return null;
        }

        functions[symbol.Name] = symbol;
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

    public FunctionSymbol? TryLookupFunction(string name)
    {
        if (functions.TryGetValue(name, out var symbol))
        {
            return symbol;
        }

        return Parent?.TryLookupFunction(name);
    }

    public bool ContainsSymbol(VariableSymbol symbol) =>
        symbols.TryGetValue(symbol.Name, out var local) && ReferenceEquals(local, symbol);
}
