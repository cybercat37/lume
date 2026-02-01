using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InputSequenceTests
{
    [Fact]
    public void Input_returns_values_in_order()
    {
        var sourceText = new SourceText("print input\nprintln input", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        interpreter.SetInput("a", "b");

        var result = interpreter.Run(syntaxTree);

        var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("a", lines[0].Trim());
        Assert.Equal("b", lines[1].Trim());
        Assert.Empty(result.Diagnostics);
    }
}
