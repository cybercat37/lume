using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class PrintlnEmptyStringTests
{
    [Fact]
    public void Println_empty_string_outputs_blank_line()
    {
        var sourceText = new SourceText("println \"\"", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal(string.Empty, result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
