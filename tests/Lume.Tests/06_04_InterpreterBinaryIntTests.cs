using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class InterpreterBinaryIntTests
{
    [Fact]
    public void Binary_expression_evaluates_with_precedence()
    {
        var sourceText = new SourceText("print 1 + 2 * 3", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("7", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
