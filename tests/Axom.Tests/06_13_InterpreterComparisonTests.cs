using Axom.Compiler.Parsing;
using Axom.Compiler.Text;
using Axom.Compiler.Interpreting;

public class InterpreterComparisonTests
{
    [Fact]
    public void Interpreter_evaluates_int_comparisons()
    {
        var sourceText = new SourceText(@"
print 1 < 2
print 3 >= 3
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("true\ntrue", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Interpreter_evaluates_string_equality()
    {
        var sourceText = new SourceText(@"
print ""a"" == ""a""
print ""a"" != ""b""
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("true\ntrue", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
