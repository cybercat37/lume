namespace Axom.Compiler.Binding;

public sealed class BoundChannelReceiveExpression : BoundExpression
{
    public BoundExpression Receiver { get; }
    public override TypeSymbol Type { get; }

    public BoundChannelReceiveExpression(BoundExpression receiver, TypeSymbol type)
    {
        Receiver = receiver;
        Type = type;
    }
}
