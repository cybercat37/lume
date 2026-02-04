using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class RecordFieldPatternSyntax : SyntaxNode
{
    public SyntaxToken IdentifierToken { get; }
    public SyntaxToken ColonToken { get; }
    public PatternSyntax Pattern { get; }

    public RecordFieldPatternSyntax(
        SyntaxToken identifierToken,
        SyntaxToken colonToken,
        PatternSyntax pattern)
    {
        IdentifierToken = identifierToken;
        ColonToken = colonToken;
        Pattern = pattern;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(IdentifierToken.Span.Start, Pattern.Span.End);
}
