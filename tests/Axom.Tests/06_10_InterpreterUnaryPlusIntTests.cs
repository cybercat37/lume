using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterUnaryPlusIntTests
{
    [Fact]
    public void Unary_plus_on_int_evaluates()
    {
        var sourceText = new SourceText("print +1", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("1", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
