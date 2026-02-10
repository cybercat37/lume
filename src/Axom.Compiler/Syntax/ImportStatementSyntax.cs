using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class ImportStatementSyntax : StatementSyntax
{
    public SyntaxToken ImportKeyword { get; }
    public IReadOnlyList<SyntaxToken> ModuleParts { get; }
    public SyntaxToken? AsKeyword { get; }
    public SyntaxToken? Alias { get; }

    public ImportStatementSyntax(
        SyntaxToken importKeyword,
        IReadOnlyList<SyntaxToken> moduleParts,
        SyntaxToken? asKeyword,
        SyntaxToken? alias)
    {
        ImportKeyword = importKeyword;
        ModuleParts = moduleParts;
        AsKeyword = asKeyword;
        Alias = alias;
    }

    public override TextSpan Span
    {
        get
        {
            var end = Alias?.Span.End
                ?? ModuleParts.LastOrDefault()?.Span.End
                ?? ImportKeyword.Span.End;
            return TextSpan.FromBounds(ImportKeyword.Span.Start, end);
        }
    }
}
