namespace Axom.Compiler.Binding;

public sealed class BoundChannelCreateExpression : BoundExpression
{
    public TypeSymbol ElementType { get; }
    public override TypeSymbol Type { get; }

    public BoundChannelCreateExpression(TypeSymbol elementType)
    {
        ElementType = elementType;
        Type = TypeSymbol.Tuple(new[] { TypeSymbol.Sender(elementType), TypeSymbol.Receiver(elementType) });
    }
}
