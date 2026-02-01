using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterPrintBoolTests
{
    [Fact]
    public void Print_bool_outputs_value()
    {
        var sourceText = new SourceText("print true", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("true", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
