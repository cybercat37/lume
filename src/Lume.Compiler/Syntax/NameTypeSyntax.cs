using Lume.Compiler.Lexing;
using Lume.Compiler.Text;

namespace Lume.Compiler.Syntax;

public sealed class NameTypeSyntax : TypeSyntax
{
    public SyntaxToken IdentifierToken { get; }

    public NameTypeSyntax(SyntaxToken identifierToken)
    {
        IdentifierToken = identifierToken;
    }

    public override TextSpan Span => IdentifierToken.Span;
}
