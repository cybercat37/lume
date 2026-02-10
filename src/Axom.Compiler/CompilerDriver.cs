using Axom.Compiler.Binding;
using Axom.Compiler.Diagnostics;
using Axom.Compiler.Emitting;
using Axom.Compiler.Lowering;
using Axom.Compiler.Modules;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

namespace Axom.Compiler;

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

        var resolved = ResolveModulesIfNeeded(source, fileName);
        if (!resolved.Success)
        {
            var entrySyntaxTree = resolved.EntrySyntaxTree ?? SyntaxTree.Parse(new SourceText(source, fileName));
            return CompilationResult.Fail(resolved.Diagnostics, entrySyntaxTree);
        }

        source = resolved.CombinedSource;
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

        var lowerer = new Lowerer();
        var loweredProgram = lowerer.Lower(bindResult.Program);
        var emitter = new Emitter();
        var generatedCode = emitter.Emit(loweredProgram);
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

        var resolved = ResolveModulesIfNeeded(source, fileName);
        if (!resolved.Success)
        {
            var entrySyntaxTree = resolved.EntrySyntaxTree ?? SyntaxTree.Parse(new SourceText(source, fileName));
            return CompilationResult.Fail(resolved.Diagnostics, entrySyntaxTree);
        }

        source = resolved.CombinedSource;
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

        var lowerer = new Lowerer();
        var loweredProgram = lowerer.Lower(bindResult.Program);
        var emitter = new Emitter();
        var generatedCode = emitter.EmitCached(loweredProgram, cache.Emitted);
        return CompilationResult.CreateSuccess(generatedCode, syntaxTree);
    }

    private static ModuleResolutionResult ResolveModulesIfNeeded(string source, string fileName)
    {
        if (!source.Contains("import", StringComparison.Ordinal)
            && !source.Contains("from", StringComparison.Ordinal)
            && !source.Contains("pub", StringComparison.Ordinal))
        {
            return ModuleResolutionResult.CreateSuccess(source);
        }

        if (!File.Exists(fileName))
        {
            return ModuleResolutionResult.CreateSuccess(source);
        }

        var resolver = new ModuleResolver();
        return resolver.Resolve(fileName, source);
    }
}
