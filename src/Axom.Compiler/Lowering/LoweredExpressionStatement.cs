namespace Axom.Compiler.Lowering;

public sealed class LoweredExpressionStatement : LoweredStatement
{
    public LoweredExpression Expression { get; }

    public LoweredExpressionStatement(LoweredExpression expression)
    {
        Expression = expression;
    }
}
