namespace Axom.Compiler.Binding;

public sealed class BoundChannelCreateExpression : BoundExpression
{
    public TypeSymbol ElementType { get; }
    public int Capacity { get; }
    public override TypeSymbol Type { get; }

    public BoundChannelCreateExpression(TypeSymbol elementType, int capacity)
    {
        ElementType = elementType;
        Capacity = capacity;
        Type = TypeSymbol.Tuple(new[] { TypeSymbol.Sender(elementType), TypeSymbol.Receiver(elementType) });
    }
}
