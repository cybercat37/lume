using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredSumConstructorExpression : LoweredExpression
{
    public SumVariantSymbol Variant { get; }
    public LoweredExpression? Payload { get; }

    public LoweredSumConstructorExpression(SumVariantSymbol variant, LoweredExpression? payload)
    {
        Variant = variant;
        Payload = payload;
    }

    public override TypeSymbol Type => Variant.DeclaringType;
}
