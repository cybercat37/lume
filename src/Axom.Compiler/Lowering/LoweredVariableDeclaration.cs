using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredVariableDeclaration : LoweredStatement
{
    public VariableSymbol Symbol { get; }
    public LoweredExpression Initializer { get; }

    public LoweredVariableDeclaration(VariableSymbol symbol, LoweredExpression initializer)
    {
        Symbol = symbol;
        Initializer = initializer;
    }
}
