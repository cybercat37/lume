namespace Axom.Compiler.Binding;

public sealed class BoundFunctionExpression : BoundExpression
{
    public FunctionSymbol Function { get; }

    public BoundFunctionExpression(FunctionSymbol function)
    {
        Function = function;
    }

    public override TypeSymbol Type => Function.Type;
}
