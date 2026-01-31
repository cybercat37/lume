using Lume.Compiler.Binding;
using Lume.Compiler.Emitting;
using Lume.Compiler.Parsing;

namespace Lume.Compiler;

public sealed class CompilerCache
{
    public SyntaxTreeCache SyntaxTrees { get; } = new();
    public BindingCache Bindings { get; } = new();
    public EmitCache Emitted { get; } = new();
}
