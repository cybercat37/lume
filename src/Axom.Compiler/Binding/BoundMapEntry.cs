namespace Axom.Compiler.Binding;

public sealed class BoundMapEntry
{
    public BoundExpression Key { get; }
    public BoundExpression Value { get; }

    public BoundMapEntry(BoundExpression key, BoundExpression value)
    {
        Key = key;
        Value = value;
    }
}
