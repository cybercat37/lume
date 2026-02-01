namespace Axom.Compiler.Binding;

public sealed class BoundPrintStatement : BoundStatement
{
    public BoundExpression Expression { get; }

    public BoundPrintStatement(BoundExpression expression)
    {
        Expression = expression;
    }
}
