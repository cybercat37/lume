using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class JoinExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken JoinKeyword { get; }
    public ExpressionSyntax Expression { get; }

    public JoinExpressionSyntax(SyntaxToken joinKeyword, ExpressionSyntax expression)
    {
        JoinKeyword = joinKeyword;
        Expression = expression;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(JoinKeyword.Span.Start, Expression.Span.End);
}
