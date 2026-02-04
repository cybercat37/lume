using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredTupleAccessExpression : LoweredExpression
{
    public LoweredExpression Target { get; }
    public int Index { get; }
    public override TypeSymbol Type { get; }

    public LoweredTupleAccessExpression(LoweredExpression target, int index, TypeSymbol type)
    {
        Target = target;
        Index = index;
        Type = type;
    }
}
