using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class RecordLiteralExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken IdentifierToken { get; }
    public SyntaxToken OpenBraceToken { get; }
    public IReadOnlyList<RecordLiteralEntrySyntax> Entries { get; }
    public SyntaxToken CloseBraceToken { get; }

    public RecordLiteralExpressionSyntax(
        SyntaxToken identifierToken,
        SyntaxToken openBraceToken,
        IReadOnlyList<RecordLiteralEntrySyntax> entries,
        SyntaxToken closeBraceToken)
    {
        IdentifierToken = identifierToken;
        OpenBraceToken = openBraceToken;
        Entries = entries;
        CloseBraceToken = closeBraceToken;
    }

    public override TextSpan Span
    {
        get
        {
            return TextSpan.FromBounds(IdentifierToken.Span.Start, CloseBraceToken.Span.End);
        }
    }
}
