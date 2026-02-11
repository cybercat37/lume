using Axom.Compiler;
using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class LoggingAspectTests
{
    [Fact]
    public void Logging_aspect_is_applied_during_interpretation()
    {
        var source = "@logging fn add(a: Int, b: Int) -> Int { return a + b }\nprint add(3, 4)";
        var syntaxTree = SyntaxTree.Parse(new SourceText(source, "test.axom"));

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
        var lines = result.Output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("[log] call add(3, 4)", lines[0]);
        Assert.Contains("[log] return add => 7", lines[1]);
        Assert.Equal("7", lines[2]);
    }

    [Fact]
    public void Logging_aspect_emits_codegen_helpers()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("@logging fn inc(x: Int) -> Int { return x + 1 }\nprint inc(1)", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("AxomLogInvocation", result.GeneratedCode);
        Assert.Contains("AxomLogReturn", result.GeneratedCode);
    }
}
