using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredIsRecordExpression : LoweredExpression
{
    public LoweredExpression Target { get; }
    public TypeSymbol RecordType { get; }

    public LoweredIsRecordExpression(LoweredExpression target, TypeSymbol recordType)
    {
        Target = target;
        RecordType = recordType;
    }

    public override TypeSymbol Type => TypeSymbol.Bool;
}
