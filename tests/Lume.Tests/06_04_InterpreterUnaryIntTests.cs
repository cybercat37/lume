using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class InterpreterUnaryIntTests
{
    [Fact]
    public void Unary_expression_evaluates()
    {
        var sourceText = new SourceText("print -1", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("-1", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
