using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class MapExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken OpenBracketToken { get; }
    public IReadOnlyList<MapEntrySyntax> Entries { get; }
    public SyntaxToken CloseBracketToken { get; }

    public MapExpressionSyntax(
        SyntaxToken openBracketToken,
        IReadOnlyList<MapEntrySyntax> entries,
        SyntaxToken closeBracketToken)
    {
        OpenBracketToken = openBracketToken;
        Entries = entries;
        CloseBracketToken = closeBracketToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(OpenBracketToken.Span.Start, CloseBracketToken.Span.End);
}
