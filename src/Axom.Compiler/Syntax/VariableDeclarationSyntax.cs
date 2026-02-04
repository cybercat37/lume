using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class VariableDeclarationSyntax : StatementSyntax
{
    public SyntaxToken LetKeyword { get; }
    public SyntaxToken? MutKeyword { get; }
    public PatternSyntax Pattern { get; }
    public SyntaxToken EqualsToken { get; }
    public ExpressionSyntax Initializer { get; }
    public SyntaxToken? SemicolonToken { get; }

    public VariableDeclarationSyntax(
        SyntaxToken letKeyword,
        SyntaxToken? mutKeyword,
        PatternSyntax pattern,
        SyntaxToken equalsToken,
        ExpressionSyntax initializer,
        SyntaxToken? semicolonToken)
    {
        LetKeyword = letKeyword;
        MutKeyword = mutKeyword;
        Pattern = pattern;
        EqualsToken = equalsToken;
        Initializer = initializer;
        SemicolonToken = semicolonToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(LetKeyword.Span.Start, (SemicolonToken?.Span.End ?? Initializer.Span.End));
}
