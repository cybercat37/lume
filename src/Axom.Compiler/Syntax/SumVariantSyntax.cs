using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class SumVariantSyntax : SyntaxNode
{
    public SyntaxToken IdentifierToken { get; }
    public SyntaxToken? OpenParenToken { get; }
    public TypeSyntax? PayloadType { get; }
    public SyntaxToken? CloseParenToken { get; }

    public SumVariantSyntax(
        SyntaxToken identifierToken,
        SyntaxToken? openParenToken,
        TypeSyntax? payloadType,
        SyntaxToken? closeParenToken)
    {
        IdentifierToken = identifierToken;
        OpenParenToken = openParenToken;
        PayloadType = payloadType;
        CloseParenToken = closeParenToken;
    }

    public override TextSpan Span
    {
        get
        {
            var end = CloseParenToken?.Span.End ?? IdentifierToken.Span.End;
            return TextSpan.FromBounds(IdentifierToken.Span.Start, end);
        }
    }
}
