namespace Axom.Compiler.Binding;

public sealed class BoundProgram
{
    public IReadOnlyList<BoundFunctionDeclaration> Functions { get; }
    public IReadOnlyList<BoundStatement> Statements { get; }

    public BoundProgram(IReadOnlyList<BoundFunctionDeclaration> functions, IReadOnlyList<BoundStatement> statements)
    {
        Functions = functions;
        Statements = statements;
    }
}
