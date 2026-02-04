using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredFieldAccessExpression : LoweredExpression
{
    public LoweredExpression Target { get; }
    public RecordFieldSymbol Field { get; }

    public LoweredFieldAccessExpression(LoweredExpression target, RecordFieldSymbol field)
    {
        Target = target;
        Field = field;
    }

    public override TypeSymbol Type => Field.Type;
}
