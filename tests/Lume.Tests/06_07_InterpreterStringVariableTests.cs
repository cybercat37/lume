using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class InterpreterStringVariableTests
{
    [Fact]
    public void String_variable_can_be_printed()
    {
        var sourceText = new SourceText(@"
let mut x = ""hi""
print x
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("hi", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
