using Axom.Compiler.Interpreting;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class InterpreterFloatBuiltinTests
{
    [Fact]
    public void Interpreter_evaluates_float_builtins()
    {
        var sourceText = new SourceText(@"
print abs(-1.5)
print min(1.5, 2.5)
print max(1.5, 2.5)
", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var interpreter = new Interpreter();
        var result = interpreter.Run(syntaxTree);

        Assert.Equal("1.5\n1.5\n2.5", result.Output.Trim());
        Assert.Empty(result.Diagnostics);
    }
}
