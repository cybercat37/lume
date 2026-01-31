using Lume.Compiler.Interpreting;
using Lume.Compiler.Parsing;
using Lume.Compiler.Text;

public class InterpreterVariableDeclarationTests
{
    [Fact]
    public void Variable_declaration_can_be_printed()
    {
        var sourceText = new SourceText(@"
let x = 1
print x
", "test.lume");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("1", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
