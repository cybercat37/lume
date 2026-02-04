using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredSumTagExpression : LoweredExpression
{
    public LoweredExpression Target { get; }

    public LoweredSumTagExpression(LoweredExpression target)
    {
        Target = target;
    }

    public override TypeSymbol Type => TypeSymbol.String;
}
