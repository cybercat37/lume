using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class InterpreterBoolVariableTests
{
    [Fact]
    public void Bool_variable_can_be_printed()
    {
        var sourceText = new SourceText(@"
let x = true
print x
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("true", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
