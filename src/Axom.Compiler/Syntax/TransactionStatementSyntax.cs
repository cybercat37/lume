using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class TransactionStatementSyntax : StatementSyntax
{
    public SyntaxToken TransactionKeyword { get; }

    public BlockStatementSyntax Body { get; }

    public TransactionStatementSyntax(SyntaxToken transactionKeyword, BlockStatementSyntax body)
    {
        TransactionKeyword = transactionKeyword;
        Body = body;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(TransactionKeyword.Span.Start, Body.Span.End);
}
