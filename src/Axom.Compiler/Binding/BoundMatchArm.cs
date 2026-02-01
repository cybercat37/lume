namespace Axom.Compiler.Binding;

public sealed class BoundMatchArm
{
    public BoundPattern Pattern { get; }
    public BoundExpression Expression { get; }

    public BoundMatchArm(BoundPattern pattern, BoundExpression expression)
    {
        Pattern = pattern;
        Expression = expression;
    }
}
