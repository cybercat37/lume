using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredChannelReceiveExpression : LoweredExpression
{
    public LoweredExpression Receiver { get; }
    public override TypeSymbol Type { get; }

    public LoweredChannelReceiveExpression(LoweredExpression receiver, TypeSymbol type)
    {
        Receiver = receiver;
        Type = type;
    }
}
