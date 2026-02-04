using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredRecordLiteralExpression : LoweredExpression
{
    public TypeSymbol RecordType { get; }
    public IReadOnlyList<LoweredRecordFieldAssignment> Fields { get; }

    public LoweredRecordLiteralExpression(
        TypeSymbol recordType,
        IReadOnlyList<LoweredRecordFieldAssignment> fields)
    {
        RecordType = recordType;
        Fields = fields;
    }

    public override TypeSymbol Type => RecordType;
}
