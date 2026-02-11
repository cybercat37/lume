using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class VariableDeclarationSyntax : StatementSyntax
{
    public IntentAnnotationSyntax? IntentAnnotation { get; }
    public SyntaxToken LetKeyword { get; }
    public SyntaxToken? MutKeyword { get; }
    public PatternSyntax Pattern { get; }
    public SyntaxToken EqualsToken { get; }
    public ExpressionSyntax Initializer { get; }
    public SyntaxToken? SemicolonToken { get; }

    public VariableDeclarationSyntax(
        IntentAnnotationSyntax? intentAnnotation,
        SyntaxToken letKeyword,
        SyntaxToken? mutKeyword,
        PatternSyntax pattern,
        SyntaxToken equalsToken,
        ExpressionSyntax initializer,
        SyntaxToken? semicolonToken)
    {
        IntentAnnotation = intentAnnotation;
        LetKeyword = letKeyword;
        MutKeyword = mutKeyword;
        Pattern = pattern;
        EqualsToken = equalsToken;
        Initializer = initializer;
        SemicolonToken = semicolonToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds((IntentAnnotation?.Span.Start ?? LetKeyword.Span.Start), (SemicolonToken?.Span.End ?? Initializer.Span.End));
}
