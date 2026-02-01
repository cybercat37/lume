using Axom.Compiler.Lexing;

namespace Axom.Compiler.Binding;

public sealed class BoundBinaryExpression : BoundExpression
{
    public BoundExpression Left { get; }
    public BoundExpression Right { get; }
    public TokenKind OperatorKind { get; }
    public override TypeSymbol Type { get; }

    public BoundBinaryExpression(
        BoundExpression left,
        TokenKind operatorKind,
        BoundExpression right,
        TypeSymbol type)
    {
        Left = left;
        OperatorKind = operatorKind;
        Right = right;
        Type = type;
    }
}
