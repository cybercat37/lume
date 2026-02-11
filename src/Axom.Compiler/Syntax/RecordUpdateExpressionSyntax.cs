using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class RecordUpdateExpressionSyntax : ExpressionSyntax
{
    public ExpressionSyntax Target { get; }
    public SyntaxToken WithKeyword { get; }
    public SyntaxToken OpenBraceToken { get; }
    public IReadOnlyList<RecordFieldAssignmentSyntax> Fields { get; }
    public SyntaxToken CloseBraceToken { get; }

    public RecordUpdateExpressionSyntax(
        ExpressionSyntax target,
        SyntaxToken withKeyword,
        SyntaxToken openBraceToken,
        IReadOnlyList<RecordFieldAssignmentSyntax> fields,
        SyntaxToken closeBraceToken)
    {
        Target = target;
        WithKeyword = withKeyword;
        OpenBraceToken = openBraceToken;
        Fields = fields;
        CloseBraceToken = closeBraceToken;
    }

    public override TextSpan Span => TextSpan.FromBounds(Target.Span.Start, CloseBraceToken.Span.End);
}
