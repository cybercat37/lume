using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredMapExpression : LoweredExpression
{
    public IReadOnlyList<LoweredMapEntry> Entries { get; }
    public override TypeSymbol Type { get; }

    public LoweredMapExpression(IReadOnlyList<LoweredMapEntry> entries, TypeSymbol type)
    {
        Entries = entries;
        Type = type;
    }
}
