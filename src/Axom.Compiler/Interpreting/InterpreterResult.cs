using Axom.Compiler.Diagnostics;

namespace Axom.Compiler.Interpreting;

public sealed class InterpreterResult
{
    public string Output { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    public InterpreterResult(string output, IReadOnlyList<Diagnostic> diagnostics)
    {
        Output = output;
        Diagnostics = diagnostics;
    }
}
