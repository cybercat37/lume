using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class PrintlnStringTests
{
    [Fact]
    public void Println_outputs_string()
    {
        var sourceText = new SourceText("println \"hi\"", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("hi", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
