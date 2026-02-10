using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class GenericCallExpressionSyntax : ExpressionSyntax
{
    public ExpressionSyntax Callee { get; }
    public SyntaxToken LessToken { get; }
    public IReadOnlyList<TypeSyntax> TypeArguments { get; }
    public SyntaxToken GreaterToken { get; }
    public SyntaxToken OpenParenToken { get; }
    public IReadOnlyList<ExpressionSyntax> Arguments { get; }
    public SyntaxToken CloseParenToken { get; }

    public GenericCallExpressionSyntax(
        ExpressionSyntax callee,
        SyntaxToken lessToken,
        IReadOnlyList<TypeSyntax> typeArguments,
        SyntaxToken greaterToken,
        SyntaxToken openParenToken,
        IReadOnlyList<ExpressionSyntax> arguments,
        SyntaxToken closeParenToken)
    {
        Callee = callee;
        LessToken = lessToken;
        TypeArguments = typeArguments;
        GreaterToken = greaterToken;
        OpenParenToken = openParenToken;
        Arguments = arguments;
        CloseParenToken = closeParenToken;
    }

    public override TextSpan Span => TextSpan.FromBounds(Callee.Span.Start, CloseParenToken.Span.End);
}
