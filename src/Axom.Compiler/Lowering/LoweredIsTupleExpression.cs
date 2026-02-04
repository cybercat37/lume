using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredIsTupleExpression : LoweredExpression
{
    public LoweredExpression Target { get; }
    public TypeSymbol TupleType { get; }

    public LoweredIsTupleExpression(LoweredExpression target, TypeSymbol tupleType)
    {
        Target = target;
        TupleType = tupleType;
    }

    public override TypeSymbol Type => TypeSymbol.Bool;
}
