using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class InterpreterPrintBoolTests
{
    [Fact]
    public void Print_bool_outputs_value()
    {
        var sourceText = new SourceText("print true", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("true", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
