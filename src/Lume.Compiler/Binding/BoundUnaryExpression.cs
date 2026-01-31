namespace Lume.Compiler.Binding;

public sealed class BoundUnaryExpression : BoundExpression
{
    public BoundExpression Operand { get; }

    public BoundUnaryExpression(BoundExpression operand)
    {
        Operand = operand;
    }
}
