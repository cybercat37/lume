using Lume.Compiler.Lexing;
using Lume.Compiler.Text;

namespace Lume.Compiler.Syntax;

public sealed class AssignmentExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken IdentifierToken { get; }
    public SyntaxToken EqualsToken { get; }
    public ExpressionSyntax Expression { get; }

    public AssignmentExpressionSyntax(
        SyntaxToken identifierToken,
        SyntaxToken equalsToken,
        ExpressionSyntax expression)
    {
        IdentifierToken = identifierToken;
        EqualsToken = equalsToken;
        Expression = expression;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(IdentifierToken.Span.Start, Expression.Span.End);
}
