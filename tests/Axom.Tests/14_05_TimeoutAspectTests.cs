using Axom.Compiler;
using Axom.Compiler.Binding;
using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class TimeoutAspectTests
{
    [Fact]
    public void Timeout_aspect_returns_error_when_execution_exceeds_deadline()
    {
        var source = "@timeout(1) fn delayed(max: Int) { sleep(10)\n return rand_int(max) }\nprint delayed(10)";
        var syntaxTree = SyntaxTree.Parse(new SourceText(source, "test.axom"));

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
        Assert.Contains("Error(timeout after 1ms)", result.Output);
    }

    [Fact]
    public void Timeout_aspect_allows_result_before_deadline()
    {
        var source = "rand_seed(7)\n@timeout(50) fn delayed(max: Int) { sleep(1)\n return rand_int(max) }\nprint delayed(10)";
        var syntaxTree = SyntaxTree.Parse(new SourceText(source, "test.axom"));

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
        Assert.Contains("Ok(", result.Output);
    }

    [Fact]
    public void Timeout_aspect_requires_result_string_return_type()
    {
        var sourceText = new SourceText("@timeout(10) fn value() -> Int => 1", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Contains(result.Diagnostics, d =>
            d.Message.Contains("Result<T, String>", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Timeout_aspect_emits_codegen_timeout_helper()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("@timeout(5) fn delayed(max: Int) { sleep(1)\n return rand_int(max) }\nprint delayed(10)", "test.axom");

        Assert.True(result.Success);
        Assert.Contains("AxomApplyTimeout", result.GeneratedCode);
        Assert.Contains("timeout after", result.GeneratedCode);
    }
}
