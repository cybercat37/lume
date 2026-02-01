using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterMinMaxBuiltinTests
{
    [Fact]
    public void Min_returns_smaller_value()
    {
        var sourceText = new SourceText("print min(5, 10)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("5", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Min_returns_first_when_equal()
    {
        var sourceText = new SourceText("print min(5, 5)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("5", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Min_with_negative_returns_smaller()
    {
        var sourceText = new SourceText("print min(-5, 10)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("-5", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Max_returns_larger_value()
    {
        var sourceText = new SourceText("print max(5, 10)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("10", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Max_returns_first_when_equal()
    {
        var sourceText = new SourceText("print max(5, 5)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("5", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Max_with_negative_returns_larger()
    {
        var sourceText = new SourceText("print max(-5, 10)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("10", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
