namespace Axom.Compiler.Binding;

public sealed class BoundProgram
{
    public IReadOnlyList<BoundRecordTypeDeclaration> RecordTypes { get; }
    public IReadOnlyList<BoundFunctionDeclaration> Functions { get; }
    public IReadOnlyList<BoundStatement> Statements { get; }

    public BoundProgram(
        IReadOnlyList<BoundRecordTypeDeclaration> recordTypes,
        IReadOnlyList<BoundFunctionDeclaration> functions,
        IReadOnlyList<BoundStatement> statements)
    {
        RecordTypes = recordTypes;
        Functions = functions;
        Statements = statements;
    }
}
