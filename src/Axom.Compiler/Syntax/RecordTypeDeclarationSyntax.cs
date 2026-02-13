using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class RecordTypeDeclarationSyntax : StatementSyntax
{
    public SyntaxToken TypeKeyword { get; }
    public SyntaxToken IdentifierToken { get; }
    public SyntaxToken? TypeParameterOpenToken { get; }
    public IReadOnlyList<SyntaxToken> TypeParameters { get; }
    public SyntaxToken? TypeParameterCloseToken { get; }
    public SyntaxToken OpenBraceToken { get; }
    public IReadOnlyList<RecordFieldSyntax> Fields { get; }
    public SyntaxToken CloseBraceToken { get; }

    public RecordTypeDeclarationSyntax(
        SyntaxToken typeKeyword,
        SyntaxToken identifierToken,
        SyntaxToken? typeParameterOpenToken,
        IReadOnlyList<SyntaxToken> typeParameters,
        SyntaxToken? typeParameterCloseToken,
        SyntaxToken openBraceToken,
        IReadOnlyList<RecordFieldSyntax> fields,
        SyntaxToken closeBraceToken)
    {
        TypeKeyword = typeKeyword;
        IdentifierToken = identifierToken;
        TypeParameterOpenToken = typeParameterOpenToken;
        TypeParameters = typeParameters;
        TypeParameterCloseToken = typeParameterCloseToken;
        OpenBraceToken = openBraceToken;
        Fields = fields;
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
