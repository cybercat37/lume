using Lume.Compiler.Lexing;
using Lume.Compiler.Text;

namespace Lume.Compiler.Syntax;

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
