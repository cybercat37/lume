using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class VariableDeclarationSyntax : StatementSyntax
{
    public SyntaxToken LetKeyword { get; }
    public SyntaxToken? MutKeyword { get; }
    public SyntaxToken IdentifierToken { get; }
    public SyntaxToken EqualsToken { get; }
    public ExpressionSyntax Initializer { get; }
    public SyntaxToken? SemicolonToken { get; }

    public VariableDeclarationSyntax(
        SyntaxToken letKeyword,
        SyntaxToken? mutKeyword,
        SyntaxToken identifierToken,
        SyntaxToken equalsToken,
        ExpressionSyntax initializer,
        SyntaxToken? semicolonToken)
    {
        LetKeyword = letKeyword;
        MutKeyword = mutKeyword;
        IdentifierToken = identifierToken;
        EqualsToken = equalsToken;
        Initializer = initializer;
        SemicolonToken = semicolonToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(LetKeyword.Span.Start, (SemicolonToken?.Span.End ?? Initializer.Span.End));
}
