using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterFloatTests
{
    [Fact]
    public void Interpreter_evaluates_float_expressions()
    {
        var sourceText = new SourceText(@"
print 1.5 + 2.25
print 3.5 >= 3.5
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("3.75\ntrue", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
