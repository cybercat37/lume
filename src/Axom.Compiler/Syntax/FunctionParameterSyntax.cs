using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class FunctionParameterSyntax : SyntaxNode
{
    public SyntaxToken IdentifierToken { get; }
    public SyntaxToken ColonToken { get; }
    public TypeSyntax Type { get; }

    public FunctionParameterSyntax(
        SyntaxToken identifierToken,
        SyntaxToken colonToken,
        TypeSyntax type)
    {
        IdentifierToken = identifierToken;
        ColonToken = colonToken;
        Type = type;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(IdentifierToken.Span.Start, Type.Span.End);
}
