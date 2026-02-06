namespace Axom.Compiler.Binding;

public sealed class BoundParExpression : BoundExpression
{
    public BoundExpression Expression { get; }
    public override TypeSymbol Type { get; }

    public BoundParExpression(BoundExpression expression, TypeSymbol type)
    {
        Expression = expression;
        Type = type;
    }
}
