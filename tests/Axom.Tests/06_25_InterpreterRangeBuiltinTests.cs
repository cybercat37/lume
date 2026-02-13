using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterRangeBuiltinTests
{
    [Fact]
    public void Range_builds_half_open_sequence()
    {
        var sourceText = new SourceText("print range(1, 5)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("[1, 2, 3, 4]", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Range_returns_empty_when_end_is_not_greater_than_start()
    {
        var sourceText = new SourceText("print range(3, 3)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("[]", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
