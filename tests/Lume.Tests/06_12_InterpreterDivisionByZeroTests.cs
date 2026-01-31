using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class InterpreterDivisionByZeroTests
{
    [Fact]
    public void Division_by_zero_produces_diagnostic()
    {
        var sourceText = new SourceText("print 1 / 0", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.NotEmpty(result.Diagnostics);
    }
}
