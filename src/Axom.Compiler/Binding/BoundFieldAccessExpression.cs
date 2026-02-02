namespace Axom.Compiler.Binding;

public sealed class BoundFieldAccessExpression : BoundExpression
{
    public BoundExpression Target { get; }
    public RecordFieldSymbol Field { get; }

    public BoundFieldAccessExpression(BoundExpression target, RecordFieldSymbol field)
    {
        Target = target;
        Field = field;
    }

    public override TypeSymbol Type => Field.Type;
}
