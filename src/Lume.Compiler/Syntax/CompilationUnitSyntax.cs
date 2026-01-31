using Lume.Compiler.Lexing;
using Lume.Compiler.Text;

namespace Lume.Compiler.Syntax;

public sealed class CompilationUnitSyntax : SyntaxNode
{
    public StatementSyntax Statement { get; }
    public SyntaxToken EndOfFileToken { get; }

    public CompilationUnitSyntax(StatementSyntax statement, SyntaxToken endOfFileToken)
    {
        Statement = statement;
        EndOfFileToken = endOfFileToken;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(Statement.Span.Start, EndOfFileToken.Span.End);
}
