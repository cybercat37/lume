namespace Axom.Compiler.Binding;

public sealed class BoundRecordPattern : BoundPattern
{
    public BoundRecordTypeDeclaration RecordType { get; }
    public IReadOnlyList<BoundRecordFieldPattern> Fields { get; }
    public override TypeSymbol Type { get; }

    public BoundRecordPattern(
        BoundRecordTypeDeclaration recordType,
        IReadOnlyList<BoundRecordFieldPattern> fields,
        TypeSymbol type)
    {
        RecordType = recordType;
        Fields = fields;
        Type = type;
    }
}
