using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredIsSumExpression : LoweredExpression
{
    public LoweredExpression Target { get; }
    public TypeSymbol SumType { get; }

    public LoweredIsSumExpression(LoweredExpression target, TypeSymbol sumType)
    {
        Target = target;
        SumType = sumType;
    }

    public override TypeSymbol Type => TypeSymbol.Bool;
}
