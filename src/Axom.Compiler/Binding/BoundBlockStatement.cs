namespace Axom.Compiler.Binding;

public sealed class BoundBlockStatement : BoundStatement
{
    public IReadOnlyList<BoundStatement> Statements { get; }
    public bool IsScopeBlock { get; }

    public BoundBlockStatement(IReadOnlyList<BoundStatement> statements, bool isScopeBlock = false)
    {
        Statements = statements;
        IsScopeBlock = isScopeBlock;
    }
}
