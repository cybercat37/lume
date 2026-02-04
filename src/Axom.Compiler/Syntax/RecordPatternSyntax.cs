using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class RecordPatternSyntax : PatternSyntax
{
    public SyntaxToken IdentifierToken { get; }
    public SyntaxToken OpenBraceToken { get; }
    public IReadOnlyList<RecordFieldPatternSyntax> Fields { get; }
    public SyntaxToken CloseBraceToken { get; }

    public RecordPatternSyntax(
        SyntaxToken identifierToken,
        SyntaxToken openBraceToken,
        IReadOnlyList<RecordFieldPatternSyntax> fields,
        SyntaxToken closeBraceToken)
    {
        IdentifierToken = identifierToken;
        OpenBraceToken = openBraceToken;
        Fields = fields;
        CloseBraceToken = closeBraceToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(IdentifierToken.Span.Start, CloseBraceToken.Span.End);
}
