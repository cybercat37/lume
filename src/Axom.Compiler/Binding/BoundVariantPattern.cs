namespace Axom.Compiler.Binding;

public sealed class BoundVariantPattern : BoundPattern
{
    public SumVariantSymbol Variant { get; }
    public BoundPattern? Payload { get; }

    public BoundVariantPattern(SumVariantSymbol variant, BoundPattern? payload, TypeSymbol type)
    {
        Variant = variant;
        Payload = payload;
        Type = type;
    }

    public override TypeSymbol Type { get; }
}
