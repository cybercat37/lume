using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class QuestionExpressionSyntax : ExpressionSyntax
{
    public ExpressionSyntax Expression { get; }
    public SyntaxToken QuestionToken { get; }

    public QuestionExpressionSyntax(ExpressionSyntax expression, SyntaxToken questionToken)
    {
        Expression = expression;
        QuestionToken = questionToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(Expression.Span.Start, QuestionToken.Span.End);
}
