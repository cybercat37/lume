using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class InterpreterBinarySubtractionTests
{
    [Fact]
    public void Subtraction_expression_evaluates()
    {
        var sourceText = new SourceText("print 5 - 2", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("3", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
