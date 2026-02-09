using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredChannelCreateExpression : LoweredExpression
{
    public TypeSymbol ElementType { get; }
    public int Capacity { get; }
    public override TypeSymbol Type { get; }

    public LoweredChannelCreateExpression(TypeSymbol elementType, int capacity)
    {
        ElementType = elementType;
        Capacity = capacity;
        Type = TypeSymbol.Tuple(new[] { TypeSymbol.Sender(elementType), TypeSymbol.Receiver(elementType) });
    }
}
