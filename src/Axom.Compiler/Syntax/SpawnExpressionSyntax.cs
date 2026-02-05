using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class SpawnExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken SpawnKeyword { get; }
    public BlockStatementSyntax Body { get; }

    public SpawnExpressionSyntax(SyntaxToken spawnKeyword, BlockStatementSyntax body)
    {
        SpawnKeyword = spawnKeyword;
        Body = body;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(SpawnKeyword.Span.Start, Body.Span.End);
}
