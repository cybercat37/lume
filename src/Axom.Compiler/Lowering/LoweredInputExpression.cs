using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredInputExpression : LoweredExpression
{
    public override TypeSymbol Type => TypeSymbol.String;
}
