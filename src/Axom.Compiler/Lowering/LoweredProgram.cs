using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredProgram
{
    public BoundProgram Source { get; }
    public IReadOnlyList<BoundRecordTypeDeclaration> RecordTypes { get; }
    public IReadOnlyList<BoundSumTypeDeclaration> SumTypes { get; }
    public IReadOnlyList<LoweredFunctionDeclaration> Functions { get; }
    public IReadOnlyList<LoweredStatement> Statements { get; }

    public LoweredProgram(
        BoundProgram source,
        IReadOnlyList<BoundRecordTypeDeclaration> recordTypes,
        IReadOnlyList<BoundSumTypeDeclaration> sumTypes,
        IReadOnlyList<LoweredFunctionDeclaration> functions,
        IReadOnlyList<LoweredStatement> statements)
    {
        Source = source;
        RecordTypes = recordTypes;
        SumTypes = sumTypes;
        Functions = functions;
        Statements = statements;
    }

    public override int GetHashCode() => Source.GetHashCode();
}
