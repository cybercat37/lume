using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class FunctionDeclarationSyntax : StatementSyntax
{
    public IReadOnlyList<string> Aspects { get; }
    public SyntaxToken FnKeyword { get; }
    public SyntaxToken IdentifierToken { get; }
    public SyntaxToken? TypeParameterOpenToken { get; }
    public IReadOnlyList<SyntaxToken> TypeParameters { get; }
    public SyntaxToken? TypeParameterCloseToken { get; }
    public SyntaxToken OpenParenToken { get; }
    public IReadOnlyList<FunctionParameterSyntax> Parameters { get; }
    public SyntaxToken CloseParenToken { get; }
    public SyntaxToken? ReturnTypeArrowToken { get; }
    public TypeSyntax? ReturnType { get; }
    public SyntaxToken? ArrowToken { get; }
    public BlockStatementSyntax? BodyBlock { get; }
    public ExpressionSyntax? BodyExpression { get; }

    public FunctionDeclarationSyntax(
        IReadOnlyList<string>? aspects,
        SyntaxToken fnKeyword,
        SyntaxToken identifierToken,
        SyntaxToken? typeParameterOpenToken,
        IReadOnlyList<SyntaxToken> typeParameters,
        SyntaxToken? typeParameterCloseToken,
        SyntaxToken openParenToken,
        IReadOnlyList<FunctionParameterSyntax> parameters,
        SyntaxToken closeParenToken,
        SyntaxToken? returnTypeArrowToken,
        TypeSyntax? returnType,
        SyntaxToken? arrowToken,
        BlockStatementSyntax? bodyBlock,
        ExpressionSyntax? bodyExpression)
    {
        Aspects = aspects ?? Array.Empty<string>();
        FnKeyword = fnKeyword;
        IdentifierToken = identifierToken;
        TypeParameterOpenToken = typeParameterOpenToken;
        TypeParameters = typeParameters;
        TypeParameterCloseToken = typeParameterCloseToken;
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
