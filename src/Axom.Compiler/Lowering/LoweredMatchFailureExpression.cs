using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredMatchFailureExpression : LoweredExpression
{
    public override TypeSymbol Type { get; }

    public LoweredMatchFailureExpression(TypeSymbol type)
    {
        Type = type;
    }
}
