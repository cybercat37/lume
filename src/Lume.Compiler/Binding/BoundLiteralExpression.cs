namespace Lume.Compiler.Binding;

public sealed class BoundLiteralExpression : BoundExpression
{
    public object? Value { get; }
    public override TypeSymbol Type { get; }

    public BoundLiteralExpression(object? value, TypeSymbol type)
    {
        Value = value;
        Type = type;
    }
}
