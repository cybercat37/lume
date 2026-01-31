using Lume.Compiler.Diagnostics;
using Lume.Compiler.Parsing;

namespace Lume.Compiler;

public sealed class CompilationResult
{
    public bool Success { get; }
    public string GeneratedCode { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public SyntaxTree SyntaxTree { get; }

    private CompilationResult(
        bool success,
        string generatedCode,
        IReadOnlyList<Diagnostic> diagnostics,
        SyntaxTree syntaxTree)
    {
        Success = success;
        GeneratedCode = generatedCode;
        Diagnostics = diagnostics;
        SyntaxTree = syntaxTree;
    }

    public static CompilationResult CreateSuccess(string code, SyntaxTree syntaxTree) =>
        new(true, code, Array.Empty<Diagnostic>(), syntaxTree);

    public static CompilationResult Fail(IReadOnlyList<Diagnostic> diagnostics, SyntaxTree syntaxTree) =>
        new(false, string.Empty, diagnostics, syntaxTree);
}
