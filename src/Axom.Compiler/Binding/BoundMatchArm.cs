namespace Axom.Compiler.Binding;

public sealed class BoundMatchArm
{
    public BoundPattern Pattern { get; }
    public BoundExpression? Guard { get; }
    public BoundExpression Expression { get; }

    public BoundMatchArm(BoundPattern pattern, BoundExpression? guard, BoundExpression expression)
    {
        Pattern = pattern;
        Guard = guard;
        Expression = expression;
    }
}
