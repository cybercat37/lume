using Axom.Compiler.Text;

namespace Axom.Compiler.Syntax;

public abstract class SyntaxNode
{
    public abstract TextSpan Span { get; }
}
