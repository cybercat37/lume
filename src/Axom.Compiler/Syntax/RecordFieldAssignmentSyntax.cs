using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class RecordFieldAssignmentSyntax : RecordLiteralEntrySyntax
{
    public SyntaxToken IdentifierToken { get; }
    public SyntaxToken ColonToken { get; }
    public ExpressionSyntax Expression { get; }

    public RecordFieldAssignmentSyntax(
        SyntaxToken identifierToken,
        SyntaxToken colonToken,
        ExpressionSyntax expression)
    {
        IdentifierToken = identifierToken;
        ColonToken = colonToken;
        Expression = expression;
    }

    public override TextSpan Span
    {
        get
        {
            return TextSpan.FromBounds(IdentifierToken.Span.Start, Expression.Span.End);
        }
    }
}
