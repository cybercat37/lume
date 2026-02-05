namespace Axom.Compiler.Binding;

public sealed class BoundMapExpression : BoundExpression
{
    public IReadOnlyList<BoundMapEntry> Entries { get; }
    public override TypeSymbol Type { get; }

    public BoundMapExpression(IReadOnlyList<BoundMapEntry> entries, TypeSymbol type)
    {
        Entries = entries;
        Type = type;
    }
}
