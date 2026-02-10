using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class PubStatementSyntax : StatementSyntax
{
    public SyntaxToken PubKeyword { get; }
    public StatementSyntax Declaration { get; }

    public PubStatementSyntax(SyntaxToken pubKeyword, StatementSyntax declaration)
    {
        PubKeyword = pubKeyword;
        Declaration = declaration;
    }

    public override TextSpan Span =>
        TextSpan.FromBounds(PubKeyword.Span.Start, Declaration.Span.End);
}
