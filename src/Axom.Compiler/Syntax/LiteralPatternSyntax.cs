using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class LiteralPatternSyntax : PatternSyntax
{
    public SyntaxToken LiteralToken { get; }

    public LiteralPatternSyntax(SyntaxToken literalToken)
    {
        LiteralToken = literalToken;
    }

    public override TextSpan Span =>
        LiteralToken.Span;
}
