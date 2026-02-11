using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class ShorthandLambdaExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken ParameterIdentifier { get; }
    public SyntaxToken ArrowToken { get; }
    public ExpressionSyntax BodyExpression { get; }

    public ShorthandLambdaExpressionSyntax(SyntaxToken parameterIdentifier, SyntaxToken arrowToken, ExpressionSyntax bodyExpression)
    {
        ParameterIdentifier = parameterIdentifier;
        ArrowToken = arrowToken;
        BodyExpression = bodyExpression;
    }

    public override TextSpan Span => TextSpan.FromBounds(ParameterIdentifier.Span.Start, BodyExpression.Span.End);
}
