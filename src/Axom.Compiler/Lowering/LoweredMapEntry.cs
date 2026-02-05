using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredMapEntry
{
    public LoweredExpression Key { get; }
    public LoweredExpression Value { get; }

    public LoweredMapEntry(LoweredExpression key, LoweredExpression value)
    {
        Key = key;
        Value = value;
    }
}
