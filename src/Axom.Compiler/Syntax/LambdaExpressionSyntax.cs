using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class LambdaExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken FnKeyword { get; }
    public SyntaxToken OpenParenToken { get; }
    public IReadOnlyList<FunctionParameterSyntax> Parameters { get; }
    public SyntaxToken CloseParenToken { get; }
    public SyntaxToken? ArrowToken { get; }
    public BlockStatementSyntax? BodyBlock { get; }
    public ExpressionSyntax? BodyExpression { get; }

    public LambdaExpressionSyntax(
        SyntaxToken fnKeyword,
        SyntaxToken openParenToken,
        IReadOnlyList<FunctionParameterSyntax> parameters,
        SyntaxToken closeParenToken,
        SyntaxToken? arrowToken,
        BlockStatementSyntax? bodyBlock,
        ExpressionSyntax? bodyExpression)
    {
        FnKeyword = fnKeyword;
        OpenParenToken = openParenToken;
        Parameters = parameters;
        CloseParenToken = closeParenToken;
        ArrowToken = arrowToken;
        BodyBlock = bodyBlock;
        BodyExpression = bodyExpression;
    }

    public override TextSpan Span
    {
        get
        {
            var end = BodyBlock?.Span.End ?? BodyExpression?.Span.End ?? CloseParenToken.Span.End;
            return TextSpan.FromBounds(FnKeyword.Span.Start, end);
        }
    }
}
