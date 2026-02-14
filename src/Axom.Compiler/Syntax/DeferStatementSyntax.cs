using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class DeferStatementSyntax : StatementSyntax
{
    public SyntaxToken DeferKeyword { get; }
    public ExpressionSyntax? Expression { get; }
    public BlockStatementSyntax? Block { get; }

    public DeferStatementSyntax(SyntaxToken deferKeyword, ExpressionSyntax? expression, BlockStatementSyntax? block)
    {
        DeferKeyword = deferKeyword;
        Expression = expression;
        Block = block;
    }

    public override TextSpan Span
    {
        get
        {
            var end = Block?.Span.End ?? Expression?.Span.End ?? DeferKeyword.Span.End;
            return TextSpan.FromBounds(DeferKeyword.Span.Start, end);
        }
    }
}
