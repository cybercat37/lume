using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class FromImportStatementSyntax : StatementSyntax
{
    public SyntaxToken FromKeyword { get; }
    public IReadOnlyList<SyntaxToken> ModuleParts { get; }
    public SyntaxToken ImportKeyword { get; }
    public IReadOnlyList<ImportSpecifierSyntax> Specifiers { get; }

    public FromImportStatementSyntax(
        SyntaxToken fromKeyword,
        IReadOnlyList<SyntaxToken> moduleParts,
        SyntaxToken importKeyword,
        IReadOnlyList<ImportSpecifierSyntax> specifiers)
    {
        FromKeyword = fromKeyword;
        ModuleParts = moduleParts;
        ImportKeyword = importKeyword;
        Specifiers = specifiers;
    }

    public override TextSpan Span
    {
        get
        {
            var end = Specifiers.LastOrDefault()?.Span.End
                ?? ModuleParts.LastOrDefault()?.Span.End
                ?? ImportKeyword.Span.End;
            return TextSpan.FromBounds(FromKeyword.Span.Start, end);
        }
    }
}
