using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterPrintStringTests
{
    [Fact]
    public void Print_string_outputs_value()
    {
        var sourceText = new SourceText("print \"hi\"", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("hi", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
