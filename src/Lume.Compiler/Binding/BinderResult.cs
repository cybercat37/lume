using Lume.Compiler.Diagnostics;

namespace Lume.Compiler.Binding;

public sealed class BinderResult
{
    public BoundProgram Program { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    public BinderResult(BoundProgram program, IReadOnlyList<Diagnostic> diagnostics)
    {
        Program = program;
        Diagnostics = diagnostics;
    }
}
