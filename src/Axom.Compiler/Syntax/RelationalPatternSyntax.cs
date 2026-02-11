using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class RelationalPatternSyntax : PatternSyntax
{
    public SyntaxToken OperatorToken { get; }
    public ExpressionSyntax RightExpression { get; }

    public RelationalPatternSyntax(SyntaxToken operatorToken, ExpressionSyntax rightExpression)
    {
        OperatorToken = operatorToken;
        RightExpression = rightExpression;
    }

    public override TextSpan Span => TextSpan.FromBounds(OperatorToken.Span.Start, RightExpression.Span.End);
}
