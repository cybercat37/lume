using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class InterpreterOuterAssignmentTests
{
    [Fact]
    public void Inner_block_can_update_outer_mutable_variable()
    {
        var sourceText = new SourceText(@"
let mut x = 1
{
x = 4
}
print x
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("4", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
