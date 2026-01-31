namespace Lume.Compiler.Binding;

public sealed class BoundBlockStatement : BoundStatement
{
    public IReadOnlyList<BoundStatement> Statements { get; }

    public BoundBlockStatement(IReadOnlyList<BoundStatement> statements)
    {
        Statements = statements;
    }
}
