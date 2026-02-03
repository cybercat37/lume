using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredProgram
{
    public BoundProgram Source { get; }
    public IReadOnlyList<BoundRecordTypeDeclaration> RecordTypes { get; }
    public IReadOnlyList<BoundSumTypeDeclaration> SumTypes { get; }
    public IReadOnlyList<BoundFunctionDeclaration> Functions { get; }
    public IReadOnlyList<BoundStatement> Statements { get; }

    public LoweredProgram(
        BoundProgram source,
        IReadOnlyList<BoundRecordTypeDeclaration> recordTypes,
        IReadOnlyList<BoundSumTypeDeclaration> sumTypes,
        IReadOnlyList<BoundFunctionDeclaration> functions,
        IReadOnlyList<BoundStatement> statements)
    {
        Source = source;
        RecordTypes = recordTypes;
        SumTypes = sumTypes;
        Functions = functions;
        Statements = statements;
    }

    public override int GetHashCode() => Source.GetHashCode();
}
