using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class MatchArmSyntax : SyntaxNode
{
    public PatternSyntax Pattern { get; }
    public SyntaxToken? IfKeyword { get; }
    public ExpressionSyntax? Guard { get; }
    public SyntaxToken ArrowToken { get; }
    public ExpressionSyntax Expression { get; }

    public MatchArmSyntax(
        PatternSyntax pattern,
        SyntaxToken? ifKeyword,
        ExpressionSyntax? guard,
        SyntaxToken arrowToken,
        ExpressionSyntax expression)
    {
        Pattern = pattern;
        IfKeyword = ifKeyword;
        Guard = guard;
        ArrowToken = arrowToken;
        Expression = expression;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(Pattern.Span.Start, Expression.Span.End);
}
