namespace Axom.Compiler.Binding;

public sealed class BoundIdentifierPattern : BoundPattern
{
    public VariableSymbol Symbol { get; }
    public override TypeSymbol Type { get; }

    public BoundIdentifierPattern(VariableSymbol symbol, TypeSymbol type)
    {
        Symbol = symbol;
        Type = type;
    }
}
