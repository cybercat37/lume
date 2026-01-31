using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class InterpreterMutableAssignmentTests
{
    [Fact]
    public void Mutable_assignment_updates_value()
    {
        var sourceText = new SourceText(@"
let mut x = 1
x = 2
print x
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("2", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
