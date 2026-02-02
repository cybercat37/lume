namespace Axom.Compiler.Binding;

public sealed class BoundRecordLiteralExpression : BoundExpression
{
    public TypeSymbol RecordType { get; }
    public IReadOnlyList<BoundRecordFieldAssignment> Fields { get; }

    public BoundRecordLiteralExpression(TypeSymbol recordType, IReadOnlyList<BoundRecordFieldAssignment> fields)
    {
        RecordType = recordType;
        Fields = fields;
    }

    public override TypeSymbol Type => RecordType;
}
