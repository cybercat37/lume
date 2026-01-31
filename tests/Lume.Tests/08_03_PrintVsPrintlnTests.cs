using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class PrintVsPrintlnTests
{
    [Fact]
    public void Print_does_not_append_extra_newline_in_output_buffer()
    {
        var sourceText = new SourceText("print 1\nprintln 2", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("1", lines[0].Trim());
        Assert.Equal("2", lines[1].Trim());
        Assert.Empty(result.Diagnostics);
    }
}
