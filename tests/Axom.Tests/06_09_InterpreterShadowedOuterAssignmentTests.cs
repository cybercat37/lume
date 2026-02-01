using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

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
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("2", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
