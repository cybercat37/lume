using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterPrintIntTests
{
    [Fact]
    public void Print_int_outputs_value()
    {
        var sourceText = new SourceText("print 1", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("1", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
