using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class RecordSpreadSyntax : RecordLiteralEntrySyntax
{
    public SyntaxToken EllipsisToken { get; }
    public ExpressionSyntax Expression { get; }

    public RecordSpreadSyntax(SyntaxToken ellipsisToken, ExpressionSyntax expression)
    {
        EllipsisToken = ellipsisToken;
        Expression = expression;
    }

    public override TextSpan Span => TextSpan.FromBounds(EllipsisToken.Span.Start, Expression.Span.End);
}
