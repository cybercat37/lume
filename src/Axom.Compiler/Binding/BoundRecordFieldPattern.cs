namespace Axom.Compiler.Binding;

public sealed class BoundRecordFieldPattern
{
    public RecordFieldSymbol Field { get; }
    public BoundPattern Pattern { get; }

    public BoundRecordFieldPattern(RecordFieldSymbol field, BoundPattern pattern)
    {
        Field = field;
        Pattern = pattern;
    }
}
