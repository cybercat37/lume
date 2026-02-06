namespace Axom.Compiler.Lowering;

public sealed class LoweredBlockStatement : LoweredStatement
{
    public IReadOnlyList<LoweredStatement> Statements { get; }
    public bool IsScopeBlock { get; }

    public LoweredBlockStatement(IReadOnlyList<LoweredStatement> statements, bool isScopeBlock = false)
    {
        Statements = statements;
        IsScopeBlock = isScopeBlock;
    }
}
