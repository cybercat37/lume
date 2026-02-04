using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class TupleTypeSyntax : TypeSyntax
{
    public SyntaxToken OpenParenToken { get; }
    public IReadOnlyList<TypeSyntax> Elements { get; }
    public SyntaxToken CloseParenToken { get; }

    public TupleTypeSyntax(
        SyntaxToken openParenToken,
        IReadOnlyList<TypeSyntax> elements,
        SyntaxToken closeParenToken)
    {
        OpenParenToken = openParenToken;
        Elements = elements;
        CloseParenToken = closeParenToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(OpenParenToken.Span.Start, CloseParenToken.Span.End);
}
