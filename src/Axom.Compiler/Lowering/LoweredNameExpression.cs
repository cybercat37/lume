using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredNameExpression : LoweredExpression
{
    public VariableSymbol Symbol { get; }

    public LoweredNameExpression(VariableSymbol symbol)
    {
        Symbol = symbol;
    }

    public override TypeSymbol Type => Symbol.Type;
}
