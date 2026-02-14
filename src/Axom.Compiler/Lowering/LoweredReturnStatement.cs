namespace Axom.Compiler.Lowering;

public sealed class LoweredReturnStatement : LoweredStatement
{
    public LoweredExpression? Expression { get; }
    public IReadOnlyList<LoweredStatement> DeferredStatements { get; }

    public LoweredReturnStatement(LoweredExpression? expression, IReadOnlyList<LoweredStatement>? deferredStatements = null)
    {
        Expression = expression;
        DeferredStatements = deferredStatements ?? Array.Empty<LoweredStatement>();
    }
}
