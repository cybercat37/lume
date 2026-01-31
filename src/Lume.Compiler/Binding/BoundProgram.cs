namespace Lume.Compiler.Binding;

public sealed class BoundProgram
{
    public IReadOnlyList<BoundStatement> Statements { get; }

    public BoundProgram(IReadOnlyList<BoundStatement> statements)
    {
        Statements = statements;
    }
}
