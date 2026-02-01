namespace Axom.Compiler.Binding;

public sealed class BoundWildcardPattern : BoundPattern
{
    public override TypeSymbol Type { get; }

    public BoundWildcardPattern(TypeSymbol type)
    {
        Type = type;
    }
}
