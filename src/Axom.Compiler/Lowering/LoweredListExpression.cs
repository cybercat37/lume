using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredListExpression : LoweredExpression
{
    public IReadOnlyList<LoweredExpression> Elements { get; }
    public override TypeSymbol Type { get; }

    public LoweredListExpression(IReadOnlyList<LoweredExpression> elements, TypeSymbol type)
    {
        Elements = elements;
        Type = type;
    }
}
