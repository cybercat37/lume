namespace Axom.Compiler.Binding;

public sealed class BoundLiteralPattern : BoundPattern
{
    public object? Value { get; }
    public override TypeSymbol Type { get; }

    public BoundLiteralPattern(object? value, TypeSymbol type)
    {
        Value = value;
        Type = type;
    }
}
