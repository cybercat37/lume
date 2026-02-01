using Axom.Compiler.Binding;
using Axom.Compiler.Emitting;
using Axom.Compiler.Parsing;

namespace Axom.Compiler;

public sealed class CompilerCache
{
    public SyntaxTreeCache SyntaxTrees { get; } = new();
    public BindingCache Bindings { get; } = new();
    public EmitCache Emitted { get; } = new();
}
