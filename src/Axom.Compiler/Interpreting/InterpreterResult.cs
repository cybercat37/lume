using Axom.Compiler.Diagnostics;

namespace Axom.Compiler.Interpreting;

public sealed class InterpreterResult
{
    public string Output { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public InterpreterHttpResponse? Response { get; }

    public InterpreterResult(string output, IReadOnlyList<Diagnostic> diagnostics, InterpreterHttpResponse? response = null)
    {
        Output = output;
        Diagnostics = diagnostics;
        Response = response;
    }
}

public sealed record InterpreterHttpResponse(int StatusCode, string Body);
