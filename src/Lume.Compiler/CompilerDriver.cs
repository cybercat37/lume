using Lume.Compiler.Binding;
using Lume.Compiler.Emitting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

namespace Lume.Compiler;

public sealed class CompilerDriver
{
    public CompilationResult Compile(string source, string fileName)
    {
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
        var generatedCode = emitter.Emit(syntaxTree.Root);
        return CompilationResult.CreateSuccess(generatedCode, syntaxTree);
    }
}
