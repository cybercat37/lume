namespace Lume.Compiler.Binding;

public sealed class BoundBinaryExpression : BoundExpression
{
    public BoundExpression Left { get; }
    public BoundExpression Right { get; }

    public BoundBinaryExpression(BoundExpression left, BoundExpression right)
    {
        Left = left;
        Right = right;
    }
}
