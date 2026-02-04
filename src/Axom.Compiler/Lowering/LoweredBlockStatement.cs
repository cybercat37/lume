namespace Axom.Compiler.Lowering;

public sealed class LoweredBlockStatement : LoweredStatement
{
    public IReadOnlyList<LoweredStatement> Statements { get; }

    public LoweredBlockStatement(IReadOnlyList<LoweredStatement> statements)
    {
        Statements = statements;
    }
}
