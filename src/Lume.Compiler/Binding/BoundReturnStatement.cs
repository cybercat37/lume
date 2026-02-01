namespace Lume.Compiler.Binding;

public sealed class BoundReturnStatement : BoundStatement
{
    public BoundExpression? Expression { get; }

    public BoundReturnStatement(BoundExpression? expression)
    {
        Expression = expression;
    }
}
