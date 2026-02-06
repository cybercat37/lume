using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredJoinExpression : LoweredExpression
{
    public LoweredExpression Expression { get; }
    public override TypeSymbol Type { get; }

    public LoweredJoinExpression(LoweredExpression expression, TypeSymbol type)
    {
        Expression = expression;
        Type = type;
    }
}
