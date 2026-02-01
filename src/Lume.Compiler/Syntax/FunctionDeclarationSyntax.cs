using Lume.Compiler.Lexing;
using Lume.Compiler.Text;

namespace Lume.Compiler.Syntax;

public sealed class FunctionDeclarationSyntax : StatementSyntax
{
    public SyntaxToken FnKeyword { get; }
    public SyntaxToken IdentifierToken { get; }
    public SyntaxToken OpenParenToken { get; }
    public IReadOnlyList<FunctionParameterSyntax> Parameters { get; }
    public SyntaxToken CloseParenToken { get; }
    public SyntaxToken? ReturnTypeArrowToken { get; }
    public TypeSyntax? ReturnType { get; }
    public SyntaxToken? ArrowToken { get; }
    public BlockStatementSyntax? BodyBlock { get; }
    public ExpressionSyntax? BodyExpression { get; }

    public FunctionDeclarationSyntax(
        SyntaxToken fnKeyword,
        SyntaxToken identifierToken,
        SyntaxToken openParenToken,
        IReadOnlyList<FunctionParameterSyntax> parameters,
        SyntaxToken closeParenToken,
        SyntaxToken? returnTypeArrowToken,
        TypeSyntax? returnType,
        SyntaxToken? arrowToken,
        BlockStatementSyntax? bodyBlock,
        ExpressionSyntax? bodyExpression)
    {
        FnKeyword = fnKeyword;
        IdentifierToken = identifierToken;
        OpenParenToken = openParenToken;
        Parameters = parameters;
        CloseParenToken = closeParenToken;
        ReturnTypeArrowToken = returnTypeArrowToken;
        ReturnType = returnType;
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
