using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class InterpreterPrintStringTests
{
    [Fact]
    public void Print_string_outputs_value()
    {
        var sourceText = new SourceText("print \"hi\"", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("hi", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
