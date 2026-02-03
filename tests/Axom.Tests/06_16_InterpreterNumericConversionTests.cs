using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterNumericConversionTests
{
    [Fact]
    public void Interpreter_evaluates_numeric_conversions()
    {
        var sourceText = new SourceText(@"
print float(2)
print int(3.9)
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("2\n3", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
