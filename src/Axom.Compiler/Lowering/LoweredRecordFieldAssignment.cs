using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredRecordFieldAssignment
{
    public RecordFieldSymbol Field { get; }
    public LoweredExpression Expression { get; }

    public LoweredRecordFieldAssignment(RecordFieldSymbol field, LoweredExpression expression)
    {
        Field = field;
        Expression = expression;
    }
}
