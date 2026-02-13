using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class ListPatternSyntax : PatternSyntax
{
    public SyntaxToken OpenBracketToken { get; }
    public IReadOnlyList<PatternSyntax> PrefixElements { get; }
    public SyntaxToken? EllipsisToken { get; }
    public PatternSyntax? RestPattern { get; }
    public IReadOnlyList<PatternSyntax> SuffixElements { get; }
    public SyntaxToken CloseBracketToken { get; }

    public bool HasRest => EllipsisToken is not null;

    public ListPatternSyntax(
        SyntaxToken openBracketToken,
        IReadOnlyList<PatternSyntax> prefixElements,
        SyntaxToken? ellipsisToken,
        PatternSyntax? restPattern,
        IReadOnlyList<PatternSyntax> suffixElements,
        SyntaxToken closeBracketToken)
    {
        OpenBracketToken = openBracketToken;
        PrefixElements = prefixElements;
        EllipsisToken = ellipsisToken;
        RestPattern = restPattern;
        SuffixElements = suffixElements;
        CloseBracketToken = closeBracketToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(OpenBracketToken.Span.Start, CloseBracketToken.Span.End);
}
