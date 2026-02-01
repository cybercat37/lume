using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterBlockScopeTests
{
    [Fact]
    public void Block_scope_shadows_variables()
    {
        var sourceText = new SourceText(@"
let x = 1
{
let x = 2
print x
}
print x
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        var lines = result.Output.Trim().Split('\n');
        Assert.Equal("2", lines[0].Trim());
        Assert.Equal("1", lines[1].Trim());
        Assert.Empty(result.Diagnostics);
    }
}
