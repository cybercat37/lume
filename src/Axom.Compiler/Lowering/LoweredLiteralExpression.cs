using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredLiteralExpression : LoweredExpression
{
    public object? Value { get; }
    public override TypeSymbol Type { get; }

    public LoweredLiteralExpression(object? value, TypeSymbol type)
    {
        Value = value;
        Type = type;
    }
}
