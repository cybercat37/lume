using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredFunctionExpression : LoweredExpression
{
    public FunctionSymbol Function { get; }

    public LoweredFunctionExpression(FunctionSymbol function)
    {
        Function = function;
    }

    public override TypeSymbol Type => Function.Type;
}
