using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class CompilationUnitSyntax : SyntaxNode
{
    public IReadOnlyList<StatementSyntax> Statements { get; }
    public SyntaxToken EndOfFileToken { get; }

    public CompilationUnitSyntax(IReadOnlyList<StatementSyntax> statements, SyntaxToken endOfFileToken)
    {
        Statements = statements;
        EndOfFileToken = endOfFileToken;
    }

    public override TextSpan Span
    {
        get
        {
            if (Statements.Count == 0)
            {
                return EndOfFileToken.Span;
            }

            return TextSpan.FromBounds(Statements[0].Span.Start, EndOfFileToken.Span.End);
        }
    }
}
