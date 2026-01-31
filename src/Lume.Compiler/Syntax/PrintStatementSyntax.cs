using Lume.Compiler.Lexing;
using Lume.Compiler.Text;

namespace Lume.Compiler.Syntax;

public sealed class PrintStatementSyntax : StatementSyntax
{
    public SyntaxToken PrintKeyword { get; }
    public ExpressionSyntax Expression { get; }

    public PrintStatementSyntax(SyntaxToken printKeyword, ExpressionSyntax expression)
    {
        PrintKeyword = printKeyword;
        Expression = expression;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(PrintKeyword.Span.Start, Expression.Span.End);
}
