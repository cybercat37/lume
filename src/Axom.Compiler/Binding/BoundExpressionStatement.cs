namespace Axom.Compiler.Binding;

public sealed class BoundExpressionStatement : BoundStatement
{
    public BoundExpression Expression { get; }
    public TypeSymbol Type => Expression.Type;

    public BoundExpressionStatement(BoundExpression expression)
    {
        Expression = expression;
    }
}
