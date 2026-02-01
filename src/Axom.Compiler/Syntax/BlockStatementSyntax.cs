using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class BlockStatementSyntax : StatementSyntax
{
    public SyntaxToken OpenBraceToken { get; }
    public IReadOnlyList<StatementSyntax> Statements { get; }
    public SyntaxToken CloseBraceToken { get; }

    public BlockStatementSyntax(
        SyntaxToken openBraceToken,
        IReadOnlyList<StatementSyntax> statements,
        SyntaxToken closeBraceToken)
    {
        OpenBraceToken = openBraceToken;
        Statements = statements;
        CloseBraceToken = closeBraceToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(OpenBraceToken.Span.Start, CloseBraceToken.Span.End);
}
