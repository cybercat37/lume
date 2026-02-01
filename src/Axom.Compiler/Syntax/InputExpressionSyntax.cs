using Axom.Compiler.Lexing;
using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public sealed class InputExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken InputKeyword { get; }

    public InputExpressionSyntax(SyntaxToken inputKeyword)
    {
        InputKeyword = inputKeyword;
    }

    public override TextSpan Span => InputKeyword.Span;
}
