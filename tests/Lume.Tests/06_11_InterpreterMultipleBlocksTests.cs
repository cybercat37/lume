using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class InterpreterMultipleBlocksTests
{
    [Fact]
    public void Multiple_blocks_execute_in_order()
    {
        var sourceText = new SourceText(@"
{
print 1
}
{
print 2
}
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        var lines = result.Output.Trim().Split('\n');
        Assert.Equal("1", lines[0].Trim());
        Assert.Equal("2", lines[1].Trim());
        Assert.Empty(result.Diagnostics);
    }
}
