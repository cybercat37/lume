using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class IntentAnnotationSyntax : SyntaxNode
{
    public SyntaxToken AtToken { get; }
    public SyntaxToken IntentIdentifier { get; }
    public SyntaxToken OpenParenToken { get; }
    public SyntaxToken MessageToken { get; }
    public SyntaxToken CloseParenToken { get; }

    public IntentAnnotationSyntax(
        SyntaxToken atToken,
        SyntaxToken intentIdentifier,
        SyntaxToken openParenToken,
        SyntaxToken messageToken,
        SyntaxToken closeParenToken)
    {
        AtToken = atToken;
        IntentIdentifier = intentIdentifier;
        OpenParenToken = openParenToken;
        MessageToken = messageToken;
        CloseParenToken = closeParenToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(AtToken.Span.Start, CloseParenToken.Span.End);
}
