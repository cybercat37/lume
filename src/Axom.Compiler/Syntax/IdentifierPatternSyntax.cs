using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class IdentifierPatternSyntax : PatternSyntax
{
    public SyntaxToken IdentifierToken { get; }

    public IdentifierPatternSyntax(SyntaxToken identifierToken)
    {
        IdentifierToken = identifierToken;
    }

    public override TextSpan Span =>
        IdentifierToken.Span;
}
