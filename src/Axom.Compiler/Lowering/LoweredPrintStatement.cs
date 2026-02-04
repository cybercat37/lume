namespace Axom.Compiler.Lowering;

public sealed class LoweredPrintStatement : LoweredStatement
{
    public LoweredExpression Expression { get; }

    public LoweredPrintStatement(LoweredExpression expression)
    {
        Expression = expression;
    }
}
