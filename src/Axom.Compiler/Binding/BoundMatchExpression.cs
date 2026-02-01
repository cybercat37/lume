namespace Axom.Compiler.Binding;

public sealed class BoundMatchExpression : BoundExpression
{
    public BoundExpression Expression { get; }
    public IReadOnlyList<BoundMatchArm> Arms { get; }
    public override TypeSymbol Type { get; }

    public BoundMatchExpression(BoundExpression expression, IReadOnlyList<BoundMatchArm> arms, TypeSymbol type)
    {
        Expression = expression;
        Arms = arms;
        Type = type;
    }
}
