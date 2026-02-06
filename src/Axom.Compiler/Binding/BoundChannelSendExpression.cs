namespace Axom.Compiler.Binding;

public sealed class BoundChannelSendExpression : BoundExpression
{
    public BoundExpression Sender { get; }
    public BoundExpression Value { get; }

    public BoundChannelSendExpression(BoundExpression sender, BoundExpression value)
    {
        Sender = sender;
        Value = value;
    }

    public override TypeSymbol Type => TypeSymbol.Unit;
}
