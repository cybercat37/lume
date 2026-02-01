using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class TuplePatternSyntax : PatternSyntax
{
    public SyntaxToken OpenParenToken { get; }
    public IReadOnlyList<PatternSyntax> Elements { get; }
    public SyntaxToken CloseParenToken { get; }

    public TuplePatternSyntax(
        SyntaxToken openParenToken,
        IReadOnlyList<PatternSyntax> elements,
        SyntaxToken closeParenToken)
    {
        OpenParenToken = openParenToken;
        Elements = elements;
        CloseParenToken = closeParenToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(OpenParenToken.Span.Start, CloseParenToken.Span.End);
}
