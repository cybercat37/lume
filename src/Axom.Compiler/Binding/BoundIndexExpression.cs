namespace Axom.Compiler.Binding;

public sealed class BoundIndexExpression : BoundExpression
{
    public BoundExpression Target { get; }
    public BoundExpression Index { get; }
    public override TypeSymbol Type { get; }

    public BoundIndexExpression(BoundExpression target, BoundExpression index, TypeSymbol type)
    {
        Target = target;
        Index = index;
        Type = type;
    }
}
