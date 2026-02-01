using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class MatchArmSyntax : SyntaxNode
{
    public PatternSyntax Pattern { get; }
    public SyntaxToken ArrowToken { get; }
    public ExpressionSyntax Expression { get; }

    public MatchArmSyntax(PatternSyntax pattern, SyntaxToken arrowToken, ExpressionSyntax expression)
    {
        Pattern = pattern;
        ArrowToken = arrowToken;
        Expression = expression;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(Pattern.Span.Start, Expression.Span.End);
}
