using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterDivisionTests
{
    [Fact]
    public void Division_expression_evaluates()
    {
        var sourceText = new SourceText("print 8 / 2", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("4", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
