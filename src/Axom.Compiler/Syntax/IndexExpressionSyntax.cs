using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class IndexExpressionSyntax : ExpressionSyntax
{
    public ExpressionSyntax Target { get; }
    public SyntaxToken OpenBracketToken { get; }
    public ExpressionSyntax Index { get; }
    public SyntaxToken CloseBracketToken { get; }

    public IndexExpressionSyntax(
        ExpressionSyntax target,
        SyntaxToken openBracketToken,
        ExpressionSyntax index,
        SyntaxToken closeBracketToken)
    {
        Target = target;
        OpenBracketToken = openBracketToken;
        Index = index;
        CloseBracketToken = closeBracketToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(Target.Span.Start, CloseBracketToken.Span.End);
}
