using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class NameTypeSyntax : TypeSyntax
{
    public SyntaxToken IdentifierToken { get; }

    public NameTypeSyntax(SyntaxToken identifierToken)
    {
        IdentifierToken = identifierToken;
    }

    public override TextSpan Span => IdentifierToken.Span;
}
