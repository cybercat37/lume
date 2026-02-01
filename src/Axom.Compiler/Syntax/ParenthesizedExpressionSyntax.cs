using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class ParenthesizedExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken OpenParenToken { get; }
    public ExpressionSyntax Expression { get; }
    public SyntaxToken CloseParenToken { get; }

    public ParenthesizedExpressionSyntax(
        SyntaxToken openParenToken,
        ExpressionSyntax expression,
        SyntaxToken closeParenToken)
    {
        OpenParenToken = openParenToken;
        Expression = expression;
        CloseParenToken = closeParenToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(OpenParenToken.Span.Start, CloseParenToken.Span.End);
}
