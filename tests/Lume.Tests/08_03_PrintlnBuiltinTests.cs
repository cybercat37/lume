using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class PrintlnBuiltinTests
{
    [Fact]
    public void Println_outputs_line()
    {
        var sourceText = new SourceText("println 1", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("1", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
