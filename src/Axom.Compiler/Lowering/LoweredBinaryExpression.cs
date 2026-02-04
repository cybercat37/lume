using Axom.Compiler.Binding;
using Axom.Compiler.Lexing;

namespace Axom.Compiler.Lowering;

public sealed class LoweredBinaryExpression : LoweredExpression
{
    public LoweredExpression Left { get; }
    public TokenKind OperatorKind { get; }
    public LoweredExpression Right { get; }
    public override TypeSymbol Type { get; }

    public LoweredBinaryExpression(LoweredExpression left, TokenKind operatorKind, LoweredExpression right, TypeSymbol type)
    {
        Left = left;
        OperatorKind = operatorKind;
        Right = right;
        Type = type;
    }
}
