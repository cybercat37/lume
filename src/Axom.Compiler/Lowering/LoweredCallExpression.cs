using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredCallExpression : LoweredExpression
{
    public LoweredExpression Callee { get; }
    public IReadOnlyList<LoweredExpression> Arguments { get; }
    public override TypeSymbol Type { get; }

    public LoweredCallExpression(LoweredExpression callee, IReadOnlyList<LoweredExpression> arguments, TypeSymbol type)
    {
        Callee = callee;
        Arguments = arguments;
        Type = type;
    }
}
