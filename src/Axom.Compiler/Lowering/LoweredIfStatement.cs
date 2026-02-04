namespace Axom.Compiler.Lowering;

public sealed class LoweredIfStatement : LoweredStatement
{
    public LoweredExpression Condition { get; }
    public LoweredStatement Then { get; }
    public LoweredStatement? Else { get; }

    public LoweredIfStatement(LoweredExpression condition, LoweredStatement thenStatement, LoweredStatement? elseStatement)
    {
        Condition = condition;
        Then = thenStatement;
        Else = elseStatement;
    }
}
