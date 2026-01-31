using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class InterpreterNestedParenTests
{
    [Fact]
    public void Nested_parentheses_evaluate_correctly()
    {
        var sourceText = new SourceText("print ((1 + 2) * (3 + 1))", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("12", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
