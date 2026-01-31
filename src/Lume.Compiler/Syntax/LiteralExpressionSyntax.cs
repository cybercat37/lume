using Lume.Compiler.Lexing;
using Lume.Compiler.Text;

namespace Lume.Compiler.Syntax;

public sealed class LiteralExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken LiteralToken { get; }

    public LiteralExpressionSyntax(SyntaxToken literalToken)
    {
        LiteralToken = literalToken;
    }

    public override TextSpan Span => LiteralToken.Span;
}
