using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class InputBuiltinTests
{
    [Fact]
    public void Input_returns_stubbed_value()
    {
        var sourceText = new SourceText("print input", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        interpreter.SetInput("hello");

        var result = interpreter.Run(syntaxTree);

        Assert.Equal("hello", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
