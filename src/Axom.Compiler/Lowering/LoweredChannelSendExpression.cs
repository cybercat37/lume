using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredChannelSendExpression : LoweredExpression
{
    public LoweredExpression Sender { get; }
    public LoweredExpression Value { get; }

    public LoweredChannelSendExpression(LoweredExpression sender, LoweredExpression value)
    {
        Sender = sender;
        Value = value;
    }

    public override TypeSymbol Type => TypeSymbol.Unit;
}
