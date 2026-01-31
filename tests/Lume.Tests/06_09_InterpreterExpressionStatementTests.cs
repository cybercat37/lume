using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class InterpreterExpressionStatementTests
{
    [Fact]
    public void Expression_statement_produces_no_output()
    {
        var sourceText = new SourceText(@"
1 + 2
print 3
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("3", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
