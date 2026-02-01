namespace Axom.Compiler.Binding;

public sealed class BoundTuplePattern : BoundPattern
{
    public IReadOnlyList<BoundPattern> Elements { get; }
    public override TypeSymbol Type { get; }

    public BoundTuplePattern(IReadOnlyList<BoundPattern> elements, TypeSymbol type)
    {
        Elements = elements;
        Type = type;
    }
}
