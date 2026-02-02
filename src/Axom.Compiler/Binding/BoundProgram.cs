namespace Axom.Compiler.Binding;

public sealed class BoundProgram
{
    public IReadOnlyList<BoundRecordTypeDeclaration> RecordTypes { get; }
    public IReadOnlyList<BoundSumTypeDeclaration> SumTypes { get; }
    public IReadOnlyList<BoundFunctionDeclaration> Functions { get; }
    public IReadOnlyList<BoundStatement> Statements { get; }

    public BoundProgram(
        IReadOnlyList<BoundRecordTypeDeclaration> recordTypes,
        IReadOnlyList<BoundSumTypeDeclaration> sumTypes,
        IReadOnlyList<BoundFunctionDeclaration> functions,
        IReadOnlyList<BoundStatement> statements)
    {
        RecordTypes = recordTypes;
        SumTypes = sumTypes;
        Functions = functions;
        Statements = statements;
    }
}
