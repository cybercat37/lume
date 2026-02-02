using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class RecordTypeDeclarationSyntax : StatementSyntax
{
    public SyntaxToken TypeKeyword { get; }
    public SyntaxToken IdentifierToken { get; }
    public SyntaxToken OpenBraceToken { get; }
    public IReadOnlyList<RecordFieldSyntax> Fields { get; }
    public SyntaxToken CloseBraceToken { get; }

    public RecordTypeDeclarationSyntax(
        SyntaxToken typeKeyword,
        SyntaxToken identifierToken,
        SyntaxToken openBraceToken,
        IReadOnlyList<RecordFieldSyntax> fields,
        SyntaxToken closeBraceToken)
    {
        TypeKeyword = typeKeyword;
        IdentifierToken = identifierToken;
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
