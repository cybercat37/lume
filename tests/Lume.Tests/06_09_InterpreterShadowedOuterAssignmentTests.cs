using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class InterpreterShadowedOuterAssignmentTests
{
    [Fact]
    public void Inner_scope_can_update_outer_mutable_variable()
    {
        var sourceText = new SourceText(@"
let mut x = 1
{
x = x + 1
}
print x
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("2", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
