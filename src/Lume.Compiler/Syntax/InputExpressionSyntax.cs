using Lume.Compiler.Lexing;
using Lume.Compiler.Text;

namespace Lume.Compiler.Syntax;

public sealed class InputExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken InputKeyword { get; }

    public InputExpressionSyntax(SyntaxToken inputKeyword)
    {
        InputKeyword = inputKeyword;
    }

    public override TextSpan Span => InputKeyword.Span;
}
