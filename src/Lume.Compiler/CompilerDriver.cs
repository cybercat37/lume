using Lume.Compiler.Diagnostics;

namespace Lume.Compiler;

public sealed class CompilerDriver
{
    public CompilationResult Compile(string source, string fileName)
    {
        var diagnostics = new List<Diagnostic>();

        if (string.IsNullOrWhiteSpace(source))
        {
            diagnostics.Add(
                Diagnostic.Error(
                    fileName,
                    1,
                    1,
                    "Source file is empty"
                )
            );

            return CompilationResult.Fail(diagnostics);
        }

        // Placeholder: lexer/parser/codegen arriveranno qui
        var generatedCode = """
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Hello from Lume (stub)");
    }
}
""";

        return CompilationResult.CreateSuccess(generatedCode);
    }
}
