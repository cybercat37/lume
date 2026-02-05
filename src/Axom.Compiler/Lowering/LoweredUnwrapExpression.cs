using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredUnwrapExpression : LoweredExpression
{
    public LoweredExpression Target { get; }
    public SumVariantSymbol FailureVariant { get; }
    public override TypeSymbol Type { get; }

    public LoweredUnwrapExpression(LoweredExpression target, SumVariantSymbol failureVariant, TypeSymbol type)
    {
        Target = target;
        FailureVariant = failureVariant;
        Type = type;
    }
}
