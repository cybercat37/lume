using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredAssignmentExpression : LoweredExpression
{
    public VariableSymbol Symbol { get; }
    public LoweredExpression Expression { get; }

    public LoweredAssignmentExpression(VariableSymbol symbol, LoweredExpression expression)
    {
        Symbol = symbol;
        Expression = expression;
    }

    public override TypeSymbol Type => Symbol.Type;
}
