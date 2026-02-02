namespace Axom.Compiler.Binding;

public sealed class BoundRecordFieldAssignment
{
    public RecordFieldSymbol Field { get; }
    public BoundExpression Expression { get; }

    public BoundRecordFieldAssignment(RecordFieldSymbol field, BoundExpression expression)
    {
        Field = field;
        Expression = expression;
    }
}
