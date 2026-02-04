using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class ListExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken OpenBracketToken { get; }
    public IReadOnlyList<ExpressionSyntax> Elements { get; }
    public SyntaxToken CloseBracketToken { get; }

    public ListExpressionSyntax(
        SyntaxToken openBracketToken,
        IReadOnlyList<ExpressionSyntax> elements,
        SyntaxToken closeBracketToken)
    {
        OpenBracketToken = openBracketToken;
        Elements = elements;
        CloseBracketToken = closeBracketToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(OpenBracketToken.Span.Start, CloseBracketToken.Span.End);
}
