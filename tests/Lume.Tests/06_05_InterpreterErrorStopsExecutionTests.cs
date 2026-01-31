using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class InterpreterErrorStopsExecutionTests
{
    [Fact]
    public void Undefined_variable_stops_execution()
    {
        var sourceText = new SourceText(@"
print x
print 1
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
        Assert.Equal(string.Empty, result.Output.Trim());
    }
}
