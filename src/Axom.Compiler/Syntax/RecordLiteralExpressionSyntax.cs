using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class RecordLiteralExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken IdentifierToken { get; }
    public SyntaxToken OpenBraceToken { get; }
    public IReadOnlyList<RecordFieldAssignmentSyntax> Fields { get; }
    public SyntaxToken CloseBraceToken { get; }

    public RecordLiteralExpressionSyntax(
        SyntaxToken identifierToken,
        SyntaxToken openBraceToken,
        IReadOnlyList<RecordFieldAssignmentSyntax> fields,
        SyntaxToken closeBraceToken)
    {
        IdentifierToken = identifierToken;
        OpenBraceToken = openBraceToken;
        Fields = fields;
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
