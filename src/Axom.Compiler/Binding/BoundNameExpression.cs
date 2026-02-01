namespace Axom.Compiler.Binding;

public sealed class BoundNameExpression : BoundExpression
{
    public VariableSymbol Symbol { get; }
    public override TypeSymbol Type => Symbol.Type;

    public BoundNameExpression(VariableSymbol symbol)
    {
        Symbol = symbol;
    }
}
