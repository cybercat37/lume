using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class MatchExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken MatchKeyword { get; }
    public ExpressionSyntax Expression { get; }
    public SyntaxToken OpenBraceToken { get; }
    public IReadOnlyList<MatchArmSyntax> Arms { get; }
    public SyntaxToken CloseBraceToken { get; }

    public MatchExpressionSyntax(
        SyntaxToken matchKeyword,
        ExpressionSyntax expression,
        SyntaxToken openBraceToken,
        IReadOnlyList<MatchArmSyntax> arms,
        SyntaxToken closeBraceToken)
    {
        MatchKeyword = matchKeyword;
        Expression = expression;
        OpenBraceToken = openBraceToken;
        Arms = arms;
        CloseBraceToken = closeBraceToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(MatchKeyword.Span.Start, CloseBraceToken.Span.End);
}
