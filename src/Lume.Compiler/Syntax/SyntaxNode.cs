using Lume.Compiler.Text;

namespace Lume.Compiler.Syntax;

public abstract class SyntaxNode
{
    public abstract TextSpan Span { get; }
}
