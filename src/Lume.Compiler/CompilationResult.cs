using Lume.Compiler.Diagnostics;

namespace Lume.Compiler;

public sealed class CompilationResult
{
    public bool Success { get; }
    public string GeneratedCode { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    private CompilationResult(
        bool success,
        string generatedCode,
        IReadOnlyList<Diagnostic> diagnostics)
    {
        Success = success;
        GeneratedCode = generatedCode;
        Diagnostics = diagnostics;
    }

    public static CompilationResult CreateSuccess(string code) =>
        new(true, code, Array.Empty<Diagnostic>());

    public static CompilationResult Fail(IReadOnlyList<Diagnostic> diagnostics) =>
        new(false, string.Empty, diagnostics);
}
