namespace Axom.Compiler.Binding;

public sealed class BoundListExpression : BoundExpression
{
    public IReadOnlyList<BoundExpression> Elements { get; }
    public override TypeSymbol Type { get; }

    public BoundListExpression(IReadOnlyList<BoundExpression> elements, TypeSymbol type)
    {
        Elements = elements;
        Type = type;
    }
}
