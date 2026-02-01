using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class CallExpressionSyntax : ExpressionSyntax
{
    public ExpressionSyntax Callee { get; }
    public SyntaxToken OpenParenToken { get; }
    public IReadOnlyList<ExpressionSyntax> Arguments { get; }
    public SyntaxToken CloseParenToken { get; }

    public CallExpressionSyntax(
        ExpressionSyntax callee,
        SyntaxToken openParenToken,
        IReadOnlyList<ExpressionSyntax> arguments,
        SyntaxToken closeParenToken)
    {
        Callee = callee;
        OpenParenToken = openParenToken;
        Arguments = arguments;
        CloseParenToken = closeParenToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(Callee.Span.Start, CloseParenToken.Span.End);
}
