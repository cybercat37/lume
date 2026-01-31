namespace Lume.Compiler.Binding;

public sealed class BoundLiteralExpression : BoundExpression
{
    public object? Value { get; }

    public BoundLiteralExpression(object? value)
    {
        Value = value;
    }
}
