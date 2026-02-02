namespace Axom.Compiler.Binding;

public sealed class BoundSumConstructorExpression : BoundExpression
{
    public SumVariantSymbol Variant { get; }
    public BoundExpression? Payload { get; }

    public BoundSumConstructorExpression(SumVariantSymbol variant, BoundExpression? payload)
    {
        Variant = variant;
        Payload = payload;
    }

    public override TypeSymbol Type => Variant.DeclaringType;
}
