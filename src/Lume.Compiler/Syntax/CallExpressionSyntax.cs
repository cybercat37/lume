using Lume.Compiler.Lexing;
using Lume.Compiler.Text;

namespace Lume.Compiler.Syntax;

public sealed class CallExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken FunctionToken { get; }
    public SyntaxToken OpenParenToken { get; }
    public IReadOnlyList<ExpressionSyntax> Arguments { get; }
    public SyntaxToken CloseParenToken { get; }

    public CallExpressionSyntax(
        SyntaxToken functionToken,
        SyntaxToken openParenToken,
        IReadOnlyList<ExpressionSyntax> arguments,
        SyntaxToken closeParenToken)
    {
        FunctionToken = functionToken;
        OpenParenToken = openParenToken;
        Arguments = arguments;
        CloseParenToken = closeParenToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(FunctionToken.Span.Start, CloseParenToken.Span.End);
}
