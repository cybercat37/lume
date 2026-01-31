using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class InterpreterLenBuiltinTests
{
    [Fact]
    public void Len_of_string_returns_length()
    {
        var sourceText = new SourceText("print len(\"hello\")", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("5", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Len_of_empty_string_returns_zero()
    {
        var sourceText = new SourceText("print len(\"\")", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("0", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Len_of_long_string_returns_correct_length()
    {
        var sourceText = new SourceText("print len(\"abcdefghijklmnopqrstuvwxyz\")", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("26", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
