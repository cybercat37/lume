using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredIndexExpression : LoweredExpression
{
    public LoweredExpression Target { get; }
    public LoweredExpression Index { get; }
    public override TypeSymbol Type { get; }

    public LoweredIndexExpression(LoweredExpression target, LoweredExpression index, TypeSymbol type)
    {
        Target = target;
        Index = index;
        Type = type;
    }
}
