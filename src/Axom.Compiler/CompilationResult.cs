using Axom.Compiler.Diagnostics;
using Axom.Compiler.Parsing;

namespace Axom.Compiler;

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

    public static CompilationResult CreateSuccess(string code, SyntaxTree syntaxTree, IReadOnlyList<Diagnostic>? diagnostics = null) =>
        new(true, code, diagnostics ?? Array.Empty<Diagnostic>(), syntaxTree);

    public static CompilationResult Fail(IReadOnlyList<Diagnostic> diagnostics, SyntaxTree syntaxTree) =>
        new(false, string.Empty, diagnostics, syntaxTree);
}
