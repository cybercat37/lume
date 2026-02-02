using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class VariantPatternSyntax : PatternSyntax
{
    public SyntaxToken IdentifierToken { get; }
    public SyntaxToken OpenParenToken { get; }
    public PatternSyntax Payload { get; }
    public SyntaxToken CloseParenToken { get; }

    public VariantPatternSyntax(
        SyntaxToken identifierToken,
        SyntaxToken openParenToken,
        PatternSyntax payload,
        SyntaxToken closeParenToken)
    {
        IdentifierToken = identifierToken;
        OpenParenToken = openParenToken;
        Payload = payload;
        CloseParenToken = closeParenToken;
    }

    public override TextSpan Span
    {
        get
        {
            return TextSpan.FromBounds(IdentifierToken.Span.Start, CloseParenToken.Span.End);
        }
    }
}
