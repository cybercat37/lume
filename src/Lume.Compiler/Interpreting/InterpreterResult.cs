using Lume.Compiler.Diagnostics;

namespace Lume.Compiler.Interpreting;

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
