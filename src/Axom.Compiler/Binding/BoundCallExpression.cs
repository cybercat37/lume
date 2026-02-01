namespace Axom.Compiler.Binding;

public sealed class BoundCallExpression : BoundExpression
{
    public BoundExpression Callee { get; }
    public IReadOnlyList<BoundExpression> Arguments { get; }
    public override TypeSymbol Type { get; }

    public BoundCallExpression(BoundExpression callee, IReadOnlyList<BoundExpression> arguments, TypeSymbol returnType)
    {
        Callee = callee;
        Arguments = arguments;
        Type = returnType;
    }
}
