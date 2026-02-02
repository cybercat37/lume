using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class FieldAccessExpressionSyntax : ExpressionSyntax
{
    public ExpressionSyntax Target { get; }
    public SyntaxToken DotToken { get; }
    public SyntaxToken IdentifierToken { get; }

    public FieldAccessExpressionSyntax(ExpressionSyntax target, SyntaxToken dotToken, SyntaxToken identifierToken)
    {
        Target = target;
        DotToken = dotToken;
        IdentifierToken = identifierToken;
    }

    public override TextSpan Span
    {
        get
        {
            return TextSpan.FromBounds(Target.Span.Start, IdentifierToken.Span.End);
        }
    }
}
