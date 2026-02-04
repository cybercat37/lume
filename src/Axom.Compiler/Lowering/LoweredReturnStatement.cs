namespace Axom.Compiler.Lowering;

public sealed class LoweredReturnStatement : LoweredStatement
{
    public LoweredExpression? Expression { get; }

    public LoweredReturnStatement(LoweredExpression? expression)
    {
        Expression = expression;
    }
}
