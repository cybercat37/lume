namespace Axom.Compiler.Binding;

public sealed class BoundAssignmentExpression : BoundExpression
{
    public VariableSymbol Symbol { get; }
    public BoundExpression Expression { get; }
    public override TypeSymbol Type => Symbol.Type;

    public BoundAssignmentExpression(VariableSymbol symbol, BoundExpression expression)
    {
        Symbol = symbol;
        Expression = expression;
    }
}
