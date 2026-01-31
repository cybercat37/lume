using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class PrintBuiltinRegressionTests
{
    [Fact]
    public void Print_builtin_still_outputs_value()
    {
        var sourceText = new SourceText("print 2", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("2", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
