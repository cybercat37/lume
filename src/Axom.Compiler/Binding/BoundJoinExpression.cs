namespace Axom.Compiler.Binding;

public sealed class BoundJoinExpression : BoundExpression
{
    public BoundExpression Expression { get; }
    public override TypeSymbol Type { get; }

    public BoundJoinExpression(BoundExpression expression, TypeSymbol type)
    {
        Expression = expression;
        Type = type;
    }
}
