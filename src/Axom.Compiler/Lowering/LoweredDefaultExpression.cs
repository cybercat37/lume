using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredDefaultExpression : LoweredExpression
{
    public override TypeSymbol Type { get; }

    public LoweredDefaultExpression(TypeSymbol type)
    {
        Type = type;
    }
}
