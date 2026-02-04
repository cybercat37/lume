using Axom.Compiler.Binding;
using Axom.Compiler.Lexing;

namespace Axom.Compiler.Lowering;

public sealed class LoweredUnaryExpression : LoweredExpression
{
    public TokenKind OperatorKind { get; }
    public LoweredExpression Operand { get; }
    public override TypeSymbol Type { get; }

    public LoweredUnaryExpression(TokenKind operatorKind, LoweredExpression operand, TypeSymbol type)
    {
        OperatorKind = operatorKind;
        Operand = operand;
        Type = type;
    }
}
