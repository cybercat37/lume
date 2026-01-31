namespace Lume.Compiler.Binding;

public sealed class BoundCallExpression : BoundExpression
{
    public FunctionSymbol Function { get; }
    public IReadOnlyList<BoundExpression> Arguments { get; }
    public override TypeSymbol Type => Function.ReturnType;

    public BoundCallExpression(FunctionSymbol function, IReadOnlyList<BoundExpression> arguments)
    {
        Function = function;
        Arguments = arguments;
    }
}
