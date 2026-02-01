using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterShadowingIsolationTests
{
    [Fact]
    public void Inner_assignment_does_not_affect_outer_variable()
    {
        var sourceText = new SourceText(@"
let mut x = 1
{
let mut x = 2
x = 3
}
print x
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("1", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
