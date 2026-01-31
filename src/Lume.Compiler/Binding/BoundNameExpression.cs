namespace Lume.Compiler.Binding;

public sealed class BoundNameExpression : BoundExpression
{
    public VariableSymbol Symbol { get; }

    public BoundNameExpression(VariableSymbol symbol)
    {
        Symbol = symbol;
    }
}
