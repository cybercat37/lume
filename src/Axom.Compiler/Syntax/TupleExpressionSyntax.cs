using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class TupleExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken OpenParenToken { get; }
    public IReadOnlyList<ExpressionSyntax> Elements { get; }
    public SyntaxToken CloseParenToken { get; }

    public TupleExpressionSyntax(
        SyntaxToken openParenToken,
        IReadOnlyList<ExpressionSyntax> elements,
        SyntaxToken closeParenToken)
    {
        OpenParenToken = openParenToken;
        Elements = elements;
        CloseParenToken = closeParenToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(OpenParenToken.Span.Start, CloseParenToken.Span.End);
}
