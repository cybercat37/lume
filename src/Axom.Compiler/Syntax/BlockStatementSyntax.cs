using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class BlockStatementSyntax : StatementSyntax
{
    public IntentAnnotationSyntax? IntentAnnotation { get; }
    public SyntaxToken OpenBraceToken { get; }
    public IReadOnlyList<StatementSyntax> Statements { get; }
    public SyntaxToken CloseBraceToken { get; }

    public BlockStatementSyntax(
        IntentAnnotationSyntax? intentAnnotation,
        SyntaxToken openBraceToken,
        IReadOnlyList<StatementSyntax> statements,
        SyntaxToken closeBraceToken)
    {
        IntentAnnotation = intentAnnotation;
        OpenBraceToken = openBraceToken;
        Statements = statements;
        CloseBraceToken = closeBraceToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds((IntentAnnotation?.Span.Start ?? OpenBraceToken.Span.Start), CloseBraceToken.Span.End);
}
