using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public abstract class LoweredExpression : LoweredNode
{
    public abstract TypeSymbol Type { get; }
}
