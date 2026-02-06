using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class ParExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken ParKeyword { get; }
    public ExpressionSyntax Expression { get; }

    public ParExpressionSyntax(SyntaxToken parKeyword, ExpressionSyntax expression)
    {
        ParKeyword = parKeyword;
        Expression = expression;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(ParKeyword.Span.Start, Expression.Span.End);
}
