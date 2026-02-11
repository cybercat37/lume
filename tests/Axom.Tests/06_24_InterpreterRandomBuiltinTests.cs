using Axom.Compiler.Diagnostics;
using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterRandomBuiltinTests
{
    [Fact]
    public void Rand_seed_makes_rand_float_deterministic()
    {
        var sourceText = new SourceText("rand_seed(123)\nprint rand_float()\nrand_seed(123)\nprint rand_float()", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal(lines[0], lines[1]);
        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void Rand_int_returns_error_when_max_is_not_positive()
    {
        var sourceText = new SourceText("print rand_int(0)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("Error(max must be > 0)", result.Output.Trim());
        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    }
}
