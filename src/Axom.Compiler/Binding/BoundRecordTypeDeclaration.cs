namespace Axom.Compiler.Binding;

public sealed class BoundRecordTypeDeclaration : BoundNode
{
    public TypeSymbol Type { get; }
    public IReadOnlyList<RecordFieldSymbol> Fields { get; }

    public BoundRecordTypeDeclaration(TypeSymbol type, IReadOnlyList<RecordFieldSymbol> fields)
    {
        Type = type;
        Fields = fields;
    }
}
