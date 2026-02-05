using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class ScopeStatementSyntax : StatementSyntax
{
    public SyntaxToken ScopeKeyword { get; }
    public BlockStatementSyntax Body { get; }

    public ScopeStatementSyntax(SyntaxToken scopeKeyword, BlockStatementSyntax body)
    {
        ScopeKeyword = scopeKeyword;
        Body = body;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(ScopeKeyword.Span.Start, Body.Span.End);
}
