using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterStdlibCollectionResultTests
{
    [Fact]
    public void Count_sum_any_all_evaluate_expected_values()
    {
        var sourceText = new SourceText(
            "print count([1, 2, 3])\nprint sum([1, 2, 3])\nprint sum([1.0, 2.5])\nprint any([1, 2, 3], fn(x: Int) => x > 2)\nprint all([1, 2, 3], fn(x: Int) => x > 0)",
            "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal("3", lines[0]);
        Assert.Equal("6", lines[1]);
        Assert.Equal("3.5", lines[2]);
        Assert.Equal("true", lines[3]);
        Assert.Equal("true", lines[4]);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Result_map_transforms_ok_and_keeps_error()
    {
        var sourceText = new SourceText(
            "rand_seed(7)\nprint result_map(rand_int(10), fn(x: Int) => x + 1)\nprint result_map(rand_int(0), fn(x: Int) => x + 1)",
            "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.StartsWith("Ok(", lines[0]);
        Assert.Equal("Error(max must be > 0)", lines[1]);
        Assert.Empty(result.Diagnostics);
    }
}
