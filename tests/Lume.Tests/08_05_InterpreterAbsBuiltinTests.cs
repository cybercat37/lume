using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class InterpreterAbsBuiltinTests
{
    [Fact]
    public void Abs_of_positive_number_returns_same()
    {
        var sourceText = new SourceText("print abs(5)", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("5", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Abs_of_negative_number_returns_positive()
    {
        var sourceText = new SourceText("print abs(-5)", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("5", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Abs_of_zero_returns_zero()
    {
        var sourceText = new SourceText("print abs(0)", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("0", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
