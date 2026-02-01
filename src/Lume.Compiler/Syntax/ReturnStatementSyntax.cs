using Lume.Compiler.Lexing;
using Lume.Compiler.Text;

namespace Lume.Compiler.Syntax;

public sealed class ReturnStatementSyntax : StatementSyntax
{
    public SyntaxToken ReturnKeyword { get; }
    public ExpressionSyntax? Expression { get; }

    public ReturnStatementSyntax(SyntaxToken returnKeyword, ExpressionSyntax? expression)
    {
        ReturnKeyword = returnKeyword;
        Expression = expression;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(ReturnKeyword.Span.Start, Expression?.Span.End ?? ReturnKeyword.Span.End);
}
