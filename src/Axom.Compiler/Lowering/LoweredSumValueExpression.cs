using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredSumValueExpression : LoweredExpression
{
    public LoweredExpression Target { get; }
    public override TypeSymbol Type { get; }

    public LoweredSumValueExpression(LoweredExpression target, TypeSymbol type)
    {
        Target = target;
        Type = type;
    }
}
