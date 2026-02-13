using Axom.Compiler.Lexing;
using Axom.Compiler.Text;
using System;

namespace Axom.Compiler.Syntax;

public sealed class NameTypeSyntax : TypeSyntax
{
    public SyntaxToken IdentifierToken { get; }
    public SyntaxToken? TypeArgumentOpenToken { get; }
    public IReadOnlyList<TypeSyntax> TypeArguments { get; }
    public SyntaxToken? TypeArgumentCloseToken { get; }

    public NameTypeSyntax(
        SyntaxToken identifierToken,
        SyntaxToken? typeArgumentOpenToken = null,
        IReadOnlyList<TypeSyntax>? typeArguments = null,
        SyntaxToken? typeArgumentCloseToken = null)
    {
        IdentifierToken = identifierToken;
        TypeArgumentOpenToken = typeArgumentOpenToken;
        TypeArguments = typeArguments ?? Array.Empty<TypeSyntax>();
        TypeArgumentCloseToken = typeArgumentCloseToken;
    }

    public override TextSpan Span => TypeArgumentCloseToken is null
        ? IdentifierToken.Span
        : TextSpan.FromBounds(IdentifierToken.Span.Start, TypeArgumentCloseToken.Span.End);
}
