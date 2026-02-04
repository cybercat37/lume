using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredTupleExpression : LoweredExpression
{
    public IReadOnlyList<LoweredExpression> Elements { get; }
    public override TypeSymbol Type { get; }

    public LoweredTupleExpression(IReadOnlyList<LoweredExpression> elements, TypeSymbol type)
    {
        Elements = elements;
        Type = type;
    }
}
