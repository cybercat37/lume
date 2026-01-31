using Lume.Compiler.Lexing;
using Lume.Compiler.Text;

namespace Lume.Compiler.Syntax;

public sealed class PrintStatementSyntax : StatementSyntax
{
    public SyntaxToken KeywordToken { get; }
    public ExpressionSyntax Expression { get; }

    public PrintStatementSyntax(SyntaxToken keywordToken, ExpressionSyntax expression)
    {
        KeywordToken = keywordToken;
        Expression = expression;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(KeywordToken.Span.Start, Expression.Span.End);
}
