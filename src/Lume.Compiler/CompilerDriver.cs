using Lume.Compiler.Binding;
using Lume.Compiler.Diagnostics;
using Lume.Compiler.Emitting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

namespace Lume.Compiler;

public sealed class CompilerDriver
{
    public const int MaxSourceLength = 1_000_000;

    public CompilationResult Compile(string source, string fileName)
    {
        if (source.Length > MaxSourceLength)
        {
            var diagnostic = Diagnostic.Error(fileName, 1, 1, $"Source file exceeds max length {MaxSourceLength}.");
            var emptyTree = SyntaxTree.Parse(new SourceText(string.Empty, fileName));
            return CompilationResult.Fail(new List<Diagnostic> { diagnostic }, emptyTree);
        }

        var sourceText = new SourceText(source, fileName);
        var syntaxTree = SyntaxTree.Parse(sourceText);
        var binder = new Binder();
        var bindResult = binder.Bind(syntaxTree);
        var diagnostics = syntaxTree.Diagnostics
            .Concat(bindResult.Diagnostics)
            .ToList();

        if (diagnostics.Count > 0)
        {
            return CompilationResult.Fail(diagnostics, syntaxTree);
        }

        var emitter = new Emitter();
        var generatedCode = emitter.Emit(bindResult.Program);
        return CompilationResult.CreateSuccess(generatedCode, syntaxTree);
    }

    public CompilationResult CompileCached(string source, string fileName, CompilerCache cache)
    {
        if (source.Length > MaxSourceLength)
        {
            var diagnostic = Diagnostic.Error(fileName, 1, 1, $"Source file exceeds max length {MaxSourceLength}.");
            var emptyTree = SyntaxTree.Parse(new SourceText(string.Empty, fileName));
            return CompilationResult.Fail(new List<Diagnostic> { diagnostic }, emptyTree);
        }

        var sourceText = new SourceText(source, fileName);
        var syntaxTree = SyntaxTree.ParseCached(sourceText, cache.SyntaxTrees);
        var binder = new Binder();
        var bindResult = binder.BindCached(syntaxTree, cache.Bindings);
        var diagnostics = syntaxTree.Diagnostics
            .Concat(bindResult.Diagnostics)
            .ToList();

        if (diagnostics.Count > 0)
        {
            return CompilationResult.Fail(diagnostics, syntaxTree);
        }

        var emitter = new Emitter();
        var generatedCode = emitter.EmitCached(bindResult.Program, cache.Emitted);
        return CompilationResult.CreateSuccess(generatedCode, syntaxTree);
    }
}
