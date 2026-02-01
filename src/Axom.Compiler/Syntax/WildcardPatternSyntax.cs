using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class WildcardPatternSyntax : PatternSyntax
{
    public SyntaxToken IdentifierToken { get; }

    public WildcardPatternSyntax(SyntaxToken identifierToken)
    {
        IdentifierToken = identifierToken;
    }

    public override TextSpan Span =>
        IdentifierToken.Span;
}
