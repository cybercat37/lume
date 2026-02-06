using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredSpawnExpression : LoweredExpression
{
    public LoweredBlockExpression Body { get; }
    public override TypeSymbol Type { get; }

    public LoweredSpawnExpression(LoweredBlockExpression body, TypeSymbol type)
    {
        Body = body;
        Type = type;
    }
}
