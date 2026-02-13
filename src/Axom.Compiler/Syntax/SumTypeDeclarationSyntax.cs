using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class SumTypeDeclarationSyntax : StatementSyntax
{
    public SyntaxToken TypeKeyword { get; }
    public SyntaxToken IdentifierToken { get; }
    public SyntaxToken? TypeParameterOpenToken { get; }
    public IReadOnlyList<SyntaxToken> TypeParameters { get; }
    public SyntaxToken? TypeParameterCloseToken { get; }
    public SyntaxToken OpenBraceToken { get; }
    public IReadOnlyList<SumVariantSyntax> Variants { get; }
    public SyntaxToken CloseBraceToken { get; }

    public SumTypeDeclarationSyntax(
        SyntaxToken typeKeyword,
        SyntaxToken identifierToken,
        SyntaxToken? typeParameterOpenToken,
        IReadOnlyList<SyntaxToken> typeParameters,
        SyntaxToken? typeParameterCloseToken,
        SyntaxToken openBraceToken,
        IReadOnlyList<SumVariantSyntax> variants,
        SyntaxToken closeBraceToken)
    {
        TypeKeyword = typeKeyword;
        IdentifierToken = identifierToken;
        TypeParameterOpenToken = typeParameterOpenToken;
        TypeParameters = typeParameters;
        TypeParameterCloseToken = typeParameterCloseToken;
        OpenBraceToken = openBraceToken;
        Variants = variants;
        CloseBraceToken = closeBraceToken;
    }

    public override TextSpan Span
    {
        get
        {
            return TextSpan.FromBounds(TypeKeyword.Span.Start, CloseBraceToken.Span.End);
        }
    }
}
