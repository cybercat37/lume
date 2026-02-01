namespace Axom.Compiler.Binding;

public sealed class BoundTupleExpression : BoundExpression
{
    public IReadOnlyList<BoundExpression> Elements { get; }
    public override TypeSymbol Type { get; }

    public BoundTupleExpression(IReadOnlyList<BoundExpression> elements, TypeSymbol type)
    {
        Elements = elements;
        Type = type;
    }
}
