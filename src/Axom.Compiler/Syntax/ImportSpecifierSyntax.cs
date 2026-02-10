using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class ImportSpecifierSyntax : SyntaxNode
{
    public SyntaxToken NameToken { get; }
    public SyntaxToken? AsKeyword { get; }
    public SyntaxToken? AliasToken { get; }

    public ImportSpecifierSyntax(SyntaxToken nameToken, SyntaxToken? asKeyword, SyntaxToken? aliasToken)
    {
        NameToken = nameToken;
        AsKeyword = asKeyword;
        AliasToken = aliasToken;
    }

    public bool IsWildcard => NameToken.Kind == TokenKind.Star;

    public override TextSpan Span =>
        TextSpan.FromBounds(NameToken.Span.Start, (AliasToken ?? NameToken).Span.End);
}
