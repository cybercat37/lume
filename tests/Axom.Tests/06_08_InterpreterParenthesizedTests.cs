using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterParenthesizedTests
{
    [Fact]
    public void Parenthesized_expression_overrides_precedence()
    {
        var sourceText = new SourceText("print (1 + 2) * 3", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("9", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
