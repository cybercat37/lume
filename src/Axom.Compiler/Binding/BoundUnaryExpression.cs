using Axom.Compiler.Lexing;

namespace Axom.Compiler.Binding;

public sealed class BoundUnaryExpression : BoundExpression
{
    public BoundExpression Operand { get; }
    public TokenKind OperatorKind { get; }
    public override TypeSymbol Type { get; }

    public BoundUnaryExpression(BoundExpression operand, TokenKind operatorKind, TypeSymbol type)
    {
        Operand = operand;
        OperatorKind = operatorKind;
        Type = type;
    }
}
