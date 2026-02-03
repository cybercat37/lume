using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterLogicalTests
{
    [Fact]
    public void Interpreter_evaluates_logical_operators()
    {
        var sourceText = new SourceText(@"
print true && false
print true || false
print !false
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("false\ntrue\ntrue", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
